using BioMedDocManager.Extensions;
using BioMedDocManager.Factory;
using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 使用者群組管理
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">操作紀錄服務</param>
    [Route("[controller]")]
    public class UserGroupController(DocControlContext context, IWebHostEnvironment hostingEnvironment, IAccessLogService accessLog) : BaseController(context, hostingEnvironment)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "使用者群組管理";

        /// <summary>
        /// 預設排序依據
        /// </summary>
        public const string InitSort = "UserGroupName";

        /// <summary>
        /// 清單表頭設定（欄位對應）
        /// </summary>
        public Dictionary<string, string> TableHeaders = TableHeaderFactory.Build<UserGroup>(
            includeRowNum: true,
            onlyProps: new[]
            {
                "UserGroupName",
                "UserGroupDescription",
                "CreatedAt",
                "UpdatedAt"
            }
        );

        /// <summary>
        /// 顯示使用者群組清單（GET）
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber, CancellationToken ct)
        {
            // 從 Session 抓查詢 model
            var queryModel = GetSessionQueryModel<UserGroupQueryViewModel>();

            // QueryString 有 page 參數就覆蓋
            if (PageSize.HasValue)
            {
                queryModel.PageSize = PageSize.Value;
            }
            if (PageNumber.HasValue)
            {
                queryModel.PageNumber = PageNumber.Value;
            }

            // 過濾字串
            QueryableExtensions.TrimStringProperties(queryModel);

            // 存回 Session
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示清單頁");

            return await BuildQueryUserGroup(queryModel, ct);
        }

        /// <summary>
        /// 使用者群組清單頁送出查詢（POST）
        /// </summary>
        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserGroupQueryViewModel queryModel)
        {
            // 過濾字串
            QueryableExtensions.TrimStringProperties(queryModel);

            // 儲存查詢 model 到 Session 中
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            // PRG：轉跳到 GET Index
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示新增群組頁面
        /// </summary>
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var model = new UserGroup
            {
                CreatedAt = DateTime.Now
            };

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示新增頁");

            return View(model);
        }

        /// <summary>
        /// 新增群組（POST）
        /// </summary>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserGroup posted)
        {
            if (posted == null)
            {
                return NotFound();
            }

            // 過濾字串
            QueryableExtensions.TrimStringProperties(posted);

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(posted);
                }

                await context.UserGroups.AddAsync(posted);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var msg = $"使用者群組-{posted.UserGroupName} 資料新增【失敗】!";
                Utilities.WriteExceptionIntoLogFile(msg, ex, this.HttpContext);
                TempData["_JSShowAlert"] = msg;
                await accessLog.NewActionAsync(GetLoginUser(), PageName, "資料新增【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"使用者群組-{posted.UserGroupName} 資料新增成功!";

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁資料新增成功");

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示編輯群組頁面
        /// </summary>
        [HttpGet("Edit/{userGroupId:int}")]
        public async Task<IActionResult> Edit([FromRoute] int? userGroupId)
        {
            if (userGroupId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var group = await context.UserGroups
                .FirstOrDefaultAsync(g => g.UserGroupId == userGroupId);

            if (group == null)
            {
                return NotFound();
            }

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");

            return View(group);
        }

        /// <summary>
        /// 編輯群組（POST）
        /// </summary>
        [HttpPost("Edit/{userGroupId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int? userGroupId, UserGroup posted)
        {
            if (posted == null || userGroupId.GetValueOrDefault() <= 0 || userGroupId != posted.UserGroupId)
            {
                return NotFound();
            }

            // 過濾字串
            QueryableExtensions.TrimStringProperties(posted);

            var dbGroup = await context.UserGroups.FirstOrDefaultAsync(g => g.UserGroupId == posted.UserGroupId);

            if (dbGroup == null)
            {
                return NotFound();
            }

            try
            {
                dbGroup.UserGroupName = posted.UserGroupName?.Trim() ?? string.Empty;
                dbGroup.UserGroupDescription = string.IsNullOrWhiteSpace(posted.UserGroupDescription)
                    ? null
                    : posted.UserGroupDescription.Trim();

                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var msg = $"使用者群組-{dbGroup.UserGroupName} 資料更新【失敗】!";
                Utilities.WriteExceptionIntoLogFile(msg, ex, this.HttpContext);
                TempData["_JSShowAlert"] = msg;
                await accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁資料更新【失敗】", msg, true);

                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"使用者群組-{dbGroup.UserGroupName} 資料更新成功!";

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁資料更新成功");

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示明細頁
        /// </summary>
        [HttpGet("Details/{userGroupId:int}")]
        public async Task<IActionResult> Details([FromRoute] int? userGroupId)
        {
            if (userGroupId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var group = await context.UserGroups
                .Include(g => g.UserGroupMembers)
                .ThenInclude(m => m.User)
                .Include(g => g.UserGroupRoles)
                .ThenInclude(gr => gr.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.UserGroupId == userGroupId);

            if (group == null)
            {
                return NotFound();
            }

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁");

            return View(group);
        }

        /// <summary>
        /// 顯示刪除確認頁
        /// </summary>
        [HttpGet("Delete/{userGroupId:int}")]
        public async Task<IActionResult> Delete([FromRoute] int? userGroupId)
        {
            if (userGroupId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var group = await context.UserGroups
                .Include(g => g.UserGroupMembers)
                .ThenInclude(m => m.User)
                .Include(g => g.UserGroupRoles)
                .ThenInclude(gr => gr.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.UserGroupId == userGroupId);

            if (group == null)
            {
                return NotFound();
            }

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除確認頁");

            return View(group);
        }

        /// <summary>
        /// 確認刪除（軟刪除）
        /// </summary>
        [HttpPost("Delete/{userGroupId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int? userGroupId, UserGroup posted)
        {
            if (posted == null || userGroupId.GetValueOrDefault() <= 0 || userGroupId != posted.UserGroupId)
            {
                return NotFound();
            }

            var group = await context.UserGroups.FirstOrDefaultAsync(g => g.UserGroupId == posted.UserGroupId);

            if (group == null)
            {
                return NotFound();
            }

            try
            {
                // 標記為刪除
                context.UserGroups.Remove(group);

                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var msg = $"使用者群組-{group.UserGroupName} 刪除【失敗】!";
                Utilities.WriteExceptionIntoLogFile(msg, ex, this.HttpContext);
                TempData["_JSShowAlert"] = msg;
                await accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗】", msg, true);

                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"使用者群組-{group.UserGroupName} 已刪除!";

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除成功");

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 建立使用者群組清單查詢 EF
        /// </summary>
        [NonAction]
        public async Task<IActionResult> BuildQueryUserGroup(UserGroupQueryViewModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            // 1) 排序欄位合法化（白名單）
            FilterOrderBy(queryModel, TableHeaders, InitSort);

            // 2) 產生查詢物件
            IQueryable<UserGroup> q = context.UserGroups.AsNoTracking();

            // 3) 條件篩選

            // 群組名稱（模糊）
            if (!string.IsNullOrWhiteSpace(queryModel.UserGroupName))
            {
                var s = $"%{queryModel.UserGroupName.Trim()}%";
                q = q.Where(g => EF.Functions.Like(g.UserGroupName, s));
            }

            // 群組說明（模糊）
            if (!string.IsNullOrWhiteSpace(queryModel.UserGroupDescription))
            {
                var s = $"%{queryModel.UserGroupDescription.Trim()}%";
                q = q.Where(g => g.UserGroupDescription != null &&
                                 EF.Functions.Like(g.UserGroupDescription, s));
            }

            // 4) 排序（用屬性名白名單）
            q = q.OrderByWhitelist(
                queryModel.OrderBy,
                queryModel.SortDir,
                TableHeaders,
                tiebreakerProperty: "UserGroupId"
            );

            // 5) 分頁＋總筆數
            var (entityList, totalCount) =
                await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

            // 6) 轉成你現有表格用的 rows
            var result = BuildRows(
                entities: entityList,
                tableHeaders: TableHeaders,
                pageNumber: queryModel.PageNumber,
                pageSize: queryModel.PageSize,
                keyMode: KeyMode.PropertyName,
                includeRowNum: true,
                payloadProps: new[] { "UserGroupId" } // 每列額外帶主鍵，方便前端操作
            );

            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }
    }
}
