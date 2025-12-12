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
    [Route("[controller]")]
    public class ResourceController(DocControlContext context, IWebHostEnvironment hostingEnvironment, IAccessLogService accessLog) : BaseController(context, hostingEnvironment)
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

        [HttpGet("")]
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

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示清單頁");

            return await BuildQueryResource(queryModel, ct);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ResourceQueryViewModel queryModel)
        {
            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Create =======================

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var model = new Resource
            {
                ResourceIsActive = true,
                CreatedAt = DateTime.Now
            };

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示新增頁");

            return View(model);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Resource posted)
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

                await context.Resources.AddAsync(posted);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"資源-{posted.ResourceKey} 新增【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, this.HttpContext);
                TempData["_JSShowAlert"] = msg;

                await accessLog.NewActionAsync(GetLoginUser(), PageName, "新增【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"資源-{posted.ResourceKey} 新增成功";
            await accessLog.NewActionAsync(GetLoginUser(), PageName, "新增成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Edit =======================

        [HttpGet("Edit/{resourceId:int}")]
        public async Task<IActionResult> Edit([FromRoute] int? resourceId)
        {
            if (resourceId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await context.Resources.FirstOrDefaultAsync(r => r.ResourceId == resourceId);
            if (entity == null)
            {
                return NotFound();
            }

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");
            return View(entity);
        }

        [HttpPost("Edit/{resourceId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int? resourceId, Resource posted)
        {
            if (posted == null || resourceId != posted.ResourceId)
            {
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            var dbEntity = await context.Resources.FirstOrDefaultAsync(r => r.ResourceId == resourceId);
            if (dbEntity == null)
            {
                return NotFound();
            }

            try
            {
                dbEntity.ResourceType = posted.ResourceType?.Trim() ?? "";
                dbEntity.ResourceKey = posted.ResourceKey?.Trim() ?? "";
                dbEntity.ResourceDisplayName = posted.ResourceDisplayName?.Trim() ?? "";
                dbEntity.ResourceIsActive = posted.ResourceIsActive;

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"系統資源管理-更新【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, this.HttpContext);
                TempData["_JSShowAlert"] = msg;

                await accessLog.NewActionAsync(GetLoginUser(), PageName, "更新【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"系統資源管理-更新成功";
            await accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Details =======================

        [HttpGet("Details/{resourceId:int}")]
        public async Task<IActionResult> Details([FromRoute] int? resourceId)
        {
            if (resourceId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await context.Resources
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResourceId == resourceId);

            if (entity == null)
            {
                return NotFound();
            }

            // 1) 找出這個 Resource 的 RolePermission
            var rolePerms = await context.RolePermissions
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
                var withGroup = await context.UserGroupRoles
                    .Where(ugr => roleIds.Contains(ugr.RoleId))
                    .Include(ugr => ugr.UserGroup)
                    .Include(ugr => ugr.Role)
                    .Select(ugr => new ResourceGroupUsageViewModel
                    {
                        UserGroupId = ugr.UserGroupId,
                        UserGroupName = ugr.UserGroup!.UserGroupName,
                        UserGroupDescription = ugr.UserGroup!.UserGroupDescription,
                        RoleId = ugr.RoleId,
                        RoleName = ugr.Role!.RoleName,
                        RoleGroup = ugr.Role!.RoleGroup,
                        HasGroup = true
                    })
                    .ToListAsync();

                // 2b) 全部角色（避免沒有群組的角色被漏掉）
                var roles = await context.Roles
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
                        UserGroupName = null,
                        UserGroupDescription = null,
                        RoleId = r.RoleId,
                        RoleName = r.RoleName,
                        RoleGroup = r.RoleGroup,
                        HasGroup = false
                    })
                    .ToList();

                groupUsage = withGroup
                    .Concat(withoutGroup)
                    .Distinct()
                    .OrderBy(g => g.UserGroupName ?? "未指定群組")
                    .ThenBy(g => g.RoleGroup)
                    .ThenBy(g => g.RoleName)
                    .ToList();
            }

            ViewBag.ResourceGroupUsageList = groupUsage;

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示詳細資料");

            return View(entity);
        }


        // ======================= Delete =======================

        [HttpGet("Delete/{resourceId:int}")]
        public async Task<IActionResult> Delete([FromRoute] int? resourceId)
        {
            if (resourceId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await context.Resources
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResourceId == resourceId);

            if (entity == null)
            {
                return NotFound();
            }

            // 1) 找出這個 Resource 的 RolePermission
            var rolePerms = await context.RolePermissions
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
                var withGroup = await context.UserGroupRoles
                    .Where(ugr => roleIds.Contains(ugr.RoleId))
                    .Include(ugr => ugr.UserGroup)
                    .Include(ugr => ugr.Role)
                    .Select(ugr => new ResourceGroupUsageViewModel
                    {
                        UserGroupId = ugr.UserGroupId,
                        UserGroupName = ugr.UserGroup!.UserGroupName,
                        UserGroupDescription = ugr.UserGroup!.UserGroupDescription,
                        RoleId = ugr.RoleId,
                        RoleName = ugr.Role!.RoleName,
                        RoleGroup = ugr.Role!.RoleGroup,
                        HasGroup = true
                    })
                    .ToListAsync();

                // 2b) 完整取得這些角色（避免有些角色完全沒綁群組）
                var roles = await context.Roles
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
                        UserGroupName = null,
                        UserGroupDescription = null,
                        RoleId = r.RoleId,
                        RoleName = r.RoleName,
                        RoleGroup = r.RoleGroup,
                        HasGroup = false
                    })
                    .ToList();

                // 2d) 合併
                groupUsage = withGroup
                    .Concat(withoutGroup)
                    .Distinct() // 避免重複
                    .OrderBy(g => g.UserGroupName ?? "未指定群組") // 讓有群組的排前面
                    .ThenBy(g => g.RoleGroup)
                    .ThenBy(g => g.RoleName)
                    .ToList();
            }

            ViewBag.ResourceGroupUsageList = groupUsage;

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁");

            return View(entity);
        }


        [HttpPost("Delete/{resourceId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromRoute] int? resourceId, Resource posted)
        {
            if (posted == null || resourceId.GetValueOrDefault() <= 0 || resourceId != posted.ResourceId)
            {
                return NotFound();
            }

            var entity = await context.Resources
                .FirstOrDefaultAsync(r => r.ResourceId == posted.ResourceId);

            if (entity == null)
            {
                return NotFound();
            }

            try
            {
                // 再檢查一次是否仍被 RolePermission 關聯
                var hasAnyRolePermission = await context.RolePermissions
                    .AnyAsync(rp => rp.ResourceId == entity.ResourceId);

                if (hasAnyRolePermission)
                {
                    var msg = $"資源-{entity.ResourceKey} ({entity.ResourceDisplayName}) 目前仍被角色權限使用，無法刪除。";
                    TempData["_JSShowAlert"] = msg;

                    await accessLog.NewActionAsync(
                        GetLoginUser(),
                        "系統資源管理",
                        "刪除【失敗-仍被角色權限使用】",
                        msg,
                        true
                    );

                    return RedirectToAction(nameof(Index));
                }

                // 沒有任何 RolePermission 使用 → 可刪除
                context.Resources.Remove(entity);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"資源-{entity.ResourceKey} ({entity.ResourceDisplayName}) 刪除【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗】", msg, true);

                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"資源-{entity.ResourceKey} ({entity.ResourceDisplayName}) 已刪除";
            await accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= 查詢邏輯 =======================
        [NonAction]
        public async Task<IActionResult> BuildQueryResource(ResourceQueryViewModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            FilterOrderBy(queryModel, TableHeaders, InitSort);

            IQueryable<Resource> q = context.Resources.AsNoTracking();

            // 資源類型
            if (!string.IsNullOrWhiteSpace(queryModel.ResourceType))
            {
                var s = $"%{queryModel.ResourceType.Trim()}%";
                q = q.Where(r => EF.Functions.Like(r.ResourceType, s));
            }

            // 資源代號
            if (!string.IsNullOrWhiteSpace(queryModel.ResourceKey))
            {
                var s = $"%{queryModel.ResourceKey.Trim()}%";
                q = q.Where(r => EF.Functions.Like(r.ResourceKey, s));
            }

            // 顯示名稱
            if (!string.IsNullOrWhiteSpace(queryModel.ResourceDisplayName))
            {
                var s = $"%{queryModel.ResourceDisplayName.Trim()}%";
                q = q.Where(r => EF.Functions.Like(r.ResourceDisplayName, s));
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
