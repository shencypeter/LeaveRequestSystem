using BioMedDocManager.Extensions;
using BioMedDocManager.Factory;
using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 系統動作管理
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    
    public class AppActionController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "系統動作管理";

        /// <summary>
        /// 預設排序依據
        /// </summary>
        public const string InitSort = "AppActionOrder";

        /// <summary>
        /// 清單表頭設定
        /// </summary>
        public Dictionary<string, string> TableHeaders = TableHeaderFactory.Build<AppAction>(
            includeRowNum: true,
            onlyProps: new[]
            {
                "AppActionCode",
                "AppActionDisplayName",
                "AppActionOrder",
                "CreatedAt",
                "UpdatedAt"
            }
        );

        // ======================= Index（清單頁） =======================
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber, CancellationToken ct)
        {
            var queryModel = GetSessionQueryModel<AppActionQueryViewModel>();

            if (PageSize.HasValue)
            {
                queryModel.PageSize = PageSize.Value;
            }
            if (PageNumber.HasValue)
            {
                queryModel.PageNumber = PageNumber.Value;
            }

            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示清單頁");

            return await BuildQueryAppAction(queryModel, ct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AppActionQueryViewModel queryModel)
        {
            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Create =======================        
        public async Task<IActionResult> Create()
        {
            var model = new AppAction
            {
                AppActionOrder = 0,
                CreatedAt = DateTime.Now
            };

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示新增頁");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppAction posted)
        {
            if (posted == null)
            {
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(posted);
                }

                await _context.AppActions.AddAsync(posted);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("AppAction.Create.Title") + "-" + posted.AppActionCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("AppAction.Create.Title") + "-" + posted.AppActionCode + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Edit =======================
        public async Task<IActionResult> Edit([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await _context.AppActions
                .FirstOrDefaultAsync(a => a.AppActionId == id);

            if (entity == null)
            {
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute]  long? id, AppAction posted)
        {
            if (posted == null || id != posted.AppActionId)
            {
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            var dbEntity = await _context.AppActions
                .FirstOrDefaultAsync(a => a.AppActionId == id);

            if (dbEntity == null)
            {
                return NotFound();
            }

            try
            {
                dbEntity.AppActionCode = posted.AppActionCode?.Trim() ?? "";
                dbEntity.AppActionOrder = posted.AppActionOrder;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("AppAction.Edit.Title") + "-" + dbEntity.AppActionCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "更新【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("AppAction.Edit.Title") + "-" + dbEntity.AppActionCode + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Details =======================
        public async Task<IActionResult> Details([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await _context.AppActions
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AppActionId == id);

            if (entity == null)
            {
                return NotFound();
            }

            // 找出這個 AppAction 的 RolePermission (只關心 Resource + Role)
            var rolePerms = await _context.RolePermissions
                .Where(rp => rp.AppActionId == entity.AppActionId)
                .Include(rp => rp.Resource)
                .Include(rp => rp.Role)
                .AsNoTracking()
                .ToListAsync();

            var hasAnyRolePermission = rolePerms.Count > 0;
            ViewBag.HasAnyRolePermission = hasAnyRolePermission;

            // Resource + Role 的組合數
            var pairCount = rolePerms
                .Where(rp => rp.Resource != null && rp.Role != null)
                .Select(rp => new { rp.ResourceId, rp.RoleId })
                .Distinct()
                .Count();

            ViewBag.RolePermissionPairCount = pairCount;

            List<AppActionRoleUsageViewModel> usageList = new();

            if (hasAnyRolePermission)
            {
                usageList = rolePerms
                    .Where(rp => rp.Resource != null && rp.Role != null)
                    .GroupBy(rp => new
                    {
                        rp.ResourceId,
                        rp.Resource!.ResourceKey,
                        rp.Resource!.ResourceDisplayName,
                        rp.RoleId,
                        rp.Role!.RoleGroup,
                        rp.Role!.RoleCode
                    })
                    .Select(g => new AppActionRoleUsageViewModel
                    {
                        ResourceId = g.Key.ResourceId,
                        ResourceKey = g.Key.ResourceKey,
                        ResourceDisplayName = g.Key.ResourceDisplayName,
                        RoleId = g.Key.RoleId,
                        RoleGroup = g.Key.RoleGroup,
                        RoleCode = g.Key.RoleCode
                    })
                    .OrderBy(u => u.RoleGroup)
                    .ThenBy(u => u.RoleCode)
                    .ThenBy(u => u.ResourceKey)
                    .ToList();
            }

            ViewBag.ActionRoleUsageList = usageList;

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示詳細資料");

            return View(entity);
        }

        // ======================= Delete =======================
        public async Task<IActionResult> Delete([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await _context.AppActions
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AppActionId == id);

            if (entity == null)
            {
                return NotFound();
            }

            var rolePerms = await _context.RolePermissions
                .Where(rp => rp.AppActionId == entity.AppActionId)
                .Include(rp => rp.Resource)
                .Include(rp => rp.Role)
                .AsNoTracking()
                .ToListAsync();

            var hasAnyRolePermission = rolePerms.Count > 0;
            ViewBag.HasAnyRolePermission = hasAnyRolePermission;

            var pairCount = rolePerms
                .Where(rp => rp.Resource != null && rp.Role != null)
                .Select(rp => new { rp.ResourceId, rp.RoleId })
                .Distinct()
                .Count();

            ViewBag.RolePermissionPairCount = pairCount;

            List<AppActionRoleUsageViewModel> usageList = new();

            if (hasAnyRolePermission)
            {
                usageList = rolePerms
                    .Where(rp => rp.Resource != null && rp.Role != null)
                    .GroupBy(rp => new
                    {
                        rp.ResourceId,
                        rp.Resource!.ResourceKey,
                        rp.Resource!.ResourceDisplayName,
                        rp.RoleId,
                        rp.Role!.RoleGroup,
                        rp.Role!.RoleCode
                    })
                    .Select(g => new AppActionRoleUsageViewModel
                    {
                        ResourceId = g.Key.ResourceId,
                        ResourceKey = g.Key.ResourceKey,
                        ResourceDisplayName = g.Key.ResourceDisplayName,
                        RoleId = g.Key.RoleId,
                        RoleGroup = g.Key.RoleGroup,
                        RoleCode = g.Key.RoleCode
                    })
                    .OrderBy(u => u.RoleGroup)
                    .ThenBy(u => u.RoleCode)
                    .ThenBy(u => u.ResourceKey)
                    .ToList();
            }

            ViewBag.ActionRoleUsageList = usageList;

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁");

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromRoute]  long? id, AppAction posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.AppActionId)
            {
                return NotFound();
            }

            var entity = await _context.AppActions
                .FirstOrDefaultAsync(a => a.AppActionId == posted.AppActionId);

            if (entity == null)
            {
                return NotFound();
            }

            try
            {
                var hasAnyRolePermission = await _context.RolePermissions
                    .AnyAsync(rp => rp.AppActionId == entity.AppActionId);

                if (hasAnyRolePermission)
                {
                    var msg = _loc.T("AppAction.Delete.InUse.Prefix")
                             + entity.AppActionCode
                             + _loc.T("AppAction.Delete.InUse.Suffix");
                    TempData["_JSShowAlert"] = msg;

                    await _accessLog.NewActionAsync(
                        GetLoginUser(),
                        PageName,
                        "刪除【失敗-仍被角色權限使用】",
                        msg,
                        true
                    );

                    return RedirectToAction(nameof(Index));
                }

                _context.AppActions.Remove(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("AppAction.Delete.Title") + "-" + entity.AppActionCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗】", msg, true);

                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("AppAction.Delete.Title") + "-" + entity.AppActionCode + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= 查詢邏輯 =======================
        [NonAction]
        public async Task<IActionResult> BuildQueryAppAction(AppActionQueryViewModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            FilterOrderBy(queryModel, TableHeaders, InitSort);

            IQueryable<AppAction> q = _context.AppActions.AsNoTracking();

            // 動作名稱
            if (!string.IsNullOrWhiteSpace(queryModel.AppActionCode))
            {
                var s = $"%{queryModel.AppActionCode.Trim()}%";
                q = q.Where(a => EF.Functions.Like(a.AppActionCode, s));
            }

            // 顯示名稱
            if (!string.IsNullOrWhiteSpace(queryModel.AppActionDisplayName))
            {
                var s = $"%{queryModel.AppActionDisplayName.Trim()}%";
                q = q.Where(a => EF.Functions.Like(a.AppActionDisplayName, s));
            }

            q = q.OrderByWhitelist(
                queryModel.OrderBy,
                queryModel.SortDir,
                TableHeaders,
                tiebreakerProperty: "AppActionId"
            );

            var (entities, totalCount) =
                await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

            // 讓 NotMapped 計算屬性可以用多語系 Loc.T(...)
            entities.WithLoc(_loc);

            var result = BuildRows(
                entities: entities,
                tableHeaders: TableHeaders,
                pageNumber: queryModel.PageNumber,
                pageSize: queryModel.PageSize,
                keyMode: KeyMode.PropertyName,
                includeRowNum: true,
                payloadProps: new[] { "AppActionId" }
            );

            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }



    }
}
