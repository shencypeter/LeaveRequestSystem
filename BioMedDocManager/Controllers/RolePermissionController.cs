using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 角色權限管理
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    
    public class RolePermissionController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "角色權限管理";

        public async Task<IActionResult> Edit([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，entity為null");
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
                RoleId = entity.RoleId,
                RoleCode = entity.RoleCode,
                Resources = resources,
                AppActions = actions,
                SelectedPermissionKeys = selectedKeys
            };

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示權限編輯頁");

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] long? id, RolePermissionEditViewModel posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.RoleId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            var dbEntity = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == posted.RoleId);

            if (dbEntity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，dbEntity為null");
                return NotFound();
            }

            // 1) 解析 SelectedPermissionKeys -> HashSet<(long ResourceId, long AppActionId)>
            var newKeys = new HashSet<(long ResourceId, long AppActionId)>();

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

                if (long.TryParse(parts[0], out var resId) &&
                    long.TryParse(parts[1], out var actId))
                {
                    newKeys.Add((resId, actId));
                }
            }

            try
            {
                // 2) 讀取目前 DB 中此角色的 RolePermission
                var existingPerms = await _context.RolePermissions
                    .Where(rp => rp.RoleId == dbEntity.RoleId)
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
                        RoleId = dbEntity.RoleId,
                        ResourceId = resId,
                        AppActionId = actId
                    };
                    await _context.RolePermissions.AddAsync(rp);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("RolePermission.Index.Title") + "-" + dbEntity.RoleCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "權限設定更新【失敗】", msg, true);

                // 失敗就回到 Details 或 Index 都可以，這邊回 Details
                return RedirectToAction(nameof(Details), new { roleId = dbEntity.RoleId });
            }

            var successMsg = _loc.T("RolePermission.Index.Title") + "-" + dbEntity.RoleCode + _loc.T("Common.Success");
            TempData["_JSShowSuccess"] = successMsg;

            await _accessLog.NewActionAsync(
                GetLoginUser(),
                "角色管理",
                "權限設定更新成功",
                successMsg
            );

            return RedirectToAction(nameof(RoleController.Index), "Role");
        }




    }
}
