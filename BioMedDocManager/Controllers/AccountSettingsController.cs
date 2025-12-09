using BioMedDocManager.Extensions;
using BioMedDocManager.Factory;
using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Dapper;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 帳號設定
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    //[Authorize(Roles = AppSettings.AdminRoleStrings.系統管理者)]
    [Route("[controller]")]
    public class AccountSettingsController(DocControlContext context, IWebHostEnvironment hostingEnvironment, IAccessLogService accessLog) : BaseController(context, hostingEnvironment)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "帳號設定";

        /// <summary>
        /// 預設排序依據
        /// </summary>
        public const string InitSort = "UserFullName";

        /// <summary>
        /// 查詢畫面的表頭DB與中文對照 (因為沒有結果也要顯示)
        /// </summary>
        public Dictionary<string, string> TableHeaders = TableHeaderFactory.Build<User>(
            includeRowNum: true,
            onlyProps: new[] { "UserAccount", "Department.Department_Name", "UserJobTitle", "UserFullName", "UserIsActiveText", "UserIsLockedText", "CreatedAt", "UserGroupRoleList" }
        );

        /// <summary>
        /// 顯示帳號設定頁面
        /// </summary>
        /// <param name="PageSize">單頁顯示筆數</param>
        /// <param name="PageNumber">第幾頁</param>
        /// <returns></returns>
        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber, CancellationToken ct)
        {
            // 從Session抓queryModel查詢物件
            var queryModel = GetSessionQueryModel<AccountViewModel>();

            // 如果query string有page參數，才使用；否則保留Session中的值
            if (PageSize.HasValue)
            {
                queryModel.PageSize = PageSize.Value;
            }
            if (PageNumber.HasValue)
            {
                queryModel.PageNumber = PageNumber.Value;
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(queryModel);

            // 儲存查詢model到session中
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            // 載入角色List
            ViewData["AllRoles"] = await GetRoles();

            // 載入角色List
            ViewData["AllRoles"] = await GetRoles();

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示清單頁");

            return await BuildQueryAccountSettings(queryModel, ct);
        }

        /// <summary>
        /// 文件管制查詢頁面送出查詢
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <returns></returns>
        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AccountViewModel queryModel)
        {
            // 儲存查詢model到session中
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            // 轉跳到GET方法頁面，顯示查詢內容
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示新增頁
        /// </summary>
        /// <returns></returns>
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var AccountViewModel = new CreateUserViewModel
            {
                CreatedAt = DateTime.Now,
            };

            // 載入角色List
            ViewData["AllRoles"] = await GetRoles();

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示新增頁");

            return View(AccountViewModel);

        }

        /// <summary>
        /// 更新個人資料
        /// </summary>
        /// <param name="user">資料</param>
        /// <returns></returns>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel PostedUser)
        {
            if (PostedUser == null)
            {
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(PostedUser);

            try
            {

                ModelState.Remove("RoleName");//不用驗證
                ModelState.Remove("RoleNameList");//不用驗證

                if (!ModelState.IsValid)
                {
                    return View(PostedUser);
                }

                var newUser = ToUserEntity(PostedUser);

                await context.Users.AddAsync(newUser);
                await context.SaveChangesAsync(); // 儲存後 newUser.Id 才有值

            }
            catch (DbUpdateConcurrencyException ex)
            {
                string customErrorString = "\"帳號設定-\" + PostedUser.FullName + \"資料新增【失敗】!\"";
                Utilities.WriteExceptionIntoLogFile(customErrorString, ex, this.HttpContext);
                TempData["_JSShowAlert"] = customErrorString;
                await accessLog.NewActionAsync(GetLoginUser(), PageName, "資料新增【失敗】", customErrorString, true);
            }

            TempData["_JSShowSuccess"] = "帳號設定-" + PostedUser.UserFullName + "資料新增成功!";

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁資料新增成功");

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示編輯頁
        /// </summary>
        /// <param name="UserId">使用者Id</param>
        /// <returns></returns>
        [HttpGet("Edit/{userId:int}")]
        public async Task<IActionResult> Edit([FromRoute] int? UserId)
        {
            if (UserId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var user = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(s => s.UserId == UserId);

            if (user == null)
            {
                return NotFound();
            }

            var AccountViewModel = new AccountViewModel
            {
                UserAccount = user.UserAccount,
                UserFullName = user.UserFullName,
                UserJobTitle = user.UserJobTitle,
                DepartmentName = user.Department.DepartmentName,
                UserEmail = user.UserEmail,
                UserPhone = user.UserPhone,
                UserMobile = user.UserMobile,
                CreatedAt = user.CreatedAt,
                UserIsActive = user.UserIsActive,
                UserIsLocked = user.UserIsLocked,
                UserRemarks = user.UserRemarks,
            };

            ViewBag.DepartmentOptions = DepartmentNameOptions(onlyActive: true);

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");

            return View(AccountViewModel);
        }

        /// <summary>
        /// 更新個人資料
        /// </summary>
        /// <param name="id">工號</param>
        /// <param name="user">資料</param>
        /// <returns></returns>
        [HttpPost("Edit/{userId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int? UserId, AccountViewModel user)
        {
            if (user == null || UserId.GetValueOrDefault() <= 0 || UserId != user.UserId)
            {
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(user);

            var DBuser = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role) // 確保 Role 有載入
                .FirstOrDefaultAsync(s => s.UserId == user.UserId);

            if (DBuser == null)
            {
                return NotFound();
            }

            try
            {
                DBuser.UserFullName = user.UserFullName?.Trim();
                DBuser.UserJobTitle = user.UserJobTitle?.Trim();
                DBuser.UserEmail = user.UserEmail?.Trim();
                DBuser.UserPhone = user.UserPhone?.Trim();
                DBuser.UserMobile = user.UserMobile?.Trim();

                // 這兩個是 bool? 的寫法（若你的欄位是 bool?）
                DBuser.UserIsActive = user.UserIsActive ?? false;
                DBuser.UserIsLocked = user.UserIsLocked ?? false;

                DBuser.UserRemarks = string.IsNullOrWhiteSpace(user.UserRemarks) ? null : user.UserRemarks.Trim();

                await context.SaveChangesAsync();

            }
            catch (DbUpdateConcurrencyException ex)
            {
                string customErrorString = "帳號設定-" + DBuser.UserFullName + "資料更新【失敗】!";
                Utilities.WriteExceptionIntoLogFile(customErrorString, ex, this.HttpContext);
                TempData["_JSShowAlert"] = customErrorString;
                await accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁資料更新【失敗】", customErrorString, true);

                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = "帳號設定-" + DBuser.UserFullName + "資料更新成功!";

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁資料更新成功");

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示密碼重設頁
        /// </summary>
        /// <param name="UserId">使用者Id</param>
        /// <returns></returns>
        [HttpGet("ResetPassword/{userId:int}")]
        public async Task<IActionResult> ResetPassword([FromRoute] int? UserId)
        {
            if (UserId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var user = await context.Users
                .FirstOrDefaultAsync(s => s.UserId == UserId);

            if (user == null)
            {
                return NotFound();
            }

            // 產生變更密碼模型
            var model = new ChangePasswordViewModel
            {
                UserAccount = user.UserAccount,
                UserFullName = user.UserFullName,
            };

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示密碼重設頁");

            return View(model);

        }

        /// <summary>
        /// 更新密碼
        /// </summary>
        /// <param name="id">工號</param>
        /// <param name="user">資料</param>
        /// <returns></returns>
        [HttpPost("ResetPassword/{userId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromRoute] int? UserId, ChangePasswordViewModel PostedUser)
        {
            if (PostedUser == null || UserId.GetValueOrDefault() <= 0 || UserId != PostedUser.UserId)
            {
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(PostedUser);

            var User = await context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(s => s.UserId == PostedUser.UserId);

            if (User == null)
            {
                return NotFound();
            }

            try
            {
                // 這是管理者重設，不用知道原本使用者密碼
                ModelState.Remove("CurrentPassword");// 不用驗證 原密碼

                if (!ModelState.IsValid)
                {
                    return View(PostedUser);
                }

                // 將新密碼寫入資料庫
                User.UserPasswordHash = HashPassword(User, PostedUser.UserNewPassword);

                await context.SaveChangesAsync();

            }
            catch (DbUpdateConcurrencyException ex)
            {
                string customErrorString = "帳號設定-" + User.UserFullName + "密碼重設【失敗】!";
                Utilities.WriteExceptionIntoLogFile(customErrorString, ex, this.HttpContext);
                TempData["_JSShowAlert"] = customErrorString;
                await accessLog.NewActionAsync(GetLoginUser(), PageName, "密碼重設【失敗】", customErrorString, true);
            }

            TempData["_JSShowSuccess"] = "帳號設定-" + User.UserFullName + "密碼重設完成!";

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "密碼重設完成");

            return RedirectToAction(nameof(Index));

        }

        /// <summary>
        /// 顯示編輯權限頁面 GET: /EditGroup/5
        /// </summary>
        /// <param name="UserId">使用者Id</param>
        /// <param name="groupIds">群組Ids</param>
        /// <returns></returns>
        [HttpGet("EditGroup/{userId:int}")]
        public async Task<IActionResult> EditGroup([FromRoute] int? UserId, [FromQuery] int[]? groupIds)
        {
            if (UserId.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == UserId);

            if (user == null)
            {
                return NotFound();
            }

            // 所有群組
            var allGroups = await context.UserGroups
                .AsNoTracking()
                .OrderBy(g => g.UserGroupName)
                .Select(g => new { g.UserGroupId, g.UserGroupName, g.UserGroupDescription })
                .ToListAsync();

            // 目前DB已有的群組
            var currentGroupIds = await context.UserGroupMembers
                .Where(m => m.UserId == UserId)
                .Select(m => m.UserGroupId)
                .ToListAsync();

            var vm = new UserGroupsEditViewModel
            {
                UserId = user.UserId,
                UserAccount = user.UserAccount,
                UserFullName = user.UserFullName,
                SelectedUserGroupIds = currentGroupIds,
                AllUserGroups = allGroups.Select(g => new SelectListItem
                {
                    Value = g.UserGroupId.ToString(),
                    Text = g.UserGroupName,
                    Selected = currentGroupIds.Contains(g.UserGroupId)
                }).ToList()
            };

            // 計算預覽（有效角色、有效權限）
            await ComputePreviewAsync(vm);

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯使用者權限群組頁面");

            return View(vm);
        }

        /// <summary>
        /// 編輯權限頁面儲存 POST: /EditGroup/5
        /// </summary>
        /// <param name="form">表單物件</param>
        /// <param name="command">命令(save/preview)</param>
        /// <returns></returns>
        [HttpPost("EditGroup/{userId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup([FromRoute] int? UserId, UserGroupsEditPostViewModel PostedUser, string command)
        {
            if (PostedUser == null || UserId.GetValueOrDefault() <= 0 || UserId != PostedUser.UserId)
            {
                return NotFound();
            }

            // 確認使用者存在
            var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == PostedUser.UserId);
            if (user == null) { 
                return NotFound();
            }

            // 目標群組（右側 selectedGroups 全部值；JS 在 submit 前已全部設成 selected）
            var want = PostedUser.SelectedUserGroupIds?
                           .Distinct()
                           .ToList()
                       ?? new List<int>();

            // 目前 DB 裡的群組
            var existing = await context.UserGroupMembers
                .Where(m => m.UserId == UserId)
                .Select(m => m.UserGroupId)
                .ToListAsync();

            var toAdd = want.Except(existing).ToList();
            var toRemove = existing.Except(want).ToList();

            if (toAdd.Count == 0 && toRemove.Count == 0)
            {
                TempData["_JSShowAlert"] = "帳號設定-使用者權限群組未異動，不儲存。";
            }
            else
            {
                using var tx = await context.Database.BeginTransactionAsync();
                try
                {
                    if (toRemove.Count > 0)
                    {
                        await context.UserGroupMembers
                            .Where(m => m.UserId == UserId && toRemove.Contains(m.UserGroupId))
                            .ExecuteDeleteAsync();// 真的刪除(軟刪除會非常複雜)
                    }

                    foreach (var gid in toAdd)
                    {
                        context.UserGroupMembers.Add(new UserGroupMember
                        {
                            UserId = UserId.Value,
                            UserGroupId = gid,
                        });
                    }

                    await context.SaveChangesAsync();
                    await tx.CommitAsync();
                    TempData["_JSShowSuccess"] = "帳號設定-使用者權限群組儲存成功";
                }
                catch
                {
                    await tx.RollbackAsync();
                    TempData["_JSShowAlert"] = "帳號設定-使用者權限群組儲存【失敗】";
                    await accessLog.NewActionAsync(GetLoginUser(), PageName, "使用者權限群組儲存【失敗】");
                }
            }

            await accessLog.NewActionAsync(GetLoginUser(), PageName, "使用者權限群組儲存成功");

            // PRG：回到帳號管理清單頁
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// AJAX：試算目前選取群組產生的「有效角色 / 有效權限」，並標出哪些是新增加的
        /// </summary>
        /// <param name="req">使用者權限預覽請求</param>
        /// <returns>使用者權限預覽結果</returns>
        [HttpPost("PreviewPermissions")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewPermissions([FromBody] PreviewPermissionsRequestViewModel req)
        {
            var userId = req.UserId;
            var newGroupIds = (req.SelectedUserGroupIds ?? new()).Distinct().ToList();

            // 目前 DB 裡這個使用者實際擁有的群組
            var currentGroupIds = await context.UserGroupMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.UserGroupId)
                .Distinct()
                .ToListAsync();

            // === 先算「目前 DB 狀態」的角色/權限 ===
            var currentRoleIds = await context.UserGroupRoles
                .Where(ugr => currentGroupIds.Contains(ugr.UserGroupId))
                .Select(ugr => ugr.RoleId)
                .Distinct()
                .ToListAsync();

            var currentPermKeys = await context.RolePermissions
                .Where(rp => currentRoleIds.Contains(rp.RoleId))
                .Select(rp => new { rp.ResourceId, rp.AppActionId })
                .Distinct()
                .ToListAsync();
            var currentPermSet = currentPermKeys
                .Select(x => (x.ResourceId, x.AppActionId))
                .ToHashSet();

            // === 再算「這次選取之後」的角色/權限 ===
            var roleFromGroups = await context.UserGroupRoles
                .Where(ugr => newGroupIds.Contains(ugr.UserGroupId))
                .Join(context.UserGroups,
                    ugr => ugr.UserGroupId,
                    ug => ug.UserGroupId,
                    (ugr, ug) => new
                    {
                        ugr.RoleId,
                        ug.UserGroupId,
                        ug.UserGroupName
                    })
                .ToListAsync();

            var newRoleIds = roleFromGroups
                .Select(x => x.RoleId)
                .Distinct()
                .ToList();

            var roles = await context.Roles
                .Where(r => newRoleIds.Contains(r.RoleId))
                .ToListAsync();

            var roleDtos = roles
                .Select(r => new PreviewRoleViewModel
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    RoleGroup = r.RoleGroup,
                    IsNew = !currentRoleIds.Contains(r.RoleId),   // 這裡沿用原本「是不是新角色」
                    FromGroups = roleFromGroups
                        .Where(x => x.RoleId == r.RoleId)
                        .Select(x => new PreviewRoleSourceGroupViewModel
                        {
                            UserGroupId = x.UserGroupId,
                            UserGroupName = x.UserGroupName
                        })
                        .Distinct()
                        .ToList()
                })
                .OrderBy(r => r.RoleGroup)
                .ThenBy(r => r.RoleName)
                .ToList();

            // 權限：RolePermission + Resource + AppAction
            var permsRaw = await context.RolePermissions
                .Where(rp => newRoleIds.Contains(rp.RoleId))
                .Join(context.Resources,
                    rp => rp.ResourceId,
                    res => res.ResourceId,
                    (rp, res) => new { rp, res })
                .Join(context.AppActions, // 你的動作表若叫 Action，就換成 context.Actions
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

            // 合併同一 Resource + Action（多角色來源只看有沒有 this key）
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
                    IsNew = !currentPermSet.Contains((g.Key.ResourceId, g.Key.AppActionId)) // 原本沒有 → 預覽
                })
                .OrderBy(p => p.ResourceDisplayName)
                .ThenBy(p => p.AppActionOrder)
                .ToList();

            return Json(new
            {
                roles = roleDtos,
                permissions = permDtos
            });
        }
        // GET: /AccountSettings/Details/5
        [HttpGet("Details/{userId:int}")]
        public async Task<IActionResult> Details(int? userId)
        {
            if (userId == null)
            {
                return NotFound();
            }

            var user = await context.Users
                .Include(u => u.Department)
                .Include(u => u.UserGroupMembers)
                    .ThenInclude(m => m.UserGroup)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.DeletedAt == null);

            if (user == null)
            {
                return NotFound();
            }

            // 目前使用者所屬的群組 Id
            var selectedGroupIds = user.UserGroupMembers
                .Select(m => m.UserGroupId)
                .Distinct()
                .ToList();

            // 利用「既有的」 UserGroupsEditViewModel + ComputePreviewAsync 來算有效角色/權限
            var previewVm = new UserGroupsEditViewModel
            {
                UserId = user.UserId,
                SelectedUserGroupIds = selectedGroupIds
            };

            await ComputePreviewAsync(previewVm);

            // 組成明細頁的 ViewModel
            var vm = new UserDetailsViewModel
            {
                User = user,
                UserGroups = user.UserGroupList,                    // 用 User 裡 NotMapped 的 UserGroupList
                EffectiveRoles = previewVm.EffectiveRoles,          // 沿用 ComputePreviewAsync 算出來的結果
                EffectivePermissions = previewVm.EffectivePermissions
            };

            return View(vm);
        }

        /// <summary>
        /// 計算「有效角色」與「有效權限」並放入 ViewModel
        /// </summary>
        [NonAction]
        private async Task ComputePreviewAsync(UserGroupsEditViewModel vm)
        {
            var selected = vm.SelectedUserGroupIds?.Distinct().ToList() ?? new();

            // 1) 先把群組→角色對照拉回來 (在記憶體)
            var roleFromGroups = await context.UserGroupRoles
                .Where(x => selected.Contains(x.UserGroupId))
                .Select(x => new { x.RoleId, x.UserGroupId })
                .ToListAsync();

            var roleIds = roleFromGroups.Select(x => x.RoleId).Distinct().ToList();

            // 2) 在記憶體先做 RoleId -> List<UserGroupId> 的 map，避免在 EF 查詢內再關聯
            var roleIdToGroupIds = roleFromGroups
                .GroupBy(x => x.RoleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.UserGroupId).Distinct().ToList()
                );

            // 3) 角色先整包查回（僅資料列），再在記憶體組 EffectiveRoleViewModel
            var roles = await context.Roles
                .Where(r => roleIds.Contains(r.RoleId))
                .AsNoTracking()
                .OrderBy(r => r.RoleGroup)
                .ThenBy(r => r.RoleName)
                .ToListAsync();

            vm.EffectiveRoles = roles.Select(r => new EffectiveRoleViewModel
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                RoleGroup = r.RoleGroup,
                FromUserGroupIds = roleIdToGroupIds.TryGetValue(r.RoleId, out var list) ? list : new List<int>()
            }).ToList();

            // ===== 以下原本權限計算維持不動（你這段是在記憶體再做 GroupBy，不會有翻譯問題） =====

            var actionOrders = await context.AppActions
                .Select(a => new { a.AppActionId, a.AppActionOrder, a.AppActionName, a.AppActionDisplayName })
                .AsNoTracking()
                .ToListAsync();

            var actionOrderMap = actionOrders.ToDictionary(a => a.AppActionId, a => a.AppActionOrder);

            var rawPerms = await context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Join(context.Resources, rp => rp.ResourceId, res => res.ResourceId, (rp, res) => new { rp, res })
                .Join(context.AppActions, j => j.rp.AppActionId, act => act.AppActionId, (j, act) => new
                {
                    j.rp.RoleId,
                    j.rp.ResourceId,
                    j.rp.AppActionId,
                    j.res.ResourceKey,
                    j.res.ResourceDisplayName,
                    AppActionName = act.AppActionName,
                    AppActionDisplayName = act.AppActionDisplayName
                })
                .AsNoTracking()
                .ToListAsync();

            var permGroups = rawPerms
                .GroupBy(p => new { p.ResourceId, p.ResourceKey, p.ResourceDisplayName, p.AppActionId, p.AppActionName, p.AppActionDisplayName })
                .Select(g => new EffectivePermissionViewModel
                {
                    ResourceId = g.Key.ResourceId,
                    ResourceKey = g.Key.ResourceKey,
                    ResourceDisplayName = g.Key.ResourceDisplayName,
                    AppActionId = g.Key.AppActionId,
                    AppActionName = g.Key.AppActionName,
                    AppActionDisplayName = g.Key.AppActionDisplayName,
                    FromRoleIds = g.Select(x => x.RoleId).Distinct().ToList()
                })
                .ToList();

            vm.EffectivePermissions = permGroups
                .OrderBy(p => p.ResourceDisplayName)
                .ThenBy(p => actionOrderMap.TryGetValue(p.AppActionId, out var ord) ? ord : int.MaxValue)
                .ToList();
        }

        /// <summary>
        /// 建立查詢EF（帳號設定）
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <param name="ct">取消token</param>
        /// <returns>查詢結果ViewResult</returns>
        [NonAction]
        public async Task<IActionResult> BuildQueryAccountSettings(AccountViewModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            // 1) 篩選與排序（你的白名單：Key=屬性名, Value=顯示文字）
            FilterOrderBy(queryModel, TableHeaders, InitSort);

            // 2) 產生查詢物件（載入角色關聯）
            IQueryable<User> q = context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsNoTracking();

            // 3) 條件判斷
            // 工號(帳號)
            if (!string.IsNullOrEmpty(queryModel.UserAccount))
            {
                var s = $"%{queryModel.UserAccount.Trim()}%";
                q = q.Where(u => EF.Functions.Like(u.UserAccount, s));
            }

            // 姓名
            if (!string.IsNullOrEmpty(queryModel.UserFullName))
            {
                var s = $"%{queryModel.UserFullName.Trim()}%";
                q = q.Where(u => EF.Functions.Like(u.UserFullName, s));
            }

            // 是否啟用
            if (queryModel.UserIsActive.HasValue)
            {
                q = q.Where(u => u.UserIsActive == queryModel.UserIsActive.Value);
            }


            /*
            // 系統角色（相當於 EXISTS 子查詢）
            if (queryModel.RoleName != null && queryModel.RoleName.Any())
            {
                var roleNames = queryModel.RoleName.ToList();
                q = q.Where(u => u.UserRoles.Any(ur => roleNames.Contains(ur.Role.RoleName)));
            }
            */



            // 4) 排序（用屬性名白名單）
            q = q.OrderByWhitelist(
                queryModel.OrderBy,          // 例如 "UserAccount" / "UserFullName" / "UserLastLoginAt"
                queryModel.SortDir,          // "asc" / "desc"
                TableHeaders,                // Key=屬性名, Value=顯示文字
                tiebreakerProperty: "UserId"     // 第2排序欄位：主鍵
            );

            // 5) 分頁＋總筆數
            var (entityList, totalCount) =
                await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

            // 6) 投影顯示：把 RoleName 串成字串（在記憶體端處理）
            var shaped = entityList.Select(u => new
            {
                u.UserId,
                u.UserAccount,
                u.UserFullName,
                u.UserJobTitle,
                u.Department.DepartmentName,
                u.UserEmail,
                u.UserPhone,
                u.UserMobile,
                u.UserIsActiveText,
                u.UserIsLockedText,
                u.UserLoginFailedCount,
                u.UserLastLoginAt,
                u.UserLastLoginIp,
                u.UserPasswordChangedAt,
                u.UserRemarks,
                u.CreatedAt,
                u.CreatedBy,
                u.UpdatedAt,
                u.UpdatedBy,
                u.DeletedAt,
                u.DeletedBy,
                u.RoleNameList,
            }).ToList();

            // 7) 轉成 View 需要的 List<Dictionary<string, object>>
            //    （用屬性名當 key，讓 tbody 依 headers.Keys 的順序渲染）
            var result = BuildRows(
                entities: shaped,
                tableHeaders: TableHeaders,       // Key=屬性名, Value=顯示文字；含 "RowNum" => "#"
                pageNumber: queryModel.PageNumber,
                pageSize: queryModel.PageSize,
                keyMode: KeyMode.PropertyName, // 用屬性名當輸出鍵
                includeRowNum: true,
                payloadProps: new[] { "UserId" } // 這裡指名要帶進每列的欄位，但不渲染顯示
            );

            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }

        


    }
}
