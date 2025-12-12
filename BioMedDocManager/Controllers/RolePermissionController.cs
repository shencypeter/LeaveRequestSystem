using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 角色權限管理
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    [Route("[controller]")]
    public class RolePermissionController(DocControlContext context, IWebHostEnvironment hostingEnvironment, IAccessLogService accessLog) : BaseController(context, hostingEnvironment)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "角色權限管理";

        [HttpGet("Edit/{roleId:int}")]
        public async Task<IActionResult> Edit([FromRoute] int? roleId)
        {
            if (roleId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var role = await context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                return NotFound();
            }

            // 啟用中的 Resource
            var resources = await context.Resources
                .Where(r => r.ResourceIsActive && r.DeletedAt == null)
                .OrderBy(r => r.ResourceDisplayName)
                .AsNoTracking()
                .ToListAsync();

            // 所有 AppAction（照 AppActionOrder）
            var actions = await context.AppActions
                .Where(a => a.DeletedAt == null)
                .OrderBy(a => a.AppActionOrder)
                .ThenBy(a => a.AppActionName)
                .AsNoTracking()
                .ToListAsync();

            // 目前這個角色既有的 RolePermission
            var existingPerms = await context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
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
                RoleName = role.RoleName,
                Resources = resources,
                AppActions = actions,
                SelectedPermissionKeys = selectedKeys
            };

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示權限編輯頁");

            return View(vm);
        }

        [HttpPost("Edit/{roleId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int? roleId, RolePermissionEditViewModel posted)
        {
            if (posted == null || roleId.GetValueOrDefault() <= 0 || roleId != posted.RoleId)
            {
                return NotFound();
            }

            var role = await context.Roles
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
                var existingPerms = await context.RolePermissions
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
                    context.RolePermissions.RemoveRange(toDelete);
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
                    await context.RolePermissions.AddAsync(rp);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"角色-{role.RoleName} 權限設定更新【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await accessLog.NewActionAsync(GetLoginUser(), PageName, "權限設定更新【失敗】", msg, true);

                return RedirectToAction(nameof(Index), "Role");
            }

            var successMsg = $"角色-{role.RoleName} 權限設定已更新";
            TempData["_JSShowSuccess"] = successMsg;

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "權限設定更新成功");

            return RedirectToAction(nameof(Index), "Role");
        }




    }
}
