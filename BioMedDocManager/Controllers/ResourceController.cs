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
    /// 資源管理
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>

    public class ResourceController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {

        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "資源管理";

        /// <summary>
        /// 預設排序依據
        /// </summary>
        public const string InitSort = "ResourceKey";

        /// <summary>
        /// 清單表頭設定
        /// </summary>
        public Dictionary<string, string> TableHeaders = TableHeaderFactory.Build<Resource>(
            includeRowNum: true,
            onlyProps: new[]
            {
                "ResourceType",
                "ResourceKey",
                "ResourceDisplayName",
                "ResourceIsActiveText",
                "CreatedAt",
                "UpdatedAt"
            }
        );

        // ======================= Index（清單頁） =======================
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber, CancellationToken ct)
        {
            var queryModel = GetSessionQueryModel<ResourceQueryViewModel>();

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

            return await BuildQueryResource(queryModel, ct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ResourceQueryViewModel queryModel)
        {
            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Create =======================
        public async Task<IActionResult> Create()
        {
            var model = new Resource
            {
                ResourceIsActive = true,
                CreatedAt = DateTime.Now
            };

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示新增頁");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Resource posted)
        {
            if (posted == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", "錯誤，posted為null");
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            try
            {
                if (!ModelState.IsValid)
                {
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", "錯誤，必填資料未填寫");
                    return View(posted);
                }

                await _context.Resources.AddAsync(posted);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("Resource.Create.Title") + "-" + posted.ResourceKey + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, this.HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("Resource.Create.Title") + "-" + posted.ResourceKey + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Edit =======================
        public async Task<IActionResult> Edit([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Resources.FirstOrDefaultAsync(r => r.ResourceId == id);
            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，entity為null");
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int? id, Resource posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.ResourceId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            var dbEntity = await _context.Resources.FirstOrDefaultAsync(r => r.ResourceId == id);
            if (dbEntity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，dbEntity為null");
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", "錯誤，必填資料未填寫");
                    return View(posted);
                }

                dbEntity.ResourceType = posted.ResourceType?.Trim() ?? "";
                dbEntity.ResourceKey = posted.ResourceKey?.Trim() ?? "";
                dbEntity.ResourceIsActive = posted.ResourceIsActive;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("Resource.Edit.Title") + "-" + dbEntity.ResourceKey + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, this.HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "更新【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("Resource.Edit.Title") + "-" + dbEntity.ResourceKey + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Details =======================
        public async Task<IActionResult> Details([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Resources
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResourceId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁", "錯誤，entity為null");
                return NotFound();
            }

            // 1) 找出這個 Resource 的 RolePermission
            var rolePerms = await _context.RolePermissions
                .Where(rp => rp.ResourceId == entity.ResourceId)
                .Include(rp => rp.Role)
                .AsNoTracking()
                .ToListAsync();

            // 有哪些角色用到這個 Resource
            var roleIds = rolePerms
                .Select(rp => rp.RoleId)
                .Distinct()
                .ToList();

            var hasAnyRolePermission = roleIds.Count > 0;
            ViewBag.HasAnyRolePermission = hasAnyRolePermission;
            ViewBag.RolePermissionCount = roleIds.Count; // 「有幾個角色設定到這個資源」

            List<ResourceGroupUsageViewModel> groupUsage = new();

            if (roleIds.Count > 0)
            {
                // 2a) 有群組的 (UserGroupRoles)
                var withGroup = await _context.UserGroupRoles
                    .Where(ugr => roleIds.Contains(ugr.RoleId))
                    .Include(ugr => ugr.UserGroup)
                    .Include(ugr => ugr.Role)
                    .Select(ugr => new ResourceGroupUsageViewModel
                    {
                        UserGroupId = ugr.UserGroupId,
                        UserGroupCode = ugr.UserGroup!.UserGroupCode,
                        UserGroupDescription = ugr.UserGroup!.UserGroupDescription,
                        RoleId = ugr.RoleId,
                        RoleCode = ugr.Role!.RoleCode,
                        RoleGroup = ugr.Role!.RoleGroup,
                        HasGroup = true
                    })
                    .ToListAsync();

                // 2b) 全部角色（避免沒有群組的角色被漏掉）
                var roles = await _context.Roles
                    .Where(r => roleIds.Contains(r.RoleId))
                    .AsNoTracking()
                    .ToListAsync();

                var roleIdsWithGroup = withGroup
                    .Select(g => g.RoleId)
                    .Distinct()
                    .ToHashSet();

                // 2c) 沒群組的角色
                var withoutGroup = roles
                    .Where(r => !roleIdsWithGroup.Contains(r.RoleId))
                    .Select(r => new ResourceGroupUsageViewModel
                    {
                        UserGroupId = 0,
                        UserGroupCode = null,
                        UserGroupDescription = null,
                        RoleId = r.RoleId,
                        RoleCode = r.RoleCode,
                        RoleGroup = r.RoleGroup,
                        HasGroup = false
                    })
                    .ToList();

                groupUsage = withGroup
                    .Concat(withoutGroup)
                    .Distinct()
                    .OrderBy(g => g.UserGroupCode ?? "未指定群組")
                    .ThenBy(g => g.RoleGroup)
                    .ThenBy(g => g.RoleCode)
                    .ToList();
            }

            ViewBag.ResourceGroupUsageList = groupUsage;

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示詳細資料");

            return View(entity);
        }


        // ======================= Delete =======================
        public async Task<IActionResult> Delete([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Resources
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResourceId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁", "錯誤，entity為null");
                return NotFound();
            }

            // 1) 找出這個 Resource 的 RolePermission
            var rolePerms = await _context.RolePermissions
                .Where(rp => rp.ResourceId == entity.ResourceId)
                .Include(rp => rp.Role)
                .AsNoTracking()
                .ToListAsync();

            // 有哪些角色用到這個 Resource
            var roleIds = rolePerms
                .Select(rp => rp.RoleId)
                .Distinct()
                .ToList();

            var hasAnyRolePermission = roleIds.Count > 0;
            ViewBag.HasAnyRolePermission = hasAnyRolePermission;
            ViewBag.RolePermissionCount = roleIds.Count; // 角色數量

            List<ResourceGroupUsageViewModel> groupUsage = new();

            if (roleIds.Count > 0)
            {
                // 2a) 有群組的角色 (Role LEFT JOIN UserGroupRoles, 這裡先取「有群組」那一側)
                var withGroup = await _context.UserGroupRoles
                    .Where(ugr => roleIds.Contains(ugr.RoleId))
                    .Include(ugr => ugr.UserGroup)
                    .Include(ugr => ugr.Role)
                    .Select(ugr => new ResourceGroupUsageViewModel
                    {
                        UserGroupId = ugr.UserGroupId,
                        UserGroupCode = ugr.UserGroup!.UserGroupCode,
                        UserGroupDescription = ugr.UserGroup!.UserGroupDescription,
                        RoleId = ugr.RoleId,
                        RoleCode = ugr.Role!.RoleCode,
                        RoleGroup = ugr.Role!.RoleGroup,
                        HasGroup = true
                    })
                    .ToListAsync();

                // 2b) 完整取得這些角色（避免有些角色完全沒綁群組）
                var roles = await _context.Roles
                    .Where(r => roleIds.Contains(r.RoleId))
                    .AsNoTracking()
                    .ToListAsync();

                // 有群組的角色 Id 集合
                var roleIdsWithGroup = withGroup
                    .Select(g => g.RoleId)
                    .Distinct()
                    .ToHashSet();

                // 2c) 沒群組的角色 → 顯示「未指定群組」
                var withoutGroup = roles
                    .Where(r => !roleIdsWithGroup.Contains(r.RoleId))
                    .Select(r => new ResourceGroupUsageViewModel
                    {
                        UserGroupId = 0,
                        UserGroupCode = null,
                        UserGroupDescription = null,
                        RoleId = r.RoleId,
                        RoleCode = r.RoleCode,
                        RoleGroup = r.RoleGroup,
                        HasGroup = false
                    })
                    .ToList();

                // 2d) 合併
                groupUsage = withGroup
                    .Concat(withoutGroup)
                    .Distinct() // 避免重複
                    .OrderBy(g => g.UserGroupCode ?? "未指定群組") // 讓有群組的排前面
                    .ThenBy(g => g.RoleGroup)
                    .ThenBy(g => g.RoleCode)
                    .ToList();
            }

            ViewBag.ResourceGroupUsageList = groupUsage;

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁");

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromRoute] int? id, Resource posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.ResourceId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            var entity = await _context.Resources
                .FirstOrDefaultAsync(r => r.ResourceId == posted.ResourceId);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除頁儲存", "錯誤，entity為null");
                return NotFound();
            }

            try
            {
                // 再檢查一次是否仍被 RolePermission 關聯
                var hasAnyRolePermission = await _context.RolePermissions
                    .AnyAsync(rp => rp.ResourceId == entity.ResourceId);

                if (hasAnyRolePermission)
                {
                    var msg = _loc.T("Resource.Delete.Title") + "-" + entity.ResourceKey + _loc.T("Resource.Delete.Blocked.Prefix") + "，" + _loc.T("Resource.Delete.Blocked.CannotDelete");
                    TempData["_JSShowAlert"] = msg;

                    await _accessLog.NewActionAsync(
                        GetLoginUser(),
                        "系統資源管理",
                        "刪除【失敗-仍被角色權限使用】",
                        msg,
                        true
                    );

                    return RedirectToAction(nameof(Index));
                }

                // 沒有任何 RolePermission 使用 → 可刪除
                _context.Resources.Remove(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("Resource.Delete.Title") + "-" + entity.ResourceKey + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗】", msg, true);

                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("Resource.Delete.Title") + "-" + entity.ResourceKey + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= 查詢邏輯 =======================
        [NonAction]
        public async Task<IActionResult> BuildQueryResource(ResourceQueryViewModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            FilterOrderBy(queryModel, TableHeaders, InitSort);

            IQueryable<Resource> q = _context.Resources.AsNoTracking();

            // 資源類型
            if (!string.IsNullOrWhiteSpace(queryModel.ResourceType))
            {
                var s = $"%{queryModel.ResourceType.Trim()}%";
                q = q.Where(r => EF.Functions.Like(r.ResourceType, s));
            }

            // 資源代碼
            if (!string.IsNullOrWhiteSpace(queryModel.ResourceKey))
            {
                var s = $"%{queryModel.ResourceKey.Trim()}%";
                q = q.Where(r => EF.Functions.Like(r.ResourceKey, s));
            }

            // 是否啟用
            if (queryModel.ResourceIsActive.HasValue)
            {
                q = q.Where(r => r.ResourceIsActive == queryModel.ResourceIsActive.Value);
            }

            q = q.OrderByWhitelist(
                queryModel.OrderBy,
                queryModel.SortDir,
                TableHeaders,
                tiebreakerProperty: "ResourceId"
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
                payloadProps: new[] { "ResourceId" }
            );

            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }
    }
}
