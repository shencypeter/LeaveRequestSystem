using BioMedDocManager.Extensions;
using BioMedDocManager.Factory;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers
{
    
    public class CIssueTablesController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        public const string PageName = "表單發行";
        public const string InitSort = nameof(IssueTable.OriginalDocNo);
        public const string InitSortHistory = nameof(DocControlMaintable.InTime);

        // ===== 主清單表頭 =====
        public Dictionary<string, string> TableHeaders =
            TableHeaderFactory.Build<IssueTable>(
                includeRowNum: true,
                onlyProps: new[] { "Name", "IssueDatetime", "OriginalDocNo", "DocVer" }
            );

        // ===== History 表頭 =====
        public Dictionary<string, string> TableHeadersHistory =
            TableHeaderFactory.Build<DocControlMaintable>(
                includeRowNum: true,
                onlyProps: new[] { "Purpose", "DateTime", "InTime", "UnuseTime", "DocStatus" }
            );

        // ===========================
        // Index
        // ===========================        
        public async Task<IActionResult> Index(int? PageSize, int? PageNumber, CancellationToken ct)
        {
            var queryModel = GetSessionQueryModel<FormQueryModel>(SessionKey);

            if (PageSize.HasValue) queryModel.PageSize = PageSize.Value;
            if (PageNumber.HasValue) queryModel.PageNumber = PageNumber.Value;

            queryModel.OrderBy ??= nameof(IssueTable.IssueDatetime);
            queryModel.SortDir ??= "desc";

            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示清單頁");

            return await BuildQueryDocs(queryModel, ct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FormQueryModel queryModel)
        {
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "送出查詢");
            return RedirectToAction(nameof(Index));
        }

        // ===========================
        // Details
        // ===========================
        public async Task<IActionResult> Details(string DocNo, string DocVer)
        {
            if (string.IsNullOrWhiteSpace(DocNo) || string.IsNullOrWhiteSpace(DocVer))
                return NotFound();

            var entity = await _context.IssueTables.AsNoTracking()
                .FirstOrDefaultAsync(x => x.OriginalDocNo == DocNo && x.DocVer == DocVer);

            if (entity == null) return NotFound();

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁");
            return View(entity);
        }

        // ===========================
        // NewVersion
        // ===========================        
        public async Task<IActionResult> NewVersion(string? DocNo, string? DocVer)
        {
            IssueTable model = new();

            if (!string.IsNullOrWhiteSpace(DocNo) && !string.IsNullOrWhiteSpace(DocVer))
            {
                if (!IsLatest(DocNo, DocVer))
                {
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示發行新版頁", "錯誤，OriginalDocNo及DocVer非最新版");
                    return NotFound();
                }

                model = await _context.IssueTables
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OriginalDocNo == DocNo && x.DocVer == DocVer)
                    ?? throw new InvalidOperationException();

                var (major, minor) = GetNextDocVersionsNoReserve(DocVer);
                ViewBag.NextMajorVersion = major;
                ViewBag.NextMinorVersion = minor;
            }
            else
            {
                ViewBag.NextMajorVersion = "1.0";
                ViewBag.NextMinorVersion = "";
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示發行新版頁");

            return View(model);
        }

        /// <summary>
        /// 表單發行-新版儲存
        /// </summary>
        /// <param name="model">資料</param>
        /// <param name="nextVersion">新版本(因下拉式選單要個別處理)</param>
        /// <param name="mockFileUpload">新版本檔案</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IssueTable model, string nextVersion, IFormFile? FileUpload)
        {
            QueryableExtensions.TrimStringProperties(model);
            QueryableExtensions.TrimStringProperties(nextVersion);

            ModelState.Remove(nameof(IssueTable.DocVer));

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(nextVersion))
            {
                TempData["_JSShowAlert"] = "資料不完整";
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "發行新版頁儲存", "錯誤，必填資料未填寫");
                return RedirectToAction(nameof(Index));
            }

            var latest = await _context.IssueTables
                .Where(x => x.OriginalDocNo == model.OriginalDocNo)
                .OrderByDescending(x => x.IssueDatetime)
                .FirstOrDefaultAsync();

            if (latest != null)
            {
                if (!IsLatest(latest.OriginalDocNo!, latest.DocVer!))
                {
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "發行新版頁儲存", "錯誤，OriginalDocNo不存在 或 DocVer不存在");
                    return NotFound();
                }

                var (major, minor) = GetNextDocVersionsNoReserve(latest.DocVer!);
                if (nextVersion != major && nextVersion != minor)
                {
                    TempData["_JSShowAlert"] = "版號錯誤";
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "發行新版頁儲存", "錯誤，版號錯誤");
                    return RedirectToAction(nameof(Index));
                }

                model.DocVer = nextVersion;
            }
            else
            {
                model.DocVer = "1.0";
            }

            if (FileUpload != null && FileUpload.Length > 0)
            {
                if (!IsValidFileExtension(FileUpload.FileName))
                {
                    TempData["_JSShowAlert"] = "檔案格式錯誤";
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "發行新版頁儲存", "錯誤，檔案格式錯誤");
                    return RedirectToAction(nameof(Index));
                }

                var ext = SaveFormFile(FileUpload, model);
                if (ext == null)
                {
                    TempData["_JSShowAlert"] = "檔案儲存失敗";
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "發行新版頁儲存", "錯誤，檔案儲存失敗");
                    return RedirectToAction(nameof(Index));
                }

                model.FileExtension = ext;
            }

            _context.IssueTables.Add(model);
            await _context.SaveChangesAsync();

            TempData["_JSShowSuccess"] = "發行成功";

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "發行新版頁儲存", "儲存成功");

            return RedirectToAction(nameof(Index));
        }

        // ===========================
        // Edit
        // ===========================
        public async Task<IActionResult> Edit(string DocNo, string DocVer)
        {
            var entity = await _context.IssueTables
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OriginalDocNo == DocNo && x.DocVer == DocVer);

            return entity == null ? NotFound() : View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string DocNo, string DocVer, IssueTable model, IFormFile? FileUpload)
        {
            var entity = await _context.IssueTables
                .FirstOrDefaultAsync(x => x.OriginalDocNo == DocNo && x.DocVer == DocVer);

            if (entity == null) return NotFound();

            if (FileUpload != null && FileUpload.Length > 0)
            {
                if (!IsValidFileExtension(FileUpload.FileName))
                {
                    TempData["_JSShowAlert"] = "檔案格式錯誤";
                    return RedirectToAction(nameof(Index));
                }

                var ext = SaveFormFile(FileUpload, entity);
                if (ext == null)
                {
                    TempData["_JSShowAlert"] = "檔案儲存失敗";
                    return RedirectToAction(nameof(Index));
                }

                entity.FileExtension = ext;
            }

            entity.Name = model.Name;
            entity.IssueDatetime = model.IssueDatetime;

            await _context.SaveChangesAsync();
            TempData["_JSShowSuccess"] = "編輯成功";

            return RedirectToAction(nameof(Index));
        }

        // ===========================
        // Delete
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Delete(string DocNo, string DocVer)
        {
            if (!IsLatest(DocNo, DocVer)) return NotFound();

            var entity = await _context.IssueTables
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OriginalDocNo == DocNo && x.DocVer == DocVer);

            return entity == null ? NotFound() : View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string DocNo, string DocVer)
        {
            if (!IsLatest(DocNo, DocVer)) return NotFound();

            var entity = await _context.IssueTables
                .FirstOrDefaultAsync(x => x.OriginalDocNo == DocNo && x.DocVer == DocVer);

            if (entity == null) return NotFound();

            bool used = await _context.DocControlMaintables
                .AnyAsync(d => d.OriginalDocNo == DocNo && d.DocVer == DocVer);

            if (used)
            {
                TempData["_JSShowAlert"] = "已有領用紀錄，無法刪除";
                return RedirectToAction(nameof(Index));
            }

            RenameDeleteFormFile(entity);
            _context.IssueTables.Remove(entity);
            await _context.SaveChangesAsync();

            TempData["_JSShowSuccess"] = "刪除成功";
            return RedirectToAction(nameof(Index));
        }

        // ===========================
        // History
        // ===========================
        public async Task<IActionResult> History(
            string DocNo,
            string DocVer,
            string? OrderBy,
            string? SortDir,
            int? PageSize,
            int? PageNumber,
            CancellationToken ct)
        {

            var formIssue = await _context.IssueTables.FirstOrDefaultAsync(m => m.OriginalDocNo == DocNo && m.DocVer == DocVer);

            if (formIssue == null)
            {
                return NotFound();
            }

            // Part 1：表單基本資料
            ViewData["formIssue"] = formIssue;

            // Part 2：入/出庫、註銷明細
            var queryModel = GetSessionQueryModel<FormQueryModel>(SessionKey);

            queryModel.OrderBy = OrderBy ?? InitSortHistory;
            queryModel.SortDir = SortDir ?? "asc";

            if (PageSize.HasValue)
            {
                queryModel.PageSize = PageSize.Value;
            }

            if (PageNumber.HasValue)
            {
                queryModel.PageNumber = PageNumber.Value;
            }

            IQueryable<DocControlMaintable> q =
                _context.DocControlMaintables
                    .Include(d => d.Person)
                    .AsNoTracking()
                    .Where(d => d.OriginalDocNo == DocNo && d.DocVer == DocVer);

            if (queryModel.OrderBy == nameof(DocControlMaintable.DocStatus))
            {
                q = queryModel.SortDir == "desc"
                    ? q.OrderByDescending(d => d.UnuseTime != null)
                         .ThenByDescending(d => d.InTime != null)
                    : q.OrderBy(d => d.UnuseTime != null)
                         .ThenBy(d => d.InTime != null);
            }
            else
            {
                q = q.OrderByWhitelist(
                    queryModel.OrderBy,
                    queryModel.SortDir,
                    TableHeadersHistory,
                    nameof(DocControlMaintable.DateTime)
                );
            }

            var (list, total) = await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

            // 讓 NotMapped 計算屬性可以用多語系 Loc.T(...)
            //範例資料不顯示多語系 list.WithLoc(_loc);

            var rows = BuildRows(
                list,
                TableHeadersHistory,
                queryModel.PageNumber,
                queryModel.PageSize,
                KeyMode.PropertyName,
                true
            );


            ViewData["totalCount"] = total;
            ViewData["tableHeaders"] = TableHeadersHistory;
            return View(rows);
        }

        // ===========================
        // BuildQueryDocs（主清單）
        // ===========================

        /// <summary>
        /// 儲存表單檔案到指定路徑
        /// </summary>
        /// <param name="file">上傳的檔案 (IFormFile)</param>
        /// <param name="model">IssueTable 物件，用於命名與寫入副檔名</param>
        /// <returns>成功儲存後的完整檔案路徑；若失敗則回傳 null</returns>
        [NonAction]
        protected string SaveFormFile(IFormFile file, IssueTable model)
        {
            if (file == null || file.Length == 0 || model == null)
                return null;

            try
            {
                // 取得儲存路徑
                var savePath = GetFormPath();

                // 確保資料夾存在
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                // 取得副檔名與儲存檔名
                var fileExt = Path.GetExtension(file.FileName); // e.g., ".docx"

                // 更新模型的副檔名
                model.FileExtension = fileExt.TrimStart('.'); // e.g., "docx"

                var fileName = $"{model.OriginalDocNo}(v{model.DocVer}).{model.FileExtension}";

                // 組成完整路徑
                var fullPath = Path.Combine(savePath, fileName);

                // 儲存檔案
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                return model.FileExtension;
            }
            catch (Exception ex)
            {
                // 這裡可以加上 log 或錯誤處理
                Console.WriteLine("檔案儲存失敗：" + ex.Message);
                return "";
            }
        }


        // ---------- 查詢清單（主畫面） ----------
        [NonAction]
        public async Task<IActionResult> BuildQueryDocs(FormQueryModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            // 防止 History 的 DocStatus 排序殘留到這支
            if (string.Equals(queryModel.OrderBy, nameof(DocControlMaintable.DocStatus), StringComparison.OrdinalIgnoreCase))
            {
                queryModel.OrderBy = InitSort;
                queryModel.SortDir ??= "asc";
            }

            // 白名單排序欄位
            FilterOrderBy(queryModel, TableHeaders, InitSort);

            // 1) 先做基本篩選（IssueTable）
            IQueryable<IssueTable> baseQ = _context.IssueTables.AsNoTracking();

            // 表單編號（FormNo → OriginalDocNo, LIKE）
            if (!string.IsNullOrWhiteSpace(queryModel.FormNo))
            {
                var s = $"%{queryModel.FormNo.Trim()}%";
                baseQ = baseQ.Where(t => EF.Functions.Like(t.OriginalDocNo!, s));
            }

            // 表單名稱（DocName → Name, LIKE）
            if (!string.IsNullOrWhiteSpace(queryModel.DocName))
            {
                var s = $"%{queryModel.DocName.Trim()}%";
                baseQ = baseQ.Where(t => EF.Functions.Like(t.Name!, s));
            }

            // 表單版次（LIKE）
            if (!string.IsNullOrWhiteSpace(queryModel.DocVer))
            {
                var s = $"%{queryModel.DocVer.Trim()}%";
                baseQ = baseQ.Where(t => EF.Functions.Like(t.DocVer!, s));
            }

            // 發行日期：同一天
            if (queryModel.IssueDate.HasValue)
            {
                var d0 = queryModel.IssueDate.Value.Date;
                var d1 = d0.AddDays(1);
                baseQ = baseQ.Where(t => t.IssueDatetime >= d0 && t.IssueDatetime < d1);
            }

            // 2) 算出每個 OriginalDocNo 的「最大版本 key」
            //    key = AAA.BBB （A/B 都補零到 3 碼），ex: 1.2 → 001.002
            var latestKeyPerDoc =
                _context.IssueTables
                    .AsNoTracking()
                    .Where(t => t.OriginalDocNo != null && t.DocVer != null)
                    .Select(t => new
                    {
                        t.OriginalDocNo,
                        t.DocVer,
                        Dot = t.DocVer!.IndexOf(".")
                    })
                    .Select(x => new
                    {
                        x.OriginalDocNo,
                        A = x.Dot > 0 ? x.DocVer!.Substring(0, x.Dot) : x.DocVer!,
                        B = x.Dot > 0 ? x.DocVer!.Substring(x.Dot + 1) : "0"
                    })
                    .Select(x => new
                    {
                        x.OriginalDocNo,
                        Key =
                            ("000" + x.A).Substring(("000" + x.A).Length - 3, 3) + "." +
                            ("000" + x.B).Substring(("000" + x.B).Length - 3, 3)
                    })
                    .GroupBy(x => x.OriginalDocNo)
                    .Select(g => new
                    {
                        OriginalDocNo = g.Key,
                        LatestKey = g.Max(v => v.Key)
                    });

            // 3) join 回 baseQ，算出當前版本的 key，判斷是不是最新版
            var q =
                from t in baseQ
                join lk in latestKeyPerDoc
                    on t.OriginalDocNo equals lk.OriginalDocNo into lkJoin
                from lk in lkJoin.DefaultIfEmpty()
                let dot = (t.DocVer ?? "").IndexOf(".")
                let a = dot > 0 ? t.DocVer!.Substring(0, dot) : (t.DocVer ?? "")
                let b = dot > 0 ? t.DocVer!.Substring(dot + 1) : "0"
                let thisKey =
                    ("000" + a).Substring(("000" + a).Length - 3, 3) + "." +
                    ("000" + b).Substring(("000" + b).Length - 3, 3)
                select new IssueTableListIssueTableViewModel
                {
                    Name = t.Name,
                    IssueDatetime = t.IssueDatetime,
                    OriginalDocNo = t.OriginalDocNo,
                    DocVer = t.DocVer,
                    FileExtension = t.FileExtension,
                    IsLatest = (lk != null && lk.LatestKey == thisKey) ? 1 : 0
                };

            // 4) 白名單排序（注意 TableHeaders 的 key 要對應 ViewModel 的屬性名）
            q = q.OrderByWhitelist(
                queryModel.OrderBy,
                queryModel.SortDir,
                TableHeaders,
                tiebreakerProperty: nameof(IssueTableListIssueTableViewModel.OriginalDocNo)
            );

            // 5) 分頁 + 總筆數
            var (entityList, totalCount) =
                await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

            // 6) BuildRows：payloadProps 把 IsLatest 帶給 View 用來決定是否顯示「發行新版 / 刪除」按鈕
            var rows = BuildRows(
                entities: entityList,
                tableHeaders: TableHeaders,
                pageNumber: queryModel.PageNumber,
                pageSize: queryModel.PageSize,
                keyMode: KeyMode.PropertyName,
                includeRowNum: true,
                payloadProps: new[] { nameof(IssueTableListIssueTableViewModel.IsLatest) }
            );

            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(rows);
        }


        // ===========================
        // IsLatest
        // ===========================
        public bool IsLatest(string originalDocNo, string docVer)
        {
            if (string.IsNullOrWhiteSpace(originalDocNo) || string.IsNullOrWhiteSpace(docVer))
                return false;

            var latest = _context.IssueTables
                .Where(t => t.OriginalDocNo == originalDocNo && t.DocVer != null)
                .Select(t => t.DocVer!)
                .AsEnumerable()
                .OrderByDescending(v => v, StringComparer.Ordinal)
                .FirstOrDefault();

            return string.Equals(latest, docVer, StringComparison.OrdinalIgnoreCase);
        }
    }
}
