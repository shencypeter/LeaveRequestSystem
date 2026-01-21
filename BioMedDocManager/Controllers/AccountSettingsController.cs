using BioMedDocManager.Extensions;
using BioMedDocManager.Factory;
using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;
using System.Security.Claims;
using System.Security.Cryptography;
using static Fable.React.Props.SVGAttr;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 帳號設定
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>

    public class AccountSettingsController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
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
            onlyProps: new[] { "UserAccount", "DepartmentName", "UserJobTitle", "UserFullName", "UserIsActiveText", "UserIsLockedText", "CreatedAt", "UserGroupRoleList" }
        );

        /// <summary>
        /// TOTP狀態物件
        /// </summary>
        private const string TotpSetupSessionKey = "_TotpSetupState";

        /// <summary>
        /// 顯示清單頁
        /// </summary>
        /// <param name="PageSize">單頁顯示筆數</param>
        /// <param name="PageNumber">第幾頁</param>
        /// <returns></returns>        
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

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示清單頁");

            return await BuildQueryAccountSettings(queryModel, ct);
        }

        /// <summary>
        /// 清單頁送出查詢
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <returns></returns>        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AccountViewModel queryModel)
        {
            // 儲存查詢model到session中
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            // 轉跳到GET方法頁面，顯示查詢內容
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示新增頁
        /// </summary>
        /// <returns></returns>        
        public async Task<IActionResult> Create()
        {
            var AccountViewModel = new CreateUserViewModel();

            // 載入角色List
            ViewData["AllRoles"] = await GetRoles();

            // 載入部門List
            ViewBag.DepartmentOptions = DepartmentNameOptions(onlyActive: true);

            // 讀取密碼政策資料
            SetPasswordPolicyToViewBag();

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示新增頁");

            return View(AccountViewModel);

        }

        /// <summary>
        /// 新增頁儲存
        /// </summary>
        /// <param name="user">資料</param>
        /// <returns></returns>        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel posted)
        {
            if (posted == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", "錯誤，posted為null");
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(posted);

            try
            {

                ModelState.Remove("RoleCode");//不用驗證
                ModelState.Remove("RoleCodeList");//不用驗證

                if (!ModelState.IsValid)
                {
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", "錯誤，必填資料未填寫");
                    return RedirectToAction(nameof(Index));
                }

                var newUser = ToUserEntity(posted);

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync(); // 儲存後 newUser.Id 才有值

            }
            catch (DbUpdateConcurrencyException ex)
            {
                string customErrorString = _loc.T("AccountSettings.Create.Title") + "-" + posted.UserFullName + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(customErrorString, ex, this.HttpContext);
                TempData["_JSShowAlert"] = customErrorString;
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", customErrorString, true);
            }

            TempData["_JSShowSuccess"] = _loc.T("AccountSettings.Create.Title") + "-" + posted.UserFullName + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", "新增成功");

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示編輯頁
        /// </summary>
        /// <param name="UserId">使用者Id</param>
        /// <returns></returns>        
        public async Task<IActionResult> Edit([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(s => s.UserId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，entity為null");
                return NotFound();
            }

            var AccountViewModel = new AccountViewModel
            {
                UserId = entity.UserId,
                UserAccount = entity.UserAccount,
                UserFullName = entity.UserFullName,
                UserJobTitle = entity.UserJobTitle,
                DepartmentId = entity.DepartmentId,
                UserEmail = entity.UserEmail,
                UserPhone = entity.UserPhone,
                UserMobile = entity.UserMobile,
                CreatedAt = entity.CreatedAt,
                UserIsActive = entity.UserIsActive,
                UserIsLocked = entity.UserIsLocked,
                UserRemarks = entity.UserRemarks,
            };

            ViewBag.DepartmentOptions = DepartmentNameOptions(onlyActive: true);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");

            return View(AccountViewModel);
        }

        /// <summary>
        /// 編輯頁儲存
        /// </summary>
        /// <param name="id">工號</param>
        /// <param name="user">資料</param>
        /// <returns></returns>        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int? id, AccountViewModel posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.UserId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(posted);

            var dbEntity = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role) // 確保 Role 有載入
                .FirstOrDefaultAsync(s => s.UserId == posted.UserId);

            if (dbEntity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，dbEntity為null");
                return NotFound();
            }

            try
            {
                dbEntity.UserFullName = posted.UserFullName?.Trim();
                dbEntity.UserJobTitle = posted.UserJobTitle?.Trim();
                dbEntity.UserEmail = posted.UserEmail?.Trim();
                dbEntity.UserPhone = posted.UserPhone?.Trim();
                dbEntity.UserMobile = posted.UserMobile?.Trim();

                // 這兩個是 bool? 的寫法
                dbEntity.UserIsActive = posted.UserIsActive ?? false;
                dbEntity.UserIsLocked = posted.UserIsLocked ?? false;

                dbEntity.UserRemarks = string.IsNullOrWhiteSpace(posted.UserRemarks) ? null : posted.UserRemarks.Trim();

                await _context.SaveChangesAsync();

            }
            catch (DbUpdateConcurrencyException ex)
            {
                string customErrorString = _loc.T("AccountSettings.Edit.Title") + "-" + dbEntity.UserFullName + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(customErrorString, ex, this.HttpContext);
                TempData["_JSShowAlert"] = customErrorString;
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", customErrorString, true);
            }

            TempData["_JSShowSuccess"] = _loc.T("AccountSettings.Edit.Title") + "-" + dbEntity.UserFullName + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "更新成功");

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示變更密碼頁
        /// </summary>
        /// <returns></returns>        
        public async Task<IActionResult> ChangePassword()
        {
            // 產生變更密碼模型
            var model = new ChangePasswordViewModel
            {
                UserAccount = User.FindFirst("UserAccount")?.Value ?? "",
                UserFullName = User.FindFirst("UserFullName")?.Value ?? ""
            };

            var flag = HttpContext.Session.GetString(AppSettings.ForceChangePasswordRequiredKey);
            var isFirst = HttpContext.Session.GetString(AppSettings.ForceChangePasswordReasonFirstKey);
            var isExpire = HttpContext.Session.GetString(AppSettings.ForceChangePasswordReasonExpireKey);

            if (string.Equals(flag, "Y", StringComparison.OrdinalIgnoreCase))
            {
                string msg;
                if (string.Equals(isFirst, "Y", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(isExpire, "Y", StringComparison.OrdinalIgnoreCase))
                {
                    // 密碼已過期且為首次登入
                    msg = _loc.T("PasswordPolicy.ExpiredFirstLogin");
                }
                else if (string.Equals(isFirst, "Y", StringComparison.OrdinalIgnoreCase))
                {
                    // 首次登入
                    msg = _loc.T("PasswordPolicy.FirstLogin");
                }
                else if (string.Equals(isExpire, "Y", StringComparison.OrdinalIgnoreCase))
                {
                    // 密碼過期
                    msg = _loc.T("PasswordPolicy.Expired");
                }
                else
                {
                    // 其他強制更換原因
                    msg = _loc.T("PasswordPolicy.PolicyRequired");
                }

                // 這裡才塞 TempData，view 一樣用 _JSShowAlert 顯示
                TempData["_JSShowAlert"] = msg;
            }

            SetPasswordPolicyToViewBag();

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示變更密碼頁");

            return View(model);
        }

        /// <summary>
        /// 變更密碼頁儲存
        /// </summary>
        /// <param name="model">資料</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            // 先抓政策
            var policy = GetPasswordPolicy();

            // 先做「新密碼強度檢核」：先加 ModelState，再走原本流程
            ValidateNewPasswordByPolicy("UserNewPasswordHash", model.UserNewPasswordHash, policy);

            if (!ModelState.IsValid)
            {
                SetPasswordPolicyToViewBag();
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "變更密碼頁儲存", "失敗，必填資料未填寫或未符合密碼政策");
                return View(model);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var uid = int.Parse(userId!);

            var user = await _context.Users.FindAsync(uid);
            if (user == null)
            {
                SetPasswordPolicyToViewBag();
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "變更密碼頁儲存", "失敗，找不到使用者");
                return NotFound();
            }

            // 歷史密碼
            var okHistory = await CheckPasswordHistoryAsync(user, model.UserNewPasswordHash, policy);
            if (!okHistory)
            {
                SetPasswordPolicyToViewBag();
                ModelState.AddModelError("UserNewPassword",
                    _loc.T("AccountSettings.Password.HistoryLimit.Prefix") +
                    policy.HistoryCount +
                    _loc.T("AccountSettings.Password.HistoryLimit.Suffix"));
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "變更密碼頁儲存", "失敗，與歷史密碼相同");
                return View(model);
            }

            // 確認原密碼正確
            var result = VerifyHashedPassword(user, user.UserPasswordHash, model.UserCurrentPassword);
            if (result != PasswordVerificationResult.Success)
            {
                SetPasswordPolicyToViewBag();
                ModelState.AddModelError("UserCurrentPassword", _loc.T("AccountSettings.Password.WrongCurrent"));
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "變更密碼頁儲存", "失敗，原密碼錯誤");
                return View(model);
            }

            user.UserPasswordHash = HashPassword(user, model.UserNewPasswordHash);
            user.UserPasswordChangedAt = DateTime.Now;

            // 將新密碼寫入歷史資料庫
            _context.UserPasswordHistories.Add(new UserPasswordHistory
            {
                UserId = user.UserId,
                PasswordHash = user.UserPasswordHash,  // 這裡是剛剛「新密碼的 Hash」，下次變更密碼判斷用
            });

            // 密碼變更成功後，清除強制改密碼 Session
            HttpContext.Session.Remove(AppSettings.ForceChangePasswordRequiredKey);
            HttpContext.Session.Remove(AppSettings.ForceChangePasswordReasonFirstKey);
            HttpContext.Session.Remove(AppSettings.ForceChangePasswordReasonExpireKey);

            await _context.SaveChangesAsync();

            await _accessLog.NewPasswordAsync(GetLoginUser(), PageName, "變更密碼頁儲存成功", GetLoginUser());

            return DismissModal(_loc.T("AccountSettings.ChangePassword.Title") + "-" + _loc.T("Common.Success"));
        }

        /// <summary>
        /// 顯示管理者重設使用者密碼頁面
        /// </summary>
        /// <param name="UserId">使用者Id</param>
        /// <returns></returns>        
        public async Task<IActionResult> ResetPassword([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示管理者重設使用者密碼頁面", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Users.FirstOrDefaultAsync(s => s.UserId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示管理者重設使用者密碼頁面", "錯誤，entity為null");
                return NotFound();
            }

            // 產生變更密碼模型
            var model = new ChangePasswordViewModel
            {
                UserAccount = entity.UserAccount,
                UserFullName = entity.UserFullName,
            };

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示管理者重設使用者密碼頁面");

            return View(model);

        }

        /// <summary>
        /// 管理者重設使用者密碼頁儲存
        /// </summary>
        /// <param name="id">工號</param>
        /// <param name="user">資料</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromRoute] int? id, ChangePasswordViewModel posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.UserId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "管理者重設使用者密碼頁儲存", "錯誤，posted為null 或 id小於等於0 或 posted與id不符");
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            var entity = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(s => s.UserId == posted.UserId);
            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "管理者重設使用者密碼頁儲存", "錯誤，entity為null");
                return NotFound();
            }

            // 密碼政策（管理者重設：只套用強度）
            var policy = GetPasswordPolicy();
            ValidateNewPasswordByPolicy("UserNewPassword", posted.UserNewPasswordHash, policy);

            try
            {
                // 這是管理者重設，不用知道原本使用者密碼
                ModelState.Remove("UserCurrentPassword");// 不用驗證 原密碼

                if (!ModelState.IsValid)
                {
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "管理者重設使用者密碼頁儲存", "錯誤，必填資料未填寫或未符合密碼政策");
                    TempData["_JSShowAlert"] = _loc.T("AccountSettings.ResetPassword.ValidationFailed");
                    return RedirectToAction(nameof(Index));
                }

                entity.UserPasswordHash = HashPassword(entity, posted.UserNewPasswordHash);
                entity.UserPasswordChangedAt = DateTime.Now;

                // 將新密碼寫入歷史資料庫
                _context.UserPasswordHistories.Add(new UserPasswordHistory
                {
                    UserId = entity.UserId,
                    PasswordHash = entity.UserPasswordHash,  // 這裡是剛剛「新密碼的 Hash」，下次變更密碼判斷用
                });

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                string customErrorString = _loc.T("AccountSettings.ResetPassword.Title") + "-" + entity.UserFullName + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(customErrorString, ex, this.HttpContext);
                TempData["_JSShowAlert"] = customErrorString;
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "管理者重設使用者密碼頁儲存", customErrorString, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("AccountSettings.ResetPassword.Title") + "-" + entity.UserFullName + _loc.T("Common.Success");

            await _accessLog.NewPasswordAsync(GetLoginUser(), PageName, "變更密碼頁儲存成功", entity);

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示編輯使用者群組頁面 GET: /EditGroup/5
        /// </summary>
        /// <param name="UserId">使用者Id</param>
        /// <param name="groupIds">群組Ids</param>
        /// <returns></returns>
        public async Task<IActionResult> EditGroup([FromRoute] int? id, [FromQuery] int[]? groupIds)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯使用者群組頁面", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯使用者群組頁面", "錯誤，entity為null");
                return NotFound();
            }

            // 所有群組
            var allGroups = await _context.UserGroups
                .AsNoTracking()
                .OrderBy(g => g.UserGroupCode)
                .Select(g => new { g.UserGroupId, g.UserGroupCode, g.UserGroupDescription })
                .ToListAsync();

            // 目前DB已有的群組
            var currentGroupIds = await _context.UserGroupMembers
                .Where(m => m.UserId == id)
                .Select(m => m.UserGroupId)
                .ToListAsync();

            var vm = new UserGroupsEditViewModel
            {
                UserId = entity.UserId,
                UserAccount = entity.UserAccount,
                UserFullName = entity.UserFullName,
                SelectedUserGroupIds = currentGroupIds,
                AllUserGroups = allGroups.Select(g => new SelectListItem
                {
                    Value = g.UserGroupId.ToString(),
                    Text = g.UserGroupCode,
                    Selected = currentGroupIds.Contains(g.UserGroupId)
                }).ToList()
            };

            // 計算預覽（有效角色、有效權限）
            await ComputePreviewAsync(vm);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯使用者群組頁面");

            return View(vm);
        }

        /// <summary>
        /// 使用者群組頁儲存 POST: /EditGroup/5
        /// </summary>
        /// <param name="form">表單物件</param>
        /// <param name="command">命令(save/preview)</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup([FromRoute] int? id, UserGroupsEditPostViewModel posted, string command)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.UserId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "使用者群組頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted不符");
                return NotFound();
            }

            // 確認使用者存在
            var entity = await _context.Users.FirstOrDefaultAsync(u => u.UserId == posted.UserId);
            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "使用者群組頁儲存", "錯誤，entity為null");
                return NotFound();
            }

            // 目標群組（右側 selectedGroups 全部值；JS 在 submit 前已全部設成 selected）
            var want = posted.SelectedUserGroupIds?
                           .Distinct()
                           .ToList()
                       ?? new List<int>();

            // 目前 DB 裡的群組
            var existing = await _context.UserGroupMembers
                .Where(m => m.UserId == id)
                .Select(m => m.UserGroupId)
                .ToListAsync();

            var toAdd = want.Except(existing).ToList();
            var toRemove = existing.Except(want).ToList();

            if (toAdd.Count == 0 && toRemove.Count == 0)
            {
                TempData["_JSShowAlert"] = _loc.T("AccountSettings.EditGroup.Title") + "-" + entity.UserFullName + _loc.T("Common.NoChange");
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "使用者群組頁儲存", "未異動，不儲存");

                // 回到帳號管理清單頁
                return RedirectToAction(nameof(Index));
            }
            else
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (toRemove.Count > 0)
                    {
                        await _context.UserGroupMembers
                            .Where(m => m.UserId == id && toRemove.Contains(m.UserGroupId))
                            .ExecuteDeleteAsync();// 真的刪除(軟刪除會非常複雜)
                    }

                    foreach (var gid in toAdd)
                    {
                        _context.UserGroupMembers.Add(new UserGroupMember
                        {
                            UserId = id.Value,
                            UserGroupId = gid,
                        });
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    TempData["_JSShowSuccess"] = _loc.T("AccountSettings.EditGroup.Title") + "-" + entity.UserFullName + _loc.T("Common.Success");
                }
                catch
                {
                    await tx.RollbackAsync();
                    TempData["_JSShowAlert"] = _loc.T("AccountSettings.EditGroup.Title") + "-" + entity.UserFullName + _loc.T("Common.Failed");
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "使用者群組頁儲存", "儲存失敗");
                }
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "使用者群組頁儲存", "儲存成功");

            // 回到帳號管理清單頁
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 使用者群組權限角色預覽 AJAX：試算目前選取群組產生的「有效角色 / 有效權限」，並標出哪些是新增加的
        /// </summary>
        /// <param name="req">使用者權限預覽請求</param>
        /// <returns>使用者權限預覽結果</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewPermissions([FromBody] PreviewPermissionsRequestViewModel req)
        {
            var userId = req.UserId;
            var newGroupIds = (req.SelectedUserGroupIds ?? new()).Distinct().ToList();

            // 目前 DB 裡這個使用者實際擁有的群組
            var currentGroupIds = await _context.UserGroupMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.UserGroupId)
                .Distinct()
                .ToListAsync();

            // === 先算「目前 DB 狀態」的角色/權限 ===
            var currentRoleIds = await _context.UserGroupRoles
                .Where(ugr => currentGroupIds.Contains(ugr.UserGroupId))
                .Select(ugr => ugr.RoleId)
                .Distinct()
                .ToListAsync();

            var currentPermKeys = await _context.RolePermissions
                .Where(rp => currentRoleIds.Contains(rp.RoleId))
                .Select(rp => new { rp.ResourceId, rp.AppActionId })
                .Distinct()
                .ToListAsync();
            var currentPermSet = currentPermKeys
                .Select(x => (x.ResourceId, x.AppActionId))
                .ToHashSet();

            // === 再算「這次選取之後」的角色/權限 ===
            var roleFromGroups = await _context.UserGroupRoles
                .Where(ugr => newGroupIds.Contains(ugr.UserGroupId))
                .Join(_context.UserGroups,
                    ugr => ugr.UserGroupId,
                    ug => ug.UserGroupId,
                    (ugr, ug) => new
                    {
                        ugr.RoleId,
                        ug.UserGroupId,
                        ug.UserGroupCode
                    })
                .ToListAsync();

            var newRoleIds = roleFromGroups
                .Select(x => x.RoleId)
                .Distinct()
                .ToList();

            var roles = await _context.Roles
                .Where(r => newRoleIds.Contains(r.RoleId))
                .ToListAsync();

            var roleDtos = roles
                .Select(r => new PreviewRoleViewModel
                {
                    RoleId = r.RoleId,
                    RoleCode = r.RoleCode,
                    RoleCodeName = _loc.T($"Role.{r.RoleCode}"),
                    RoleGroup = r.RoleGroup,
                    RoleGroupName = _loc.T($"Role.{r.RoleGroup}"),
                    IsNew = !currentRoleIds.Contains(r.RoleId),   // 這裡沿用原本「是不是新角色」
                    FromGroups = roleFromGroups
                        .Where(x => x.RoleId == r.RoleId)
                        .Select(x => new PreviewRoleSourceGroupViewModel
                        {
                            UserGroupId = x.UserGroupId,
                            UserGroupCode = x.UserGroupCode
                        })
                        .Distinct()
                        .ToList()
                })
                .OrderBy(r => r.RoleGroup)
                .ThenBy(r => r.RoleCode)
                .ToList();

            // 權限：RolePermission + Resource + AppAction
            var permsRaw = await _context.RolePermissions
                .Where(rp => newRoleIds.Contains(rp.RoleId))
                .Join(_context.Resources,
                    rp => rp.ResourceId,
                    res => res.ResourceId,
                    (rp, res) => new { rp, res })
                .Join(_context.AppActions, // 你的動作表若叫 Action，就換成 context.Actions
                    j => j.rp.AppActionId,
                    act => act.AppActionId,
                    (j, act) => new
                    {
                        j.rp.RoleId,
                        j.rp.ResourceId,
                        j.rp.AppActionId,
                        j.res.ResourceKey,
                        act.AppActionCode,
                        act.AppActionOrder,
                    })
                .ToListAsync();

            // 合併同一 Resource + Action（多角色來源只看有沒有 this key）
            var permDtos = permsRaw
                .GroupBy(p => new
                {
                    p.ResourceId,
                    p.ResourceKey,
                    p.AppActionId,
                    p.AppActionCode,
                    p.AppActionOrder,
                })
                .Select(g => new PreviewPermissionViewModel
                {
                    ResourceId = g.Key.ResourceId,
                    ResourceKey = g.Key.ResourceKey,
                    ResourceDisplayName = _loc.T($"{g.Key.ResourceKey}.Index.Title"),
                    AppActionId = g.Key.AppActionId,
                    AppActionCode = g.Key.AppActionCode,
                    AppActionDisplayName = _loc.T($"AppAction.{g.Key.AppActionCode}"),
                    AppActionOrder = g.Key.AppActionOrder,
                    IsNew = !currentPermSet.Contains((g.Key.ResourceId, g.Key.AppActionId)) // 原本沒有 → 預覽
                })
                .OrderBy(p => p.ResourceDisplayName)
                .ThenBy(p => p.AppActionOrder)
                .ToList();

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "使用者群組權限角色預覽");

            return Json(new
            {
                roles = roleDtos,
                permissions = permDtos
            });
        }

        // GET: /AccountSettings/Details/5
        /// <summary>
        /// 顯示帳號明細頁
        /// </summary>
        /// <param name="userId">使用者Id</param>
        /// <returns></returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示帳號明細頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserGroupMembers)
                    .ThenInclude(m => m.UserGroup)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == id && u.DeletedAt == null);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示帳號明細頁", "錯誤，entity為null");
                return NotFound();
            }

            // 目前使用者所屬的群組 Id
            var selectedGroupIds = entity.UserGroupMembers
                .Select(m => m.UserGroupId)
                .Distinct()
                .ToList();

            // 利用「既有的」 UserGroupsEditViewModel + ComputePreviewAsync 來算有效角色/權限
            var previewVm = new UserGroupsEditViewModel
            {
                UserId = entity.UserId,
                SelectedUserGroupIds = selectedGroupIds
            };

            await ComputePreviewAsync(previewVm);

            // 組成明細頁的 ViewModel
            var vm = new UserDetailsViewModel
            {
                User = entity,
                UserGroups = entity.UserGroupList,                    // 用 User 裡 NotMapped 的 UserGroupList
                EffectiveRoles = previewVm.EffectiveRoles,          // 沿用 ComputePreviewAsync 算出來的結果
                EffectivePermissions = previewVm.EffectivePermissions
            };

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示帳號明細頁");

            return View(vm);
        }

        /// <summary>
        /// 顯示註冊 TOTP (Google Authenticator) 頁面
        /// </summary>
        public async Task<IActionResult> RegisterTotp()
        {

            bool TwoFA_ENABLED = _param.GetBool("SEC_2FA_ENABLED");

            // 不用2FA
            if (!TwoFA_ENABLED)
            {
                return RedirectToAction("Index", "Home");
            }

            // 1) 找出目前登入的使用者
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                TempData["_JSShowAlert"] = _loc.T("Auth.LoginInfoExpired");
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["_JSShowAlert"] = _loc.T("Auth.UserNotFoundReLogin");
                return RedirectToAction("Index", "Home");
            }

            // 是否已經啟用 TOTP（已有 Secret）
            ViewBag.IsAlreadyEnabled = !string.IsNullOrWhiteSpace(user.UserTotpSecret);

            // 2) 嘗試從 Session 取出尚未完成的註冊狀態
            var state = HttpContext.Session.GetObject<TotpSetupState>(TotpSetupSessionKey);
            if (state == null || state.UserId != user.UserId)
            {
                // 3) 沒有就重新產生一組 Secret + otpauth URI

                // Issuer 建議用系統名稱，可視情況改成 Parameter 取值
                // TODO：正式網站可改成公司名稱或系統名稱
                var issuer = _param.GetString("SITE_NAME") ?? "DEMO SITE";
                var accountLabel = user.UserAccount;

                // 產生 20 bytes 隨機 secret，轉成 Base32
                var secretBytes = new byte[20];
                RandomNumberGenerator.Fill(secretBytes);
                var secretBase32 = Base32Encoding.ToString(secretBytes); // OtpNet 提供的 Base32

                var encodedIssuer = Uri.EscapeDataString(issuer);
                var encodedAccount = Uri.EscapeDataString(accountLabel);

                // otpauth://totp/{issuer}:{account}?secret=...&issuer=...&digits=6&period=30
                var otpauthUri =
                    $"otpauth://totp/{encodedIssuer}:{encodedAccount}" +
                    $"?secret={secretBase32}&issuer={encodedIssuer}&digits=6&period=30";

                state = new TotpSetupState
                {
                    UserId = user.UserId,
                    Secret = secretBase32,
                    Issuer = issuer,
                    AccountLabel = accountLabel,
                    OtpauthUri = otpauthUri
                };

                HttpContext.Session.SetObject(TotpSetupSessionKey, state);
            }

            // 4) 組 ViewModel
            var vm = new TotpSetupViewModel
            {
                UserAccount = user.UserAccount,
                Issuer = state.Issuer,
                Secret = state.Secret,
                OtpauthUri = state.OtpauthUri
            };

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示註冊 TOTP 頁面");

            return View(vm);
        }

        /// <summary>
        /// 註冊 TOTP 頁面送出（驗證使用者輸入的 6 碼）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterTotp(TotpSetupViewModel model)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                TempData["_JSShowAlert"] = _loc.T("Auth.LoginInfoExpired");
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["_JSShowAlert"] = _loc.T("Auth.UserNotFoundReLogin");
                return RedirectToAction("Index", "Home");
            }

            var state = HttpContext.Session.GetObject<TotpSetupState>(TotpSetupSessionKey);
            if (state == null || state.UserId != user.UserId)
            {
                TempData["_JSShowAlert"] = _loc.T("Totp.SessionExpired");
                return RedirectToAction(nameof(RegisterTotp));
            }

            if (!ModelState.IsValid)
            {
                // 把狀態補回去（避免畫面無法顯示 QR / Secret）
                model.UserAccount = user.UserAccount;
                model.Issuer = state.Issuer;
                model.Secret = state.Secret;
                model.OtpauthUri = state.OtpauthUri;

                return View(model);
            }

            // 用暫時 user 物件來驗證（VerifyTotpCodeAsync 只會用到 TotpSecret）
            var tempUser = new User
            {
                UserTotpSecret = state.Secret
            };

            var ok = await VerifyTotpCodeAsync(tempUser, model.Code);
            if (!ok)
            {
                ModelState.AddModelError(nameof(TotpSetupViewModel.Code), _loc.T("Totp.VerifyFailed"));

                model.UserAccount = user.UserAccount;
                model.Issuer = state.Issuer;
                model.Secret = state.Secret;
                model.OtpauthUri = state.OtpauthUri;

                return View(model);
            }

            // 驗證成功 → 寫入 DB（正式啟用 TOTP）
            user.UserTotpSecret = state.Secret;
            await _context.SaveChangesAsync();

            // 清除暫存狀態
            HttpContext.Session.Remove(TotpSetupSessionKey);

            TempData["_JSShowSuccess"] = _loc.T("Totp.SetupSuccess");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "註冊 TOTP 成功");

            // 啟用後你可以選擇要導去哪裡：帳號明細 / 安全設定 / 首頁等
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示 TOTP 註冊用 QR Code 圖片 (PNG)
        /// </summary>
        public async Task<IActionResult> TotpQrCode()
        {
            var state = HttpContext.Session.GetObject<TotpSetupState>(TotpSetupSessionKey);
            if (state == null || string.IsNullOrWhiteSpace(state.OtpauthUri))
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示TOTP註冊用QRCode圖片", "錯誤，state為null 或 state.OtpauthUri為空");
                return NotFound();
            }

            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(state.OtpauthUri, QRCodeGenerator.ECCLevel.Q);
                var pngQr = new PngByteQRCode(qrData);
                var bytes = pngQr.GetGraphic(20); // 20: pixel size

                return File(bytes, "image/png");
            }
            catch (Exception ex)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示TOTP註冊用QRCode圖片", "錯誤，無法顯示");
                Utilities.WriteExceptionIntoLogFile("產生 TOTP QR Code 失敗", ex, this.HttpContext);
                return NotFound();
            }
        }

        /// <summary>
        /// 計算「有效角色」與「有效權限」並放入 ViewModel
        /// </summary>
        [NonAction]
        private async Task ComputePreviewAsync(UserGroupsEditViewModel vm)
        {
            var selected = vm.SelectedUserGroupIds?.Distinct().ToList() ?? new();

            // 1) 先把群組→角色對照拉回來 (在記憶體)
            var roleFromGroups = await _context.UserGroupRoles
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
            var roles = await _context.Roles
                .Where(r => roleIds.Contains(r.RoleId))
                .AsNoTracking()
                .OrderBy(r => r.RoleGroup)
                .ThenBy(r => r.RoleCode)
                .ToListAsync();

            vm.EffectiveRoles = roles.Select(r => new EffectiveRoleViewModel
            {
                RoleId = r.RoleId,
                RoleCode = r.RoleCode,
                RoleGroup = r.RoleGroup,
                FromUserGroupIds = roleIdToGroupIds.TryGetValue(r.RoleId, out var list) ? list : new List<int>()
            }).ToList();

            // ===== 以下原本權限計算維持不動（你這段是在記憶體再做 GroupBy，不會有翻譯問題） =====

            var actionOrders = await _context.AppActions
                .Select(a => new { a.AppActionId, a.AppActionOrder, a.AppActionCode, a.AppActionDisplayName })
                .AsNoTracking()
                .ToListAsync();

            var actionOrderMap = actionOrders.ToDictionary(a => a.AppActionId, a => a.AppActionOrder);

            var rawPerms = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Join(_context.Resources, rp => rp.ResourceId, res => res.ResourceId, (rp, res) => new { rp, res })
                .Join(_context.AppActions, j => j.rp.AppActionId, act => act.AppActionId, (j, act) => new
                {
                    j.rp.RoleId,
                    j.rp.ResourceId,
                    j.rp.AppActionId,
                    j.res.ResourceKey,
                    act.AppActionCode,
                })
                .AsNoTracking()
                .ToListAsync();

            var permGroups = rawPerms
                .GroupBy(p => new { p.ResourceId, p.ResourceKey, p.AppActionId, p.AppActionCode })
                .Select(g => new EffectivePermissionViewModel
                {
                    ResourceId = g.Key.ResourceId,
                    ResourceKey = g.Key.ResourceKey,
                    ResourceDisplayName = _loc.T($"{g.Key.ResourceKey}.Index.Title"),
                    AppActionId = g.Key.AppActionId,
                    AppActionCode = g.Key.AppActionCode,
                    AppActionDisplayName = _loc.T($"AppAction.{g.Key.AppActionCode}"),
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
            IQueryable<User> q = _context.Users
                .Include(u => u.Department)
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


            // 依 RoleId 篩選
            if (queryModel.RoleId != null && queryModel.RoleId.Any())
            {
                var roleIds = queryModel.RoleId.ToList(); // 多筆 RoleId

                // 找出所有擁有任一 RoleId 的 UserId
                var userIdsWithRolesQuery =
                    from ugm in _context.UserGroupMembers
                    join ugr in _context.UserGroupRoles
                        on ugm.UserGroupId equals ugr.UserGroupId
                    join rp in _context.RolePermissions
                        on ugr.RoleId equals rp.RoleId
                    where roleIds.Contains(rp.RoleId)
                    select ugm.UserId;

                // 套用條件：UserId 落在 userIdsWithRolesQuery 裡
                q = q.Where(u => userIdsWithRolesQuery.Contains(u.UserId));
            }


            // 4) 排序（用屬性名白名單）
            q = q.OrderByWhitelist(
            queryModel.OrderBy,          // 例如 "UserAccount" / "UserFullName" / "UserLastLoginAt"
            queryModel.SortDir,          // "asc" / "desc"
            TableHeaders,                // Key=屬性名, Value=顯示文字
            tiebreakerProperty: "UserId"     // 第2排序欄位：主鍵
        );

            // 5) 分頁＋總筆數
            var (entities, totalCount) =
                await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

            // 讓 NotMapped 計算屬性可以用多語系 Loc.T(...)
            entities.WithLoc(_loc);

            // Department 也要有 Loc（不然 DepartmentName 抓不到）
            foreach (var m in entities)
            {
                if (m.Department != null)
                {
                    m.Department.WithLoc(_loc);
                }
            }


            // 6) 投影顯示：把 RoleCode 串成字串（在記憶體端處理）
            var shaped = entities.Select(u => new
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
                u.RoleCodeList,
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

        /// <summary>
        /// 歷史密碼檢查
        /// </summary>
        /// <param name="user">使用者</param>
        /// <param name="newPassword">新密碼</param>
        /// <param name="policy">密碼政策</param>
        /// <param name="ct">取消標記</param>
        /// <returns></returns>
        [NonAction]
        private async Task<bool> CheckPasswordHistoryAsync(User user, string newPassword, PasswordPolicy policy, CancellationToken ct = default)
        {
            // 沒開啟政策，或沒有設定要檢查歷史次數 -> 直接通過
            if (!policy.PolicyEnabled || policy.HistoryCount <= 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                // 這種情況通常會在前面就被擋掉，但保險起見
                return true;
            }

            // 1) 檢查「現在這個密碼」是否與新密碼相同
            //    不論歷史次數怎麼設定，通常都不允許改成目前正在使用的密碼
            if (VerifyHashedPassword(user, user.UserPasswordHash, newPassword)
                == PasswordVerificationResult.Success)
            {
                return false;
            }

            // 2) 抓最近 N 筆歷史密碼（依照 SEC_PASSWORD_HISTORY_COUNT）
            var histories = await _context.UserPasswordHistories
                .Where(h => h.UserId == user.UserId)
                .OrderByDescending(h => h.CreatedAt)
                .Take(policy.HistoryCount)
                .ToListAsync(ct);

            // 3) 逐筆用 VerifyHashedPassword 比對
            foreach (var h in histories)
            {
                var result = VerifyHashedPassword(user, h.PasswordHash, newPassword);
                if (result == PasswordVerificationResult.Success)
                {
                    // 命中歷史密碼
                    return false;
                }
            }

            // 沒有命中 -> 通過
            return true;
        }



    }
}
