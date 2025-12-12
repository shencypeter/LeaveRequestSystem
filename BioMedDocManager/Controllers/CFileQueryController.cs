using BioMedDocManager.Extensions;
using BioMedDocManager.Factory;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 文件查詢
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    [Route("[controller]")]
    public class CFileQueryController(DocControlContext context, IWebHostEnvironment hostingEnvironment, IAccessLogService accessLog) : BaseController(context, hostingEnvironment)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "文件查詢";

        /// <summary>
        /// 預設排序依據
        /// </summary>
        public const string InitSort = "IdNo";

        /// <summary>
        /// 查詢畫面的表頭DB與中文對照 (因為沒有結果也要顯示)，onlyProps可控制哪些欄位要顯示，且照順序顯示）
        /// 欄位名會自動抓Model的[Display(Name="…", ResourceType=…)]（還可自動抓多語系資源字串）
        /// </summary>
        public Dictionary<string, string> TableHeaders = TableHeaderFactory.Build<DocControlMaintable>(
            includeRowNum: true,
            onlyProps: new[] { "PersonName", "DateTime", "IdNo", "Name", "Purpose", "OriginalDocNo", "DocVer", "ProjectName", "InTime", "UnuseTime", "RejectReason", "IsConfidentialText", "IsSensitiveText" }
        );

        /// <summary>
        /// 顯示文件查詢頁面
        /// </summary>
        /// <param name="PageSize">單頁顯示筆數</param>
        /// <param name="PageNumber">第幾頁</param>
        /// <returns></returns>
        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber, CancellationToken ct)
        {
            // 從Session中找出查詢model或建立預設查詢model
            var queryModel = GetSessionQueryModel<FormQueryModel>(SessionKey);

            // 如果query string有帶入page參數，才使用；否則保留Session中的值
            if (PageSize.HasValue)
            {
                queryModel.PageSize = PageSize.Value;
            }
            if (PageNumber.HasValue)
            {
                queryModel.PageNumber = PageNumber.Value;
            }

            // 一進來頁面就先按照文件編號倒序
            queryModel.OrderBy ??= InitSort;
            queryModel.SortDir ??= "desc";

            // 過濾文字
            QueryableExtensions.TrimStringProperties(queryModel);

            // 儲存查詢model到session中
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            // 領用人下拉式選單(List)
            ViewData["DocUser"] = DocAuthors();

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示清單頁");

            return await BuildQueryDocs(queryModel, ct);
        }

        /// <summary>
        /// 文件查詢頁面送出查詢
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <returns></returns>
        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FormQueryModel queryModel)
        {

            // 交換文件編號(年月)
            (queryModel.DocNoA, queryModel.DocNoB) = GetOrderedDocNo(queryModel.DocNoA, queryModel.DocNoB);

            // Normalize both to use proper serial suffix
            if (!string.IsNullOrEmpty(queryModel.DocNoA))
            {
                queryModel.DocNoA = $"{queryModel.DocNoA[..7]}000";
            }

            if (!string.IsNullOrEmpty(queryModel.DocNoB))
            {
                queryModel.DocNoB = $"{queryModel.DocNoB[..7]}999";
            }

            // 儲存查詢model到session中
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            // 轉跳到GET方法頁面，顯示查詢內容
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示明細頁
        /// </summary>
        /// <param name="IdNo">文件編號</param>
        /// <returns></returns>
        [HttpGet("Details/{IdNo}")]
        public async Task<IActionResult> Details([FromRoute] string IdNo)
        {
            if (string.IsNullOrEmpty(IdNo))
            {
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(IdNo);

            var formDocControlMaintable = await context.DocControlMaintables.Include(d => d.Person)
                .FirstOrDefaultAsync(m => m.IdNo == IdNo);

            if (formDocControlMaintable == null)
            {
                return NotFound();
            }

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁");

            return View(formDocControlMaintable);
        }

        /// <summary>
        /// 匯出查詢結果Excel
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <returns>查詢結果Excel檔</returns>
        [HttpPost("Export")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(FormQueryModel queryModel, CancellationToken ct)
        {
            // 不分頁，取全部
            queryModel.PageNumber = 0;
            queryModel.PageSize = 0;

            var vr = await BuildQueryDocs(queryModel, ct) as ViewResult;
            if (vr?.Model is not List<Dictionary<string, object>> rows || rows.Count == 0)
            {
                return NotFound();
            }

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "下載Excel");

            // 交給BaseController統一輸出Excel
            return GetExcelFile(rows, TableHeaders, InitSort, "文件查詢");

        }

        /// <summary>
        /// 建立查詢EF
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <param name="ct">取消token</param>
        /// <returns>查詢結果ViewResult</returns>
        [NonAction]
        public async Task<IActionResult> BuildQueryDocs(FormQueryModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            // 1) 篩選與排序
            FilterOrderBy(queryModel, TableHeaders, InitSort);

            // 2) 產生查詢物件
            IQueryable<DocControlMaintable> q = context.DocControlMaintables.Include(m => m.Person).AsNoTracking();

            // 3) 條件判斷
            // (A) 文件編號 (年月) 範圍：以字串前綴 + 000/999 建上下界
            if (!string.IsNullOrWhiteSpace(queryModel.DocNoA) && !string.IsNullOrWhiteSpace(queryModel.DocNoB))
            {
                string docA = queryModel.DocNoA.Trim();
                string docB = queryModel.DocNoB.Trim();

                // 假設尾碼固定 3 碼序號（000~999）
                if (docA.Length >= 3 && docB.Length >= 3)
                {
                    string prefixA = docA.Substring(0, docA.Length - 3);
                    string prefixB = docB.Substring(0, docB.Length - 3);

                    string min = string.Compare(prefixA, prefixB, StringComparison.Ordinal) <= 0 ? prefixA : prefixB;
                    string max = string.Compare(prefixA, prefixB, StringComparison.Ordinal) > 0 ? prefixA : prefixB;

                    string docNoStart = min + "000";
                    string docNoEnd = max + "999";

                    // 用字串比較（SQL Server 會依欄位定義的 Collation 排序；大多數情境可行）
                    q = q.Where(dc => dc.IdNo.CompareTo(docNoStart) >= 0 && dc.IdNo.CompareTo(docNoEnd) <= 0);
                    // 或者：q = q.Where(dc => dc.IdNo >= docNoStart && dc.IdNo <= docNoEnd);
                }
            }

            // (B) 文件類別（LIKE）
            if (!string.IsNullOrEmpty(queryModel.DocType))
            {
                var s = $"%{queryModel.DocType.Trim()}%";
                q = q.Where(dc => EF.Functions.Like(dc.Type, s));
            }

            // (C) 未入庫且未註銷
            if (string.Equals(queryModel.UnFiledAndNotRevoked, "true", StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(dc => dc.InTime == null && dc.UnuseTime == null);
            }

            // (D) 表單編號（LIKE）
            if (!string.IsNullOrEmpty(queryModel.OriginalDocNo))
            {
                var s = $"%{queryModel.OriginalDocNo.Trim()}%";
                q = q.Where(dc => EF.Functions.Like(dc.OriginalDocNo, s));
            }

            // (E) 表單版次（LIKE）
            if (!string.IsNullOrEmpty(queryModel.DocVer))
            {
                var s = $"%{queryModel.DocVer.Trim()}%";
                q = q.Where(dc => EF.Functions.Like(dc.DocVer, s));
            }

            // (F) 領用人（LIKE）
            if (!string.IsNullOrEmpty(queryModel.Id))
            {
                var s = $"%{queryModel.Id.Trim()}%";
                q = q.Where(dc => EF.Functions.Like(dc.Id, s));
            }

            // (G) 紀錄名稱（LIKE）
            if (!string.IsNullOrEmpty(queryModel.DocName))
            {
                var s = $"%{queryModel.DocName.Trim()}%";
                q = q.Where(dc => EF.Functions.Like(dc.Name, s));
            }

            // (H) 文件編號（LIKE）
            if (!string.IsNullOrEmpty(queryModel.DocNo))
            {
                var s = $"%{queryModel.DocNo.Trim()}%";
                q = q.Where(dc => EF.Functions.Like(dc.IdNo, s));
            }

            // (I) 專案代碼（LIKE）
            if (!string.IsNullOrEmpty(queryModel.ProjectName))
            {
                var s = $"%{queryModel.ProjectName.Trim()}%";
                q = q.Where(dc => EF.Functions.Like(dc.ProjectName, s));
            }

            // (J) 領用目的（LIKE）
            if (!string.IsNullOrEmpty(queryModel.Purpose))
            {
                var s = $"%{queryModel.Purpose.Trim()}%";
                q = q.Where(dc => EF.Functions.Like(dc.Purpose, s));
            }

            // (K) 是否機密 / 是否機敏（等值）
            if (queryModel.IsConfidential != null)
            {
                q = q.Where(dc => dc.IsConfidential == queryModel.IsConfidential);
            }
            if (queryModel.IsSensitive != null)
            {
                q = q.Where(dc => dc.IsSensitive == queryModel.IsSensitive);
            }

            // 4) 白名單排序
            q = q.OrderByWhitelist(
                queryModel.OrderBy,         // 例如 "IdNo"
                queryModel.SortDir,         // "asc"/"desc"
                TableHeaders,               // Key=屬性名, Value=顯示文字
                tiebreakerProperty: "IdNo"  // 第2排序欄位：主鍵
            );

            // 5) 分頁＋總筆數
            var (entityList, totalCount) = await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

            // 6) 轉成 View 要的 List<Dictionary<string, object>>
            var result = BuildRows(
                entities: entityList,
                tableHeaders: TableHeaders,       // Key=屬性名, Value=顯示文字；含 "RowNum" => "#"
                pageNumber: queryModel.PageNumber,
                pageSize: queryModel.PageSize,
                keyMode: KeyMode.PropertyName, // 用屬性名當輸出鍵
                includeRowNum: true
            );

            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }


    }
}
