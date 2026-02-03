using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 使用者群組角色管理
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    
    public class UserGroupRoleController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "使用者群組角色管理";

        /// <summary>
        /// 顯示群組角色設定頁（指定某個 UserGroup）
        /// </summary>
        public async Task<IActionResult> Edit([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，id小於等於0");
                return NotFound();
            }

            // 先抓群組 + 目前已綁定的角色
            var entity = await _context.UserGroups
                .Include(g => g.UserGroupRoles)
                .ThenInclude(ugr => ugr.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.UserGroupId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，entity為null");
                return NotFound();
            }

            // 全部角色（你可以之後在這邊加上狀態過濾，例如只抓啟用中的角色）
            var allRoles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleCode)
                .ToListAsync();

            var selectedRoleIds = entity.UserGroupRoles
                .Select(ugr => ugr.RoleId)
                .ToList();

            // ====== 這段是「有效權限」計算 ======
            // 權限：RolePermission + Resource + AppAction
            var permsRaw = await _context.RolePermissions
                .Where(rp => selectedRoleIds.Contains(rp.RoleId))
                .Join(_context.Resources,
                    rp => rp.ResourceId,
                    res => res.ResourceId,
                    (rp, res) => new { rp, res })
                .Join(_context.AppActions, // 如果你那張表叫別名，就改這裡
                    j => j.rp.AppActionId,
                    act => act.AppActionId,
                    (j, act) => new
                    {
                        j.rp.RoleId,
                        j.rp.ResourceId,
                        j.rp.AppActionId,
                        j.res.ResourceKey,
                        j.res.ResourceDisplayName,
                        act.AppActionCode,
                        act.AppActionDisplayName,
                        act.AppActionOrder,
                    })
                .ToListAsync();

            // 合併同一 Resource + Action（多角色的權限只要「有」就好）
            var effectivePerms = permsRaw
                .GroupBy(p => new
                {
                    p.ResourceId,
                    p.ResourceKey,
                    p.ResourceDisplayName,
                    p.AppActionId,
                    p.AppActionCode,
                    p.AppActionDisplayName,
                    p.AppActionOrder,
                })
                .Select(g => new PreviewPermissionViewModel
                {
                    ResourceId = g.Key.ResourceId,
                    ResourceKey = g.Key.ResourceKey,
                    ResourceDisplayName = g.Key.ResourceDisplayName,
                    AppActionId = g.Key.AppActionId,
                    AppActionCode = g.Key.AppActionCode,
                    AppActionDisplayName = g.Key.AppActionDisplayName,
                    AppActionOrder = g.Key.AppActionOrder,
                    // 在群組頁這裡不用比較「是不是新」，統一視為目前有效權限
                    IsNew = false
                })
                .OrderBy(p => p.ResourceDisplayName)
                .ThenBy(p => p.AppActionOrder)
                .ToList();


            var vm = new UserGroupRoleEditViewModel
            {
                UserGroupId = entity.UserGroupId,
                UserGroupCode = entity.UserGroupCode,
                SelectedRoleIds = selectedRoleIds,
                AllRoles = allRoles,
                EffectivePermissions = effectivePerms
            };

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示群組角色設定頁");

            return View(vm);
        }

        /// <summary>
        /// 更新群組角色設定（批次勾選角色）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute]  long? id, UserGroupRoleEditViewModel posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.UserGroupId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            // 重新抓 DB 中的群組 + 目前的角色關聯（追蹤中）
            var dbEntity = await _context.UserGroups
                .Include(g => g.UserGroupRoles)
                .FirstOrDefaultAsync(g => g.UserGroupId == posted.UserGroupId);

            if (dbEntity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，dbEntity為null");
                return NotFound();
            }

            // 目前 DB 已有的角色 Id
            var currentRoleIds = dbEntity.UserGroupRoles
                .Select(ugr => ugr.RoleId)
                .ToHashSet();

            // 使用者這次送出的角色 Id（可能為 null）
            var wantedRoleIds = (posted.SelectedRoleIds ?? new List<long>())
                .Distinct()
                .ToHashSet();

            // 需要新增的角色（這次有勾，但 DB 沒有）
            var toAdd = wantedRoleIds
                .Where(id => !currentRoleIds.Contains(id))
                .ToList();

            // 需要刪除的角色（DB 有，但這次沒勾）
            var toRemove = currentRoleIds
                .Where(id => !wantedRoleIds.Contains(id))
                .ToList();

            try
            {
                // 新增 UserGroupRole
                foreach (var roleId in toAdd)
                {
                    var entity = new UserGroupRole
                    {
                        UserGroupId = dbEntity.UserGroupId,
                        RoleId = roleId
                        // CreatedAt / CreatedBy 可交給 DB default，或你之後在這裡補上
                    };
                    await _context.UserGroupRoles.AddAsync(entity);
                }

                // 刪除 UserGroupRole
                if (toRemove.Count > 0)
                {
                    var removeEntities = dbEntity.UserGroupRoles
                        .Where(ugr => toRemove.Contains(ugr.RoleId))
                        .ToList();

                    if (removeEntities.Count > 0)
                    {
                        _context.UserGroupRoles.RemoveRange(removeEntities);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var msg = _loc.T("UserGroupRole.Edit.Title") + "-" + dbEntity.UserGroupCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, this.HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "群組角色設定更新【失敗】", msg);

                return RedirectToAction(nameof(UserGroupController.Index), "UserGroup");
            }

            TempData["_JSShowSuccess"] = _loc.T("UserGroupRole.Edit.Title") + "-" + dbEntity.UserGroupCode + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "群組角色設定更新成功");
            return RedirectToAction(nameof(UserGroupController.Index), "UserGroup");
        }

        /// <summary>
        /// 預覽某群組在目前勾選角色下的有效權限變化
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewPermissions([FromBody] PreviewPermissionsViewModel posted)
        {
            if (posted == null || posted.UserGroupId <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示權限預覽資料(JSON)", "錯誤，posted為null 或 id小於等於0");
                return BadRequest();
            }

            var groupId = posted.UserGroupId;
            var selectedRoleIds = (posted.SelectedRoleIds ?? new()).Distinct().ToList();

            // === 1) 目前 DB 狀態：這個群組原本擁有的角色 ===
            var currentRoleIds = await _context.UserGroupRoles
                .Where(ugr => ugr.UserGroupId == groupId)
                .Select(ugr => ugr.RoleId)
                .Distinct()
                .ToListAsync();

            // === 2) 目前 DB 狀態：原本的權限集合（ResourceId, AppActionId） ===
            var currentPermKeys = await _context.RolePermissions
                .Where(rp => currentRoleIds.Contains(rp.RoleId))
                .Select(rp => new { rp.ResourceId, rp.AppActionId })
                .Distinct()
                .ToListAsync();

            var currentPermSet = currentPermKeys
                .Select(x => (x.ResourceId, x.AppActionId))
                .ToHashSet();

            // === 3) 這次 checkbox 勾選後的角色 → 權限 ===
            var newRoleIds = selectedRoleIds;

            var permsRaw = await _context.RolePermissions
                .Where(rp => newRoleIds.Contains(rp.RoleId))
                .Join(_context.Resources,
                    rp => rp.ResourceId,
                    res => res.ResourceId,
                    (rp, res) => new { rp, res })
                .Join(_context.AppActions,
                    j => j.rp.AppActionId,
                    act => act.AppActionId,
                    (j, act) => new
                    {
                        j.rp.RoleId,
                        j.rp.ResourceId,
                        j.rp.AppActionId,
                        j.res.ResourceKey,
                        j.res.ResourceDisplayName,
                        act.AppActionCode,
                        act.AppActionDisplayName,
                        act.AppActionOrder,
                    })
                .ToListAsync();

            // 合併同一 Resource + Action，並標記 IsNew（原本沒有 → 預覽）
            var permDtos = permsRaw
                .GroupBy(p => new
                {
                    p.ResourceId,
                    p.ResourceKey,
                    p.ResourceDisplayName,
                    p.AppActionId,
                    p.AppActionCode,
                    p.AppActionDisplayName,
                    p.AppActionOrder,
                })
                .Select(g => new PreviewPermissionViewModel
                {
                    ResourceId = g.Key.ResourceId,
                    ResourceKey = g.Key.ResourceKey,
                    ResourceDisplayName = g.Key.ResourceDisplayName,
                    AppActionId = g.Key.AppActionId,
                    AppActionCode = g.Key.AppActionCode,
                    AppActionDisplayName = g.Key.AppActionDisplayName,
                    AppActionOrder = g.Key.AppActionOrder,
                    IsNew = !currentPermSet.Contains((g.Key.ResourceId, g.Key.AppActionId))
                })
                .OrderBy(p => p.ResourceDisplayName)
                .ThenBy(p => p.AppActionOrder)
                .ToList();

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "群組角色有效權限預覽");

            return Json(new
            {
                permissions = permDtos
            });
        }



    }


}
