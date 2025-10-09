using BioMedDocManager.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 文件查詢
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    [Authorize(Roles = DocRoleStrings.領用人)]
    public class CFileQueryController(DocControlContext context, IWebHostEnvironment hostingEnvironment) : BaseController(context, hostingEnvironment)
    {

        /// <summary>
        /// 預設排序依據
        /// </summary>
        public const string InitSort = "id_no";

        /// <summary>
        /// 查詢畫面的表頭DB與中文對照 (因為沒有結果也要顯示)
        /// </summary>
        public Dictionary<string, string> TableHeaders = new()
        {
            // 系統用欄位
            { "RowNum", "#" },

            // 使用者相關
            { "person_name","領用人" },
            { "date_time", "領用日期" },
            { "id_no", "文件編號" },
            { "name", "紀錄名稱" },
            { "purpose", "領用目的" },
            { "original_doc_no", "表單編號" },
            { "doc_ver", "表單版次" },
            { "in_time", "入庫日期" },
            { "unuse_time", "註銷日期" },
            { "reject_reason", "註銷原因" },
            { "project_name", "專案代碼" },
            { "is_confidential", "是否機密" },
            { "is_sensitive", "是否機敏" },
        };

        /// <summary>
        /// 顯示文件查詢頁面
        /// </summary>
        /// <param name="PageSize">單頁顯示筆數</param>
        /// <param name="PageNumber">第幾頁</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber)
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

            return await LoadPage(queryModel);
        }

        /// <summary>
        /// 文件查詢頁面送出查詢
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(FormQueryModel queryModel)
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

            // 轉跳到GET方法頁面，顯示查詢內容
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示明細頁
        /// </summary>
        /// <param name="IdNo">文件編號</param>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/Details/{IdNo}")]
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

            return View(formDocControlMaintable);
        }

        /// <summary>
        /// 匯出查詢結果Excel
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <returns>查詢結果Excel檔</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetExcel(FormQueryModel queryModel)
        {
            try
            {
                // 交換文件編號(年月)
                (queryModel.DocNoA, queryModel.DocNoB) = GetOrderedDocNo(queryModel.DocNoA, queryModel.DocNoB);

                // 過濾文字
                QueryableExtensions.TrimStringProperties(queryModel);

                // 查詢SQL
                BuildQueryDocs(queryModel, out var parameters, out var sqlDef);

                // 產生Excel檔
                return await GetExcelFile<FormQueryModel>(queryModel, sqlDef, parameters, TableHeaders, InitSort, "表單查詢");

            }
            catch (FileNotFoundException)
            {
                //查無結果 不提供檔案
                return NotFound();
            }

        }

        /// <summary>
        /// 載入資料與回傳畫面
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <returns></returns>
        private async Task<IActionResult> LoadPage(FormQueryModel queryModel)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            BuildQueryDocs(queryModel, out DynamicParameters parameters, out string sqlDef);
            FilterOrderBy(queryModel, TableHeaders, InitSort);

            // 使用Dapper對查詢進行分頁(Paginate)
            var (items, totalCount) = await context.BySqlGetPagedWithCountAsync<dynamic>(
                sqlDef,
                orderByPart: $" ORDER BY {queryModel.OrderBy} {queryModel.SortDir}",
                queryModel.PageNumber,
                queryModel.PageSize,
                parameters
            );

            // 即使無資料，也要確認標題存在
            List<Dictionary<string, object>> result = items?.Select(item =>
                (item as IDictionary<string, object>)?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            ).ToList() ?? new List<Dictionary<string, object>>();

            // Pass data to ViewData
            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }

        /// <summary>
        /// 查詢SQL
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <param name="parameters">輸出查詢參數</param>
        /// <param name="sqlQuery">輸出查詢SQL</param>
        private static void BuildQueryDocs(FormQueryModel queryModel, out DynamicParameters parameters, out string sqlQuery)
        {

            sqlQuery = $@"
                SELECT
                    u.full_name AS person_name,
                    dc.date_time,
                    dc.id_no,
                    dc.name,
                    dc.purpose,
                    dc.original_doc_no,
                    dc.doc_ver,
                    dc.in_time,
                    dc.unuse_time,
                    dc.reject_reason,
                    dc.project_name,
                    CASE WHEN dc.is_confidential = 1 THEN N'是' WHEN dc.is_confidential = 0 THEN N'否' ELSE NULL END AS is_confidential,
                    CASE WHEN dc.is_sensitive = 1 THEN N'是' WHEN dc.is_sensitive = 0 THEN N'否' ELSE NULL END AS is_sensitive
                FROM doc_control_maintable dc
                LEFT JOIN [user] u
                    ON dc.id=u.username
                WHERE 1=1
            ";
            var whereClauses = new List<string>();
            parameters = new DynamicParameters();

            if (new[] { queryModel.DocNoA, queryModel.DocNoB }.All(s => !string.IsNullOrWhiteSpace(s)))
            {
                string docA = queryModel.DocNoA.Trim();
                string docB = queryModel.DocNoB.Trim();

                // Force valid bounds: extract prefixes (remove serials) and reattach full range
                string prefixA = docA.Substring(0, docA.Length - 3);
                string prefixB = docB.Substring(0, docB.Length - 3);

                string min = string.Compare(prefixA, prefixB, StringComparison.Ordinal) <= 0 ? prefixA : prefixB;
                string max = string.Compare(prefixA, prefixB, StringComparison.Ordinal) > 0 ? prefixA : prefixB;

                string docNoStart = min + "000";
                string docNoEnd = max + "999";

                // 文件編號 (年月)
                whereClauses.Add("dc.id_no BETWEEN @DocNoStart AND @DocNoEnd");
                parameters.Add("DocNoStart", docNoStart);
                parameters.Add("DocNoEnd", docNoEnd);
            }

            // 文件類別
            if (!string.IsNullOrEmpty(queryModel.DocType))
            {
                whereClauses.Add("dc.type LIKE @DocType");
                parameters.Add("DocType", $"%{queryModel.DocType.Trim()}%");
            }

            // 文件狀態(是否為「未入庫註銷文件」)
            if (queryModel.UnFiledAndNotRevoked == "true")
            {
                //checkbox 有打勾
                whereClauses.Add("dc.in_time IS NULL AND unuse_time IS NULL");
            }

            // 表單編號
            if (!string.IsNullOrEmpty(queryModel.OriginalDocNo))
            {
                whereClauses.Add("dc.original_doc_no LIKE @OriginalDocNo");
                parameters.Add("OriginalDocNo", $"%{queryModel.OriginalDocNo.Trim()}%");
            }

            // 表單版次
            if (!string.IsNullOrEmpty(queryModel.DocVer))
            {
                whereClauses.Add("dc.doc_ver LIKE @DocVer");
                parameters.Add("DocVer", $"%{queryModel.DocVer.Trim()}%");
            }

            // 領用人
            if (!string.IsNullOrEmpty(queryModel.Id))
            {
                whereClauses.Add("dc.id LIKE @Id");
                parameters.Add("Id", $"%{queryModel.Id.Trim()}%");
            }

            // 紀錄名稱
            if (!string.IsNullOrEmpty(queryModel.DocName))
            {
                whereClauses.Add("dc.name LIKE @DocName");
                parameters.Add("DocName", $"%{queryModel.DocName.Trim()}%");
            }

            // 文件編號
            if (!string.IsNullOrEmpty(queryModel.DocNo))
            {
                whereClauses.Add("dc.id_no LIKE @DocNo");
                parameters.Add("DocNo", $"%{queryModel.DocNo.Trim()}%");
            }

            // 專案代碼
            if (!string.IsNullOrEmpty(queryModel.ProjectName))
            {
                whereClauses.Add("dc.project_name LIKE @ProjectName");
                parameters.Add("ProjectName", $"%{queryModel.ProjectName.Trim()}%");
            }

            // 領用目的
            if (!string.IsNullOrEmpty(queryModel.Purpose))
            {
                whereClauses.Add("dc.purpose LIKE @Purpose");
                parameters.Add("Purpose", $"%{queryModel.Purpose.Trim()}%");
            }

            // 是否機密
            if (queryModel.IsConfidential != null)
            {
                whereClauses.Add("dc.is_confidential = @IsConfidential");
                parameters.Add("IsConfidential", queryModel.IsConfidential);
            }

            // 是否機敏
            if (queryModel.IsSensitive != null)
            {
                whereClauses.Add("dc.is_sensitive = @IsSensitive");
                parameters.Add("IsSensitive", queryModel.IsSensitive);
            }


            // Add whereClauses to the SQL query if they exist
            if (whereClauses.Any())
            {
                sqlQuery += " AND " + string.Join(" AND ", whereClauses);
            }

        }





    }
}
