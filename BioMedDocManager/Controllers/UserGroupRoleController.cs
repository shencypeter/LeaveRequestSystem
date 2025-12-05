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
    /// 使用者群組角色設定
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">操作紀錄服務</param>
    //[Authorize(Roles = AppSettings.AdminRoleStrings.系統管理者)]
    [Route("[controller]")]
    public class UserGroupRoleController(
        DocControlContext context,
        IWebHostEnvironment hostingEnvironment,
        IAccessLogService accessLog
    ) : BaseController(context, hostingEnvironment)
    {
        /// <summary>
        /// 顯示群組角色設定頁（指定某個 UserGroup）
        /// </summary>
        [HttpGet("Edit/{userGroupId:int}")]
        public async Task<IActionResult> Edit([FromRoute] int? userGroupId)
        {
            if (userGroupId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            // 先抓群組 + 目前已綁定的角色
            var group = await context.UserGroups
                .Include(g => g.UserGroupRoles)
                .ThenInclude(ugr => ugr.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.UserGroupId == userGroupId);

            if (group == null)
            {
                return NotFound();
            }

            // 全部角色（你可以之後在這邊加上狀態過濾，例如只抓啟用中的角色）
            var allRoles = await context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            var selectedRoleIds = group.UserGroupRoles
                .Select(ugr => ugr.RoleId)
                .ToList();

            // ====== 這段是「有效權限」計算 ======
            // 權限：RolePermission + Resource + AppAction
            var permsRaw = await context.RolePermissions
                .Where(rp => selectedRoleIds.Contains(rp.RoleId))
                .Join(context.Resources,
                    rp => rp.ResourceId,
                    res => res.ResourceId,
                    (rp, res) => new { rp, res })
                .Join(context.AppActions, // 如果你那張表叫別名，就改這裡
                    j => j.rp.AppActionId,
                    act => act.AppActionId,
                    (j, act) => new
                    {
                        j.rp.RoleId,
                        j.rp.ResourceId,
                        j.rp.AppActionId,
                        j.res.ResourceKey,
                        j.res.ResourceDisplayName,
                        act.AppActionName,
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
                    p.AppActionName,
                    p.AppActionDisplayName,
                    p.AppActionOrder,
                })
                .Select(g => new PreviewPermissionViewModel
                {
                    ResourceId = g.Key.ResourceId,
                    ResourceKey = g.Key.ResourceKey,
                    ResourceDisplayName = g.Key.ResourceDisplayName,
                    AppActionId = g.Key.AppActionId,
                    AppActionName = g.Key.AppActionName,
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
                UserGroupId = group.UserGroupId,
                UserGroupName = group.UserGroupName,
                SelectedRoleIds = selectedRoleIds,
                AllRoles = allRoles,
                EffectivePermissions= effectivePerms
            };

            await accessLog.NewActionAsync(GetLoginUser(), "使用者群組角色", "顯示群組角色設定頁");

            return View(vm);
        }

        /// <summary>
        /// 更新群組角色設定（批次勾選角色）
        /// </summary>
        [HttpPost("Edit/{userGroupId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int? userGroupId, UserGroupRoleEditViewModel posted)
        {
            if (posted == null || userGroupId.GetValueOrDefault() <= 0 || userGroupId != posted.UserGroupId)
            {
                return NotFound();
            }

            // 重新抓 DB 中的群組 + 目前的角色關聯（追蹤中）
            var group = await context.UserGroups
                .Include(g => g.UserGroupRoles)
                .FirstOrDefaultAsync(g => g.UserGroupId == posted.UserGroupId);

            if (group == null)
            {
                return NotFound();
            }

            // 目前 DB 已有的角色 Id
            var currentRoleIds = group.UserGroupRoles
                .Select(ugr => ugr.RoleId)
                .ToHashSet();

            // 使用者這次送出的角色 Id（可能為 null）
            var wantedRoleIds = (posted.SelectedRoleIds ?? new List<int>())
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
                        UserGroupId = group.UserGroupId,
                        RoleId = roleId
                        // CreatedAt / CreatedBy 可交給 DB default，或你之後在這裡補上
                    };
                    await context.UserGroupRoles.AddAsync(entity);
                }

                // 刪除 UserGroupRole
                if (toRemove.Count > 0)
                {
                    var removeEntities = group.UserGroupRoles
                        .Where(ugr => toRemove.Contains(ugr.RoleId))
                        .ToList();

                    if (removeEntities.Count > 0)
                    {
                        context.UserGroupRoles.RemoveRange(removeEntities);
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var msg = $"使用者群組-{group.UserGroupName} 角色設定更新【失敗】!";
                Utilities.WriteExceptionIntoLogFile(msg, ex, this.HttpContext);
                TempData["_JSShowAlert"] = msg;

                await accessLog.NewActionAsync(
                    GetLoginUser(),
                    "使用者群組角色",
                    "群組角色設定更新【失敗】",
                    msg
                );

                return RedirectToAction(nameof(UserGroupController.Index), "UserGroup");
            }

            TempData["_JSShowSuccess"] = $"使用者群組-{group.UserGroupName} 角色設定更新成功!";

            await accessLog.NewActionAsync(
                GetLoginUser(),
                "使用者群組角色",
                "群組角色設定更新成功"
            );

            return RedirectToAction(nameof(UserGroupController.Index), "UserGroup");
        }

        /// <summary>
        /// 預覽某群組在目前勾選角色下的有效權限變化
        /// </summary>
        [HttpPost("PreviewGroupPermissions")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewGroupPermissions([FromBody] PreviewGroupPermissionsViewModel req)
        {
            if (req == null || req.UserGroupId <= 0)
            {
                return BadRequest();
            }

            var groupId = req.UserGroupId;
            var selectedRoleIds = (req.SelectedRoleIds ?? new()).Distinct().ToList();

            // === 1) 目前 DB 狀態：這個群組原本擁有的角色 ===
            var currentRoleIds = await context.UserGroupRoles
                .Where(ugr => ugr.UserGroupId == groupId)
                .Select(ugr => ugr.RoleId)
                .Distinct()
                .ToListAsync();

            // === 2) 目前 DB 狀態：原本的權限集合（ResourceId, AppActionId） ===
            var currentPermKeys = await context.RolePermissions
                .Where(rp => currentRoleIds.Contains(rp.RoleId))
                .Select(rp => new { rp.ResourceId, rp.AppActionId })
                .Distinct()
                .ToListAsync();

            var currentPermSet = currentPermKeys
                .Select(x => (x.ResourceId, x.AppActionId))
                .ToHashSet();

            // === 3) 這次 checkbox 勾選後的角色 → 權限 ===
            var newRoleIds = selectedRoleIds;

            var permsRaw = await context.RolePermissions
                .Where(rp => newRoleIds.Contains(rp.RoleId))
                .Join(context.Resources,
                    rp => rp.ResourceId,
                    res => res.ResourceId,
                    (rp, res) => new { rp, res })
                .Join(context.AppActions,
                    j => j.rp.AppActionId,
                    act => act.AppActionId,
                    (j, act) => new
                    {
                        j.rp.RoleId,
                        j.rp.ResourceId,
                        j.rp.AppActionId,
                        j.res.ResourceKey,
                        j.res.ResourceDisplayName,
                        act.AppActionName,
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
                    p.AppActionName,
                    p.AppActionDisplayName,
                    p.AppActionOrder,
                })
                .Select(g => new PreviewPermissionViewModel
                {
                    ResourceId = g.Key.ResourceId,
                    ResourceKey = g.Key.ResourceKey,
                    ResourceDisplayName = g.Key.ResourceDisplayName,
                    AppActionId = g.Key.AppActionId,
                    AppActionName = g.Key.AppActionName,
                    AppActionDisplayName = g.Key.AppActionDisplayName,
                    AppActionOrder = g.Key.AppActionOrder,
                    IsNew = !currentPermSet.Contains((g.Key.ResourceId, g.Key.AppActionId))
                })
                .OrderBy(p => p.ResourceDisplayName)
                .ThenBy(p => p.AppActionOrder)
                .ToList();

            return Json(new
            {
                permissions = permDtos
            });
        }



    }

    
}
