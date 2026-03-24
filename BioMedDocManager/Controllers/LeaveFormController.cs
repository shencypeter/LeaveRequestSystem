using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            public bool IsHoliday { get; set; }          // official/final truth
            public bool IsMakeUpWorkday { get; set; }    // future-proof
            public string? HolidayName { get; set; }     // official/final truth

            // Curiosity / display layer
            public string? LunarLabel { get; set; }      // 初一, 初二, 十五...
            public string? FestivalHint { get; set; }    // 春節, 端午節, 中秋節 (informational)

            public int AbsenceAmMinutes { get; set; }
            public int AbsencePmMinutes { get; set; }

            public bool IsWorkingDay => (!IsWeekend || IsMakeUpWorkday) && !IsHoliday;
        }
    }

    [AllowAnonymous]
    public class LeaveFormController : Controller
    {
        /// <summary>
        /// 僅代表每年度政府公布的行事曆. 
        /// </summary>
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

            var calendarService = new InlineCalendarService(HolidaysCsv2026);
            var all = calendarService.BuildMonth(year, monthView);

            var vm = new WorkCalendarVm
            {
                Year = year,
                Month = monthView,
                Left = all.Take(15).ToList(),
                Right = all.Skip(15).ToList(),
                WorkingDays = all.Count(x => x.IsWorkingDay)
            };

            return View(vm);
        }

        /// <summary>
        /// Single-file inline service:
        /// 1) official CSV = truth layer
        /// 2) predicted gregorian/lunisolar = fallback layer
        /// 3) lunar label/festival = display layer
        /// </summary>
        private sealed class InlineCalendarService
        {
            private readonly string _officialCsv;
            private readonly TaiwanLunisolarCalendar _lunar = new();

            public InlineCalendarService(string officialCsv)
            {
                _officialCsv = officialCsv ?? string.Empty;
            }

            public List<WorkCalendarVm.DayInfo> BuildMonth(int year, int month)
            {
                var officialMap = ParseOfficialHolidayMap(_officialCsv);
                var predictionMap = BuildPredictedHolidayMap(year);

                var daysInMonth = DateTime.DaysInMonth(year, month);
                var result = new List<WorkCalendarVm.DayInfo>(daysInMonth);

                for (int d = 1; d <= daysInMonth; d++)
                {
                    var date = new DateTime(year, month, d);
                    var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                    // Official truth wins; prediction is fallback
                    officialMap.TryGetValue(date.Date, out var officialHolidayName);
                    predictionMap.TryGetValue(date.Date, out var predictedHolidayName);

                    var resolvedHolidayName = !string.IsNullOrWhiteSpace(officialHolidayName)
                        ? officialHolidayName
                        : predictedHolidayName;

                    var isHoliday = !isWeekend && !string.IsNullOrWhiteSpace(resolvedHolidayName);

                    var lunarInfo = GetLunarInfo(date);

                    result.Add(new WorkCalendarVm.DayInfo
                    {
                        Date = date,
                        IsWeekend = isWeekend,
                        IsHoliday = isHoliday,
                        IsMakeUpWorkday = false, // reserved for future DB/manual override support
                        HolidayName = resolvedHolidayName,
                        LunarLabel = lunarInfo.LunarLabel,
                        FestivalHint = lunarInfo.FestivalHint
                    });
                }

                return result;
            }

            /// <summary>
            /// Official CSV / imported calendar truth.
            /// We ignore "例假日" because weekends are derived separately.
            /// </summary>
            private static Dictionary<DateTime, string> ParseOfficialHolidayMap(string tsv)
            {
                var map = new Dictionary<DateTime, string>();

                var lines = tsv
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToList();

                if (lines.Count <= 1)
                    return map;

                for (int i = 1; i < lines.Count; i++)
                {
                    var cols = lines[i].Split('\t');
                    if (cols.Length < 2)
                        continue;

                    var subject = cols[0]?.Trim();
                    var startDateRaw = cols[1]?.Trim();

                    if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(startDateRaw))
                        continue;

                    if (subject == "例假日")
                        continue;

                    if (!TryParseYmdLoose(startDateRaw, out var dt))
                        continue;

                    map[dt.Date] = subject;
                }

                return map;
            }

            /// <summary>
            /// Prediction layer:
            /// - fixed Gregorian holidays
            /// - inferred lunar festivals
            /// Used only when official import does not exist for the date.
            /// </summary>
            private Dictionary<DateTime, string> BuildPredictedHolidayMap(int year)
            {
                var map = new Dictionary<DateTime, string>();

                // Fixed Gregorian holidays
                map[new DateTime(year, 1, 1)] = "中華民國開國紀念日";
                map[new DateTime(year, 2, 28)] = "和平紀念日";
                map[new DateTime(year, 5, 1)] = "勞動節";
                map[new DateTime(year, 9, 28)] = "教師節";
                map[new DateTime(year, 10, 10)] = "國慶日";
                map[new DateTime(year, 10, 25)] = "臺灣光復暨金門古寧頭大捷紀念日";
                map[new DateTime(year, 12, 25)] = "行憲紀念日";

                // Lunar-based predictions
                for (var date = new DateTime(year, 1, 1); date.Year == year; date = date.AddDays(1))
                {
                    var lunarInfo = GetLunarInfo(date);

                    if (lunarInfo.IsLeapMonth)
                        continue; 

                    if (lunarInfo.LunarMonth == 1 && lunarInfo.LunarDay == 1)
                        map[date] = "春節";
                    else if (lunarInfo.LunarMonth == 1 && lunarInfo.LunarDay == 2)
                        map[date] = "春節";
                    else if (lunarInfo.LunarMonth == 1 && lunarInfo.LunarDay == 3)
                        map[date] = "春節";
                    else if (lunarInfo.LunarMonth == 5 && lunarInfo.LunarDay == 5)
                        map[date] = "端午節";
                    else if (lunarInfo.LunarMonth == 8 && lunarInfo.LunarDay == 15)
                        map[date] = "中秋節";

                    // 除夕 = the day before lunar 1/1
                    var tomorrow = date.AddDays(1);
                    if (tomorrow.Year >= year)
                    {
                        var tmr = GetLunarInfo(tomorrow);
                        if (!tmr.IsLeapMonth && tmr.LunarMonth == 1 && tmr.LunarDay == 1)
                        {
                            map[date] = "農曆除夕";
                        }
                    }
                }

                return map;
            }

            private LunarInfo GetLunarInfo(DateTime date)
            {
                int lunarYear = _lunar.GetYear(date);
                int rawMonth = _lunar.GetMonth(date);
                int leapMonth = _lunar.GetLeapMonth(lunarYear);

                bool isLeapMonth = false;
                int normalizedMonth;

                if (leapMonth == 0)
                {
                    normalizedMonth = rawMonth;
                }
                else if (rawMonth < leapMonth)
                {
                    normalizedMonth = rawMonth;
                }
                else if (rawMonth == leapMonth)
                {
                    normalizedMonth = rawMonth - 1;
                    isLeapMonth = true;
                }
                else
                {
                    normalizedMonth = rawMonth - 1;
                }

                int lunarDay = _lunar.GetDayOfMonth(date);

                string lunarLabel;

           
                //每逢初一才顯示月名
                lunarLabel = (lunarDay == 1) ? 
                    GetChineseMonthLabel(normalizedMonth, isLeapMonth) : 
                    GetChineseDayLabel(lunarDay);

                string? festivalHint = GetFestivalHint(normalizedMonth, lunarDay, isLeapMonth, date);

                return new LunarInfo
                {
                    LunarMonth = normalizedMonth,
                    LunarDay = lunarDay,
                    IsLeapMonth = isLeapMonth,
                    LunarLabel = lunarLabel,
                    FestivalHint = festivalHint
                };
            }

            private string? GetFestivalHint(int lunarMonth, int lunarDay, bool isLeapMonth, DateTime date)
            {
                if (!isLeapMonth)
                {
                    if (lunarMonth == 1 && lunarDay >= 1 && lunarDay <= 3)
                        return "春節";

                    if (lunarMonth == 5 && lunarDay == 5)
                        return "端午節";

                    if (lunarMonth == 8 && lunarDay == 15)
                        return "中秋節";
                }

                // 除夕 hint
                var tomorrow = date.AddDays(1);
                int tomorrowLunarYear = _lunar.GetYear(tomorrow);
                int tomorrowRawMonth = _lunar.GetMonth(tomorrow);
                int tomorrowLeapMonth = _lunar.GetLeapMonth(tomorrowLunarYear);

                bool tomorrowIsLeap = false;
                int tomorrowNormalizedMonth;

                if (tomorrowLeapMonth == 0)
                {
                    tomorrowNormalizedMonth = tomorrowRawMonth;
                }
                else if (tomorrowRawMonth < tomorrowLeapMonth)
                {
                    tomorrowNormalizedMonth = tomorrowRawMonth;
                }
                else if (tomorrowRawMonth == tomorrowLeapMonth)
                {
                    tomorrowNormalizedMonth = tomorrowRawMonth - 1;
                    tomorrowIsLeap = true;
                }
                else
                {
                    tomorrowNormalizedMonth = tomorrowRawMonth - 1;
                }

                int tomorrowLunarDay = _lunar.GetDayOfMonth(tomorrow);

                if (!tomorrowIsLeap && tomorrowNormalizedMonth == 1 && tomorrowLunarDay == 1)
                    return "農曆除夕";

                return null;
            }

            private static string GetChineseMonthLabel(int month, bool isLeapMonth, bool showMonth = false)
            {
         
                string[] monthNames =
                {
                    "", "正月", "二月", "三月", "四月", "五月", "六月",
                    "七月", "八月", "九月", "十月", "冬月", "臘月"
                };

                var name = month >= 1 && month <= 12 ? monthNames[month] : $"{month}月";
                return isLeapMonth ? $"閏{name}" : name;
            }

            private static string GetChineseDayLabel(int day)
            {
                return day switch
                {
                    1 => "初一",
                    2 => "初二",
                    3 => "初三",
                    4 => "初四",
                    5 => "初五",
                    6 => "初六",
                    7 => "初七",
                    8 => "初八",
                    9 => "初九",
                    10 => "初十",
                    11 => "十一",
                    12 => "十二",
                    13 => "十三",
                    14 => "十四",
                    15 => "十五",
                    16 => "十六",
                    17 => "十七",
                    18 => "十八",
                    19 => "十九",
                    20 => "二十",
                    21 => "廿一",
                    22 => "廿二",
                    23 => "廿三",
                    24 => "廿四",
                    25 => "廿五",
                    26 => "廿六",
                    27 => "廿七",
                    28 => "廿八",
                    29 => "廿九",
                    30 => "三十",
                    _ => day.ToString(CultureInfo.InvariantCulture)
                };
            }

            private static bool TryParseYmdLoose(string s, out DateTime dt)
            {
                var formats = new[] { "yyyy-M-d", "yyyy-MM-dd" };
                return DateTime.TryParseExact(
                    s,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out dt);
            }

            private sealed class LunarInfo
            {
                public int LunarMonth { get; set; }
                public int LunarDay { get; set; }
                public bool IsLeapMonth { get; set; }
                public string LunarLabel { get; set; } = "";
                public string? FestivalHint { get; set; }
            }
        }
    }
}