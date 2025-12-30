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
    /// 角色管理
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    
    public class RoleController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "角色管理";

        /// <summary>
        /// 預設排序依據
        /// </summary>
        public const string InitSort = "RoleGroup";

        /// <summary>
        /// 清單表頭設定
        /// </summary>
        public Dictionary<string, string> TableHeaders = TableHeaderFactory.Build<Role>(
            includeRowNum: true,
            onlyProps: new[]
            {
                "RoleGroup",
                "RoleCode",
                "CreatedAt",
                "UpdatedAt"
            }
        );

        // ======================= Index =======================
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber, CancellationToken ct)
        {
            var queryModel = GetSessionQueryModel<RoleQueryViewModel>();

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

            return await BuildQueryRole(queryModel, ct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RoleQueryViewModel queryModel)
        {
            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Create =======================
        public async Task<IActionResult> Create()
        {
            var model = new Role
            {
                CreatedAt = DateTime.Now
            };

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示新增頁");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role posted)
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

                await _context.Roles.AddAsync(posted);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"角色-{posted.RoleCode} 新增【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"角色-{posted.RoleCode} 新增成功";
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Edit =======================
        public async Task<IActionResult> Edit([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == id);

            if (entity == null)
            {
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int? id, Role posted)
        {
            if (posted == null || id != posted.RoleId)
            {
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            var dbEntity = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (dbEntity == null)
            {
                return NotFound();
            }

            try
            {
                dbEntity.RoleCode = posted.RoleCode?.Trim() ?? string.Empty;
                dbEntity.RoleGroup = posted.RoleGroup?.Trim() ?? string.Empty;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"角色-{dbEntity.RoleCode} 更新【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "更新【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"角色-{dbEntity.RoleCode} 更新成功";
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= EditPermission =======================
        public async Task<IActionResult> EditPermission([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                return NotFound();
            }

            // 啟用中的 Resource
            var resources = await _context.Resources
                .Where(r => r.ResourceIsActive && r.DeletedAt == null)
                .OrderBy(r => r.ResourceKey)
                .AsNoTracking()
                .ToListAsync();

            // 所有 AppAction（照 AppActionOrder）
            var actions = await _context.AppActions
                .Where(a => a.DeletedAt == null)
                .OrderBy(a => a.AppActionOrder)
                .ThenBy(a => a.AppActionCode)
                .AsNoTracking()
                .ToListAsync();

            // 目前這個角色既有的 RolePermission
            var existingPerms = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => new
                {
                    rp.ResourceId,
                    rp.AppActionId
                })
                .ToListAsync();

            var selectedKeys = existingPerms
                .Select(p => $"{p.ResourceId}:{p.AppActionId}")
                .ToList();

            var vm = new RolePermissionEditViewModel
            {
                RoleId = role.RoleId,
                RoleCode = role.RoleCode,
                Resources = resources,
                AppActions = actions,
                SelectedPermissionKeys = selectedKeys
            };

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示權限編輯頁");

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPermission([FromRoute] int? id, RolePermissionEditViewModel posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.RoleId)
            {
                return NotFound();
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == posted.RoleId);

            if (role == null)
            {
                return NotFound();
            }

            // 1) 解析 SelectedPermissionKeys -> HashSet<(int ResourceId, int AppActionId)>
            var newKeys = new HashSet<(int ResourceId, int AppActionId)>();

            var rawKeys = posted.SelectedPermissionKeys ?? new List<string>();
            foreach (var key in rawKeys.Distinct())
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var parts = key.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    continue;
                }

                if (int.TryParse(parts[0], out var resId) &&
                    int.TryParse(parts[1], out var actId))
                {
                    newKeys.Add((resId, actId));
                }
            }

            try
            {
                // 2) 讀取目前 DB 中此角色的 RolePermission
                var existingPerms = await _context.RolePermissions
                    .Where(rp => rp.RoleId == role.RoleId)
                    .ToListAsync();

                var existingKeySet = existingPerms
                    .Select(p => (p.ResourceId, p.AppActionId))
                    .ToHashSet();

                // 3) 找出要刪除的：DB 有，但勾選已取消
                var toDelete = existingPerms
                    .Where(p => !newKeys.Contains((p.ResourceId, p.AppActionId)))
                    .ToList();

                if (toDelete.Count > 0)
                {
                    _context.RolePermissions.RemoveRange(toDelete);
                }

                // 4) 找出要新增的：勾選有，但 DB 沒有
                var toAddKeys = newKeys
                    .Where(k => !existingKeySet.Contains(k))
                    .ToList();

                foreach (var (resId, actId) in toAddKeys)
                {
                    var rp = new RolePermission
                    {
                        RoleId = role.RoleId,
                        ResourceId = resId,
                        AppActionId = actId
                    };
                    await _context.RolePermissions.AddAsync(rp);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"角色-{role.RoleCode} 權限設定更新【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "權限設定更新【失敗】", msg, true);

                // 失敗就回到 Details 或 Index 都可以，這邊回 Details
                return RedirectToAction(nameof(Details), new { roleId = role.RoleId });
            }

            var successMsg = $"角色-{role.RoleCode} 權限設定已更新";
            TempData["_JSShowSuccess"] = successMsg;

            await _accessLog.NewActionAsync(
                GetLoginUser(),
                "角色管理",
                "權限設定更新成功",
                successMsg
            );


            return RedirectToAction(nameof(Index));
        }

        // ======================= Details =======================
        public async Task<IActionResult> Details([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
                return NotFound();

            var entity = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (entity == null)
                return NotFound();

            // 取有效權限（ResourceIsActive = 1）
            var perms = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Include(rp => rp.Resource)
                .Include(rp => rp.AppAction)
                .Where(rp => rp.Resource != null && rp.Resource.ResourceIsActive)
                .OrderBy(rp => rp.Resource!.ResourceKey)
                .ThenBy(rp => rp.AppAction!.AppActionOrder)
                .ToListAsync();

            // 丟給 ViewBag
            ViewBag.GroupPerms = perms
                .GroupBy(p => new
                {
                    p.Resource!.ResourceId,
                    p.Resource.ResourceKey
                })
                .ToDictionary(
                    g => _loc.T(g.Key.ResourceKey + ".Index.Title"),
                    g => g.ToList()
                );

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁");

            return View(entity);
        }

        // ======================= Delete =======================
        public async Task<IActionResult> Delete([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
                return NotFound();

            var role = await _context.Roles
                .Include(r => r.UserRoles)
                    .ThenInclude(ur => ur.User)
                .Include(r => r.UserGroupRoles)
                    .ThenInclude(ugr => ugr.UserGroup)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
                return NotFound();

            // ==== 使用者明細 ====
            var userList = role.UserRoles
                .Where(ur => ur.User != null)
                .Select(ur => new RoleUsageUserViewModel
                {
                    UserId = ur.UserId,
                    UserAccount = ur.User!.UserAccount,
                    UserFullName = ur.User!.UserFullName
                })
                .ToList();

            // ==== 群組明細 ====
            var groupList = role.UserGroupRoles
                .Where(ugr => ugr.UserGroup != null)
                .Select(ugr => new RoleUsageGroupViewModel
                {
                    UserGroupId = ugr.UserGroupId,
                    UserGroupCode = ugr.UserGroup!.UserGroupCode,
                    UserGroupDescription = ugr.UserGroup!.UserGroupDescription
                })
                .ToList();

            ViewBag.UserUsageList = userList;
            ViewBag.GroupUsageList = groupList;
            ViewBag.HasUsage = (userList.Count + groupList.Count) > 0;

            // ==== 有效權限（ResourceIsActive = 1） ====
            var perms = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Include(rp => rp.Resource)
                .Include(rp => rp.AppAction)
                .Where(rp => rp.Resource != null && rp.Resource.ResourceIsActive)
                .AsNoTracking()
                .ToListAsync();

            // 不在 Controller 先 Group，讓 View 自己分組
            ViewBag.RolePermissions = perms;

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁");

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int? id, Role posted)
        {
            if (posted == null || id != posted.RoleId)
            {
                return NotFound();
            }

            var entity = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == posted.RoleId);

            if (entity == null)
            {
                return NotFound();
            }

            try
            {
                // 再檢查一次是否仍被使用者或群組引用
                var usedByUser = await _context.UserRoles.AnyAsync(ur => ur.RoleId == entity.RoleId);
                var usedByGroup = await _context.UserGroupRoles.AnyAsync(ugr => ugr.RoleId == entity.RoleId);

                if (usedByUser || usedByGroup)
                {
                    var msg = $"角色-{entity.RoleCode} 目前仍被使用者或群組使用，無法刪除。";
                    TempData["_JSShowAlert"] = msg;
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗-角色已被使用】", msg, true);

                    return RedirectToAction(nameof(Index));
                }

                // 先刪除 RolePermissions（不阻擋刪除，但要一併清掉）
                var rolePerms = await _context.RolePermissions
                    .Where(rp => rp.RoleId == entity.RoleId)
                    .ToListAsync();

                if (rolePerms.Count > 0)
                {
                    _context.RolePermissions.RemoveRange(rolePerms);
                }

                // 再刪除 Role 本身
                _context.Roles.Remove(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"角色-{entity.RoleCode} 刪除【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"角色-{entity.RoleCode} 已刪除";
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除成功");

            return RedirectToAction(nameof(Index));
        }


        // ======================= 查詢邏輯 =======================
        [NonAction]
        public async Task<IActionResult> BuildQueryRole(RoleQueryViewModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            FilterOrderBy(queryModel, TableHeaders, InitSort);

            IQueryable<Role> q = _context.Roles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(queryModel.RoleCode))
            {
                var s = $"%{queryModel.RoleCode.Trim()}%";
                q = q.Where(r => EF.Functions.Like(r.RoleCode, s));
            }

            if (!string.IsNullOrWhiteSpace(queryModel.RoleGroup))
            {
                var s = $"%{queryModel.RoleGroup.Trim()}%";
                q = q.Where(r => EF.Functions.Like(r.RoleGroup, s));
            }

            q = q.OrderByWhitelist(
                queryModel.OrderBy,
                queryModel.SortDir,
                TableHeaders,
                tiebreakerProperty: "RoleId"
            );

            var (entities, totalCount) =
                await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

            // 讓 NotMapped 計算屬性可以用多語系 Loc.T(...)
            entities.WithLoc(_loc);

            // 讓 NotMapped 計算屬性可以用多語系 Loc.T(...)
            entities.WithLoc(_loc);

            var result = BuildRows(
                entities: entities,
                tableHeaders: TableHeaders,
                pageNumber: queryModel.PageNumber,
                pageSize: queryModel.PageSize,
                keyMode: KeyMode.PropertyName,
                includeRowNum: true,
                payloadProps: new[] { "RoleId" }
            );

            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }
    }
}
