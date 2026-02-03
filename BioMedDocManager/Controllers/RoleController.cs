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

                await _context.Roles.AddAsync(posted);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("Role.Create.Title") + "-" + posted.RoleCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("Role.Create.Title") + "-" + posted.RoleCode + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Edit =======================
        public async Task<IActionResult> Edit([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == id);

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
        public async Task<IActionResult> Edit([FromRoute]  long? id, Role posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.RoleId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            var dbEntity = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (dbEntity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，dbEntity為null");
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
                var msg = _loc.T("Role.Edit.Title") + "-" + dbEntity.RoleCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "更新【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("Role.Edit.Title") + "-" + dbEntity.RoleCode + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Details =======================
        public async Task<IActionResult> Details([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁", "錯誤，id小於等於0");
                return NotFound();
            }
            var entity = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁", "錯誤，entity為null");
                return NotFound();
            }

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
        public async Task<IActionResult> Delete([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Roles
                .Include(r => r.UserRoles)
                    .ThenInclude(ur => ur.User)
                .Include(r => r.UserGroupRoles)
                    .ThenInclude(ugr => ugr.UserGroup)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁", "錯誤，entity為null");
                return NotFound();
            }

            // ==== 使用者明細 ====
            var userList = entity.UserRoles
                .Where(ur => ur.User != null)
                .Select(ur => new RoleUsageUserViewModel
                {
                    UserId = ur.UserId,
                    UserAccount = ur.User!.UserAccount,
                    UserFullName = ur.User!.UserFullName
                })
                .ToList();

            // ==== 群組明細 ====
            var groupList = entity.UserGroupRoles
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

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute]  long? id, Role posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.RoleId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            var entity = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == posted.RoleId);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除頁儲存", "錯誤，entity為null");
                return NotFound();
            }

            try
            {
                // 再檢查一次是否仍被使用者或群組引用
                var usedByUser = await _context.UserRoles.AnyAsync(ur => ur.RoleId == entity.RoleId);
                var usedByGroup = await _context.UserGroupRoles.AnyAsync(ugr => ugr.RoleId == entity.RoleId);

                if (usedByUser || usedByGroup)
                {
                    var msg =
                            _loc.T("Role.Delete.UsedByUserOrGroup.Prefix")
                            + entity.RoleCode
                            + _loc.T("Role.Delete.UsedByUserOrGroup.Suffix");
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
                var msg = _loc.T("Role.Delete.Title") + "-" + entity.RoleCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("Role.Delete.Title") + "-" + entity.RoleCode + _loc.T("Common.Success");

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
