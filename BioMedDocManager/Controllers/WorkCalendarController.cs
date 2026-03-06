using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Fable.React.Props.SVGAttr;

namespace BioMedDocManager.Controllers
{
    public class WorkCalendarVm
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public List<DayInfo> Left { get; set; } = new();
        public List<DayInfo> Right { get; set; } = new();

        public int WorkingDays { get; set; }
        public int WorkingHours => WorkingDays * 8;

        public class DayInfo
        {
            public DateTime Date { get; set; }
            public bool IsWeekend { get; set; }
            public bool IsHoliday { get; set; }      // non-weekend holiday
            public string? HolidayName { get; set; } // e.g. 國慶日, 補假...

            public int AbsenceAmMinutes { get; set; }
            public int AbsencePmMinutes { get; set; }
        }
    }

    [AllowAnonymous]
    public class WorkCalendarController : Controller
    {
        // TODO  DB TABLE 建立與匯入 CSV /TSV 功能 (明年度)
        // 行事曆有萬年曆 除週六補班外不須匯入六日的利假. 只需手動維護颱風調整放假
        // 員工申請請假期間, 會從總表比對, 只針對正常上班日使用請假時數 (同常會在年底結算)
        private const string HolidaysCsv2026 = @"
Subject	Start Date	Start Time	End Date	End Time	All Day Event	Description	Location
中華民國開國紀念日	2026-1-1		2026-1-1		TRUE
例假日	2026-1-3		2026-1-3		TRUE
例假日	2026-1-4		2026-1-4		TRUE
例假日	2026-1-10		2026-1-10		TRUE
例假日	2026-1-11		2026-1-11		TRUE
例假日	2026-1-17		2026-1-17		TRUE
例假日	2026-1-18		2026-1-18		TRUE
例假日	2026-1-24		2026-1-24		TRUE
例假日	2026-1-25		2026-1-25		TRUE
例假日	2026-1-31		2026-1-31		TRUE
例假日	2026-2-1		2026-2-1		TRUE
例假日	2026-2-7		2026-2-7		TRUE
例假日	2026-2-8		2026-2-8		TRUE
例假日	2026-2-14		2026-2-14		TRUE
小年夜	2026-2-15		2026-2-15		TRUE
農曆除夕	2026-2-16		2026-2-16		TRUE
春節	2026-2-17		2026-2-17		TRUE
春節	2026-2-18		2026-2-18		TRUE
春節	2026-2-19		2026-2-19		TRUE
補假	2026-2-20		2026-2-20		TRUE
例假日	2026-2-21		2026-2-21		TRUE
例假日	2026-2-22		2026-2-22		TRUE
補假	2026-2-27		2026-2-27		TRUE
和平紀念日	2026-2-28		2026-2-28		TRUE
例假日	2026-3-1		2026-3-1		TRUE
補假	2026-4-3		2026-4-3		TRUE
兒童節	2026-4-4		2026-4-4		TRUE
清明節	2026-4-5		2026-4-5		TRUE
補假	2026-4-6		2026-4-6		TRUE
勞動節	2026-5-1		2026-5-1		TRUE
端午節	2026-6-19		2026-6-19		TRUE
中秋節	2026-9-25		2026-9-25		TRUE
教師節	2026-9-28		2026-9-28		TRUE
補假	2026-10-9		2026-10-9		TRUE
國慶日	2026-10-10		2026-10-10		TRUE
臺灣光復暨金門古寧頭大捷紀念日	2026-10-25		2026-10-25		TRUE
補假	2026-10-26		2026-10-26		TRUE
行憲紀念日	2026-12-25		2026-12-25		TRUE
";

        // GET /WorkCalendar?year=2026&month=2
        public IActionResult Index(int year = 2026, int? month = null)
        {

            ViewBag.CspNonce = HttpContext.Items["CspNonce"] as string;
            year = 2026; // lock it for now

            int monthView = Math.Clamp(month ?? DateTime.Now.Month, 1, 12);

            var holidayMap = ParseHolidayMap(HolidaysCsv2026);

            var daysInMonth = DateTime.DaysInMonth(year, monthView);
            var all = new List<WorkCalendarVm.DayInfo>(daysInMonth);

            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(year, monthView, d);
                var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                holidayMap.TryGetValue(date, out var holidayName);

                var isHoliday = !isWeekend && !string.IsNullOrWhiteSpace(holidayName);

                all.Add(new WorkCalendarVm.DayInfo
                {
                    Date = date,
                    IsWeekend = isWeekend,
                    IsHoliday = isHoliday,
                    HolidayName = holidayName
                });
            }

            var vm = new WorkCalendarVm
            {
                Year = year,
                Month = monthView,
                Left = all.Take(15).ToList(),
                Right = all.Skip(15).ToList(),
                WorkingDays = all.Count(x => !x.IsWeekend && !x.IsHoliday)
            };




            return View(vm);
        }

        private static Dictionary<DateTime, string> ParseHolidayMap(string tsv)
        {
            var map = new Dictionary<DateTime, string>();

            var lines = tsv
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();

            if (lines.Count <= 1)
                return map; // only header

            // Expect tab-separated (Google Calendar CSV often becomes TSV when pasted).
            for (int i = 1; i < lines.Count; i++)
            {
                var cols = lines[i].Split('\t');
                if (cols.Length < 2)
                    continue;

                var subject = cols[0]?.Trim();
                var startDateRaw = cols[1]?.Trim();

                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(startDateRaw))
                    continue;

                // We only treat non-例假日 as “holiday names” for red marking.
                if (subject == "例假日")
                    continue;

                if (!TryParseYmdLoose(startDateRaw, out var dt))
                    continue;

                map[dt.Date] = subject; // if duplicates, last wins (fine for now)
            }

            return map;
        }

        private static bool TryParseYmdLoose(string s, out DateTime dt)
        {
            // Accept "2026-1-1" as well as "2026-01-01"
            var formats = new[] { "yyyy-M-d", "yyyy-MM-dd" };
            return DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
        }
    }
}
