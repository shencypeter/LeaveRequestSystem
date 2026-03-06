using BioMedDocManager.Enums;
using BioMedDocManager.Extensions;
using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;

namespace BioMedDocManager.Controllers
{

    /// <summary>
    /// 登入/登出控制器
    /// </summary>
    /// <param name="httpAccessor">取得HttpContext，例如Session、Request等</param>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    
    public class LoginController(IHttpContextAccessor httpAccessor, DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IMailHelper _mailHelper, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "登入";

        /// <summary>
        /// 登入畫面
        /// </summary>
        /// <param name="returnUrl">導回原始頁面的URL</param>
        /// <returns></returns>       
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl)
        {
            //密碼錯誤退回來時, 記得剛才的帳號
            TempData["lastUserId"] = HttpContext.Session.GetString("try_login");

            if (returnUrl != null && returnUrl.EndsWith("Login"))
            {
                //return URL 防止回到登入頁面
                returnUrl = "";
            }

            // 將 returnUrl 存入ViewBag
            ViewBag.ReturnUrl = returnUrl;

            TempData["Messages"] = "登入公告文字";// TODO：這裡是範例，可以改成從資料庫讀取

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string userAccount, string password, string? captcha, string? returnUrl)
        {
            try
            {
                // ================== 驗證碼檢查 ==================
                var captchaResult = await ValidateCaptchaAsync(userAccount, captcha);
                if (captchaResult != null)
                {
                    return captchaResult;
                }

                HttpContext.Session.SetString("try_login", userAccount);

                // 取得使用者
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserAccount == userAccount);

                // 不存在或未啟用（訊息仍給通用字樣）
                if (user is null || !user.UserIsActive)
                {
                    TempData["_JSShowAlert"] = _loc.T("Auth.InvalidCredentialsForgotHint");

                    await _accessLog.NewLoginFailedAsync(
                        user is null ? AccountType.Unknow : AccountType.Admin,
                        userAccount,
                        user?.UserId ?? 0,
                        PageName,
                        "登入",
                        1,
                        user is null ? "登入失敗：帳號不存在" : "登入失敗：帳號未啟用"
                    );

                    return RedirectToAction(nameof(Index));
                }

                // ================== 讀取安全設定（全部由 Parameter 控制） ==================
                var sec = LoadSecuritySettingsFromParameter();
                // ===================================================================

                // ================== 帳號是否被鎖定處理 ==================
                var lockedResult = await HandleLockedUserIfNeededAsync(user, sec);
                if (lockedResult != null)
                {
                    return lockedResult;
                }

                // ================== 驗證密碼 + 登入失敗計數 / 鎖定 ==================
                var verifyResult = await VerifyPasswordAndHandleFailedAsync(user, password, sec);
                if (verifyResult != null)
                {
                    return verifyResult;
                }

                // ================== 密碼驗證成功後：判斷過期 / 首次登入 / 2FA ==================
                bool mustChangePassword;
                bool requireFirstLoginChange;
                bool requireExpireChange;
                bool need2Fa;

                EvaluatePostLoginFlags(
                    user,
                    sec,
                    out mustChangePassword,
                    out requireFirstLoginChange,
                    out requireExpireChange,
                    out need2Fa
                );

                // 用Session 記錄「必須先變更密碼」旗標，讓全域 Middleware 來控制 Redirect
                if (mustChangePassword)
                {
                    SetForceChangePasswordSession(
                        mustChangePassword,
                        requireFirstLoginChange,
                        requireExpireChange
                    );
                }

                // ===== 5. 若需要 2FA：進入二階段驗證流程（此時尚未完整登入） =====
                if (need2Fa)
                {
                    string? sanitizedReturnUrl = null;
                    if (!string.IsNullOrEmpty(returnUrl)
                        && Url.IsLocalUrl(returnUrl)
                        && !returnUrl.Contains("Login", StringComparison.OrdinalIgnoreCase))
                    {
                        sanitizedReturnUrl = returnUrl;
                    }

                    // 這裡把「之後要不要強制改密碼」的資訊一起丟進 2FA 狀態，2FA 通過後才 SignIn + 設 Session
                    await StartTwoFactorFlowAsync(
                        user,
                        sec,
                        sanitizedReturnUrl
                    );

                    return RedirectToAction(nameof(TwoFactor));
                }

                // ===== 6. 不需要 2FA：直接登入成功流程 =====
                await SignInAndBuildSessionAsync(user, mustChangePassword);

                // 不需要 2FA 的情況，剛剛已經 SetForceChangePasswordSession 了
                // 如果為 false，這裡可以再保險清一次：
                if (!mustChangePassword)
                {
                    SetForceChangePasswordSession(false, false, false);
                }


                // 一般成功登入 Redirect
                if (!string.IsNullOrEmpty(returnUrl)
                    && Url.IsLocalUrl(returnUrl)
                    && !returnUrl.Contains("Login", StringComparison.OrdinalIgnoreCase))
                {
                    var controller = returnUrl.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
                    return Redirect($"/{controller}");
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteExceptionIntoLogFile("登入異常", ex, this.HttpContext);

                await _accessLog.NewLoginFailedAsync(
                    AccountType.Admin,
                    userAccount ?? "",
                    0,
                    PageName,
                    "登入",
                    3,
                    $"登入異常：{ex.Message}"
                );
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// 2FA 驗證頁（讓使用者選 Email / TOTP，並輸入驗證碼）
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> TwoFactor()
        {
            var state = HttpContext.Session.GetObject<TwoFactorState>(AppSettings.TwoFactorSessionKey);
            if (state == null)
            {
                TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.SessionExpiredReLogin");
                return RedirectToAction(nameof(Index));
            }

            // 可用的 2FA 方法資訊傳給 View
            ViewBag.CanUseEmail = state.CanUseEmail;
            ViewBag.CanUseTotp = state.CanUseTotp;

            var prefer = TempData["TwoFactor.SelectedProvider"] as string;

            string selected = null;
            if (string.Equals(prefer, "Email", StringComparison.OrdinalIgnoreCase) && state.CanUseEmail)
            {
                selected = "Email";
            }
            else if (string.Equals(prefer, "Totp", StringComparison.OrdinalIgnoreCase) && state.CanUseTotp)
            {
                selected = "Totp";
            }

            if (string.IsNullOrEmpty(selected))
            {
                selected = state.CanUseTotp ? "Totp" : "Email";
            }

            ViewBag.DefaultProvider = selected;

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示二階段驗證頁");

            return View();
        }

        /// <summary>
        /// 二階段驗證送出（Email / TOTP 共用）
        /// </summary>
        /// <param name="code">驗證碼</param>
        /// <param name="provider">驗證方式：Email / Totp</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyTwoFactor(string code, string provider)
        {
            // 從 Session 取出先前 Login 建立的 2FA 狀態
            var state = HttpContext.Session.GetObject<TwoFactorState>(AppSettings.TwoFactorSessionKey);
            if (state == null)
            {
                TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.SessionExpiredReLogin");
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(provider))
            {
                TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.SelectProvider");
                return RedirectToAction(nameof(TwoFactor));
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.EnterCode");
                return RedirectToAction(nameof(TwoFactor));
            }

            // 正規化 provider
            var mode = provider.Trim();
            var codeUpper = code.Trim().ToUpper();
            bool isEmail = mode.Equals("Email", StringComparison.OrdinalIgnoreCase);
            bool isTotp = mode.Equals("Totp", StringComparison.OrdinalIgnoreCase);

            if (!isEmail && !isTotp)
            {
                TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.InvalidProvider");
                return RedirectToAction(nameof(TwoFactor));
            }

            // ===== 1) Email OTP 驗證流程 =====
            if (isEmail)
            {
                if (!state.CanUseEmail)
                {
                    TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.EmailNotEnabled");
                    return RedirectToAction(nameof(TwoFactor));
                }

                if (!state.EmailOtpExpiresAt.HasValue || state.EmailOtpExpiresAt.Value < DateTime.UtcNow)
                {
                    TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.CodeExpiredResend");
                    return RedirectToAction(nameof(TwoFactor));
                }

                if (state.EmailOtpVerifyFailCount >= 5)
                {
                    TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.TooManyAttemptsReLogin");
                    HttpContext.Session.Remove(AppSettings.TwoFactorSessionKey);
                    return RedirectToAction(nameof(Index));
                }

                var inputHash = ComputeSha256(codeUpper);
                if (!SecureEqualsHash(state.EmailOtpHash, inputHash))
                {
                    state.EmailOtpVerifyFailCount++;
                    HttpContext.Session.SetObject(AppSettings.TwoFactorSessionKey, state);

                    TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.CodeInvalidRetry");
                    return RedirectToAction(nameof(TwoFactor));
                }

                // Email OTP 驗證成功，往下走共用成功流程
            }

            // ===== 2) TOTP 驗證流程 =====
            if (isTotp)
            {
                if (!state.CanUseTotp)
                {
                    TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.TotpNotEnabled");
                    return RedirectToAction(nameof(TwoFactor));
                }
                var userForTotp = await _context.Users.FindAsync(state.UserId);
                if (userForTotp == null)
                {
                    TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.UserInfoExpiredReLogin");
                    HttpContext.Session.Remove(AppSettings.TwoFactorSessionKey);
                    return RedirectToAction(nameof(Index));
                }

                var totpOk = await VerifyTotpCodeAsync(userForTotp, code);
                if (!totpOk)
                {
                    TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.CodeInvalidOrExpiredRetry");
                    return RedirectToAction(nameof(TwoFactor));
                }

                // TOTP 驗證成功，往下走共用成功流程
            }

            // ===== 3) 驗證成功：清除 2FA 狀態，正式登入 =====
            var user = await _context.Users.FindAsync(state.UserId);
            if (user == null)
            {
                TempData["_JSShowAlert"] = _loc.T("Auth.LoginInfoExpired");
                HttpContext.Session.Remove(AppSettings.TwoFactorSessionKey);
                return RedirectToAction(nameof(Index));
            }

            // 驗證成功 → 不再需要 2FA 狀態
            HttpContext.Session.Remove(AppSettings.TwoFactorSessionKey);

            // 2FA 通過後才真正建立 Claims / Cookie / Menu / LoginSuccessLog
            // 第二個參數仍然帶入 mustChangePassword（這樣 Claims 可帶 MustChangePassword 或其他資訊）
            await SignInAndBuildSessionAsync(user, state.MustChangePassword);

            // 回原本的 returnUrl（若有），否則到首頁
            if (!string.IsNullOrEmpty(state.ReturnUrl) &&
                Url.IsLocalUrl(state.ReturnUrl) &&
                !state.ReturnUrl.Contains("Login", StringComparison.OrdinalIgnoreCase))
            {
                var controller = state.ReturnUrl.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
                return Redirect($"/{controller}");
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// 重新寄送二階段驗證 Email OTP
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> SendTwoFactorEmailCode()
        {
            var state = HttpContext.Session.GetObject<TwoFactorState>(AppSettings.TwoFactorSessionKey);
            if (state == null)
            {
                TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.SessionExpiredReLogin");
                return RedirectToAction(nameof(Index));
            }

            if (!state.CanUseEmail)
            {
                TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.EmailNotEnabled");
                return RedirectToAction(nameof(TwoFactor));
            }

            // 取得使用者
            var user = await _context.Users.FindAsync(state.UserId);
            if (user == null)
            {
                TempData["_JSShowAlert"] = _loc.T("Auth.LoginInfoExpired");
                HttpContext.Session.Remove(AppSettings.TwoFactorSessionKey);
                return RedirectToAction(nameof(Index));
            }

            // 產生新 OTP
            var otp = GenerateRandomCode(6);
            var nowUtc = DateTime.UtcNow;

            state.EmailOtpHash = ComputeSha256(otp);
            state.EmailOtpExpiresAt = nowUtc.AddMinutes(AppSettings.TwoFactorEmailOtpExpireTime);
            state.EmailOtpVerifyFailCount = 0;

            // 寄信
            await SendTwoFactorEmailCodeAsync(user, otp);

            // 更新 Session
            HttpContext.Session.SetObject(AppSettings.TwoFactorSessionKey, state);

            TempData["_JSShowAlert"] = _loc.T("Login.TwoFactor.EmailSentCheckInbox");

            // 設定目前選擇的 provider 為 Email
            TempData["TwoFactor.SelectedProvider"] = "Email";

            // 回到二階段驗證畫面
            return RedirectToAction(nameof(TwoFactor));
        }

        /// <summary>
        /// 登出
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            // 1️ 清空所有 Session（包含所有 culture 的 MenuTree）
            HttpContext.Session.Clear();

            // 2 記錄登出行為
            await _accessLog.NewLogoutAsync(GetLoginUser());

            // 3 登出 Cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 4️ 顯示提示訊息
            TempData["_JSShowAlert"] = _loc.T("Auth.LogoutThanks");

            return RedirectToAction("Index", "Home");
        }


        /// <summary>
        /// 取得驗證碼
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public IActionResult GetCaptcha()
        {
            string code = GenerateRandomCode(5); // e.g., "A3X9B"
            httpAccessor.HttpContext.Session.SetString("CaptchaCode", code);

            byte[] imageBytes = GenerateCaptchaImage(code);
            return File(imageBytes, "image/png");
        }

        /// <summary>
        /// 初次遷移用：將所有明碼密碼轉換成hash密碼
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Migrate()
        {

#if DEBUG
            //只有DEBUG模式才可以使用
            var users = await _context.Users
                .ToListAsync();

            foreach (var user in users)
            {
                user.UserPasswordHash = HashPassword(user, "Abcd" + user.UserAccount);
            }

            await _context.SaveChangesAsync();

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "初次遷移密碼");
#endif


            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 登入成功後的共通流程：
        /// - 清除失敗次數與鎖定狀態
        /// - 登入稽核（時間 + IP）
        /// - 計算有效角色，建立 Claims 與 Cookie
        /// - 建立選單樹存入 Session
        /// - 寫登入成功紀錄
        /// </summary>
        [NonAction]
        private async Task SignInAndBuildSessionAsync(User user, bool mustChangePassword)
        {
            // 成功：清除失敗與鎖定
            user.UserLoginFailedCount = 0;
            user.UserIsLocked = false;
            user.UserLockedUntil = null;
            await _context.SaveChangesAsync();

            // 登入稽核（時間+IP，內部應會更新 UserLastLoginAt）
            await SetUserLoginAuditAsync(user);

            // ===== 1. 透過 UserGroupMember → UserGroupRole → Role 算出有效角色 =====
            var rolesFromGroups = await _context.UserGroupMembers
                .Where(ugm => ugm.UserId == user.UserId)
                .SelectMany(ugm => ugm.UserGroup!.UserGroupRoles)   // 到 UserGroupRole
                .Where(ugr => ugr.Role != null)
                .Select(ugr => ugr.Role!)
                .Distinct()
                .ToListAsync();

            var allRoles = rolesFromGroups;

            // ===== 2. 建立 Claims =====
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new("UserFullName", user.UserFullName),
                new("UserAccount", user.UserAccount)
            };

            // 角色名稱 → 標準 Role Claim：之後 [Authorize(Roles = "xxx")] 會吃這個
            foreach (var r in allRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, r.RoleCode));
            }

            // 角色群組（自訂 ClaimType，方便你之後用來分群或顯示）
            foreach (var g in allRoles
                                 .Select(x => x.RoleGroup)
                                 .Where(x => !string.IsNullOrEmpty(x))
                                 .Distinct())
            {
                claims.Add(new Claim("RoleGroup", g));
            }

            if (mustChangePassword)
            {
                claims.Add(new Claim("MustChangePassword", "Y"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // ===== 3. 建立選單樹(依照目前語言，只做一次，切換語言寫在切換那邊) =====
            var menuTree = BuildMenuTreeForUser(principal);
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name; // "zh-TW" / "en-US"
            var cultureSessionKey = $"MenuTree::{culture}";
            HttpContext.Session.SetObject(cultureSessionKey, menuTree);

            await _accessLog.NewLoginSuccessAsync(user);
        }

        /// <summary>
        /// 寄送二階段驗證 Email OTP 的實際動作。
        /// </summary>
        /// <param name="user">使用者</param>
        /// <param name="otp">OTP驗證碼</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        [NonAction]
        private async Task SendTwoFactorEmailCodeAsync(User user, string code)
        {
            if (string.IsNullOrWhiteSpace(user.UserEmail))
            {
                // 沒 Email 就不寄，必要時寫 log
                Utilities.WriteExceptionIntoLogFile(
                    $"使用者 {user.UserAccount} 沒有設定 Email，無法寄送 2FA 驗證碼。",
                    new Exception("UserEmail is empty")
                );
                return;
            }

            var to = new List<string> { user.UserEmail };

            var SiteName = _param.GetString("SITE_NAME") ?? "範例網站";

            var subject = $"【{SiteName}】二階段驗證碼";
            var body = string.Format(
                AppSettings.TwoFactorEmailBodyTemplate,
                user.UserFullName,
                code,
                AppSettings.TwoFactorEmailOtpExpireTime
            );


            await _mailHelper.SendMailAsync(
                subject: subject,
                body: body,
                toGroup: to,
                ccGroup: null,
                bccGroup: null,
                attachmentGroup: null,
                isBodyHtml: true
            );
        }

        /// <summary>
        /// 密碼驗證成功後，依「首次登入」、「密碼過期」與 2FA 設定算出後續旗標。
        /// </summary>
        [NonAction]
        private void EvaluatePostLoginFlags(
            User user,
            PasswordPolicy sec,
            out bool mustChangePassword,
            out bool requireFirstLoginChange,
            out bool requireExpireChange,
            out bool need2Fa)
        {


            DateTime now = DateTime.Now;

            mustChangePassword = false;
            requireFirstLoginChange = false;
            requireExpireChange = false;
            need2Fa = false;

            if (!sec.PolicyEnabled)
            {
                return;
            }

            // --- 首次登入強制改密碼 ---
            if (sec.ForceChangeFirstLoginFlag)
            {
                var isFirstLogin = !user.UserLastLoginAt.HasValue;
                if (isFirstLogin)
                {
                    mustChangePassword = true;
                    requireFirstLoginChange = true;
                }
            }

            // --- 密碼過期天數（0 = 不過期）---
            if (sec.PasswordExpireDays.HasValue && sec.PasswordExpireDays.Value > 0)
            {
                var lastChangeTime = user.UserPasswordChangedAt ?? user.CreatedAt;
                if (lastChangeTime.Value.AddDays(sec.PasswordExpireDays.Value) < now)
                {
                    mustChangePassword = true;
                    requireExpireChange = true;
                }
            }

            // --- 2FA 判斷：同時看「全域」＋「這個使用者是否真的有東西可用」 ---
            if (sec.Sec2faEnabled)
            {
                // 對「這個 user」而言，Email / TOTP 是否真的可用
                bool canUseEmail =
                    sec.Sec2faEmailEnabled &&
                    !string.IsNullOrWhiteSpace(user.UserEmail);   // 需要有 Email

                bool canUseTotp =
                    sec.Sec2faTotpEnabled &&
                    !string.IsNullOrWhiteSpace(user.UserTotpSecret);  // 需要有 TOTP Secret（已綁定過）

                if (canUseEmail || canUseTotp)
                {
                    need2Fa = true;
                }
                else
                {
                    // 全域雖然開了 2FA，但這個帳號完全沒有 2FA 方法 → 視為「暫時不啟用 2FA」
                    need2Fa = false;
                }
            }
        }

        /// <summary>
        /// 驗證碼檢查（RELEASE 模式才啟用）；
        /// 驗證失敗時會寫登入失敗紀錄並回傳 RedirectResult，成功則回傳 null。
        /// </summary>
        /// <param name="userAccount">帳號</param>
        /// <param name="captcha">驗證碼</param>
        /// <returns></returns>
        [NonAction]
        private async Task<IActionResult?> ValidateCaptchaAsync(string userAccount, string? captcha)
        {
#if RELEASE
    var storedCaptcha = httpAccessor.HttpContext.Session.GetString("CaptchaCode");
    if (string.IsNullOrEmpty(captcha) || !string.Equals(captcha, storedCaptcha, StringComparison.OrdinalIgnoreCase))
    {
        TempData["_JSShowAlert"] = _loc.T("Login.Captcha.Invalid");

        // 驗證碼錯誤 → 失敗紀錄
        await _accessLog.NewLoginFailedAsync(
            AccountType.Admin,                    // 後台登入
            userAccount ?? "",
            0,                                    // 沒 user
            PageName,
            "登入",
            1,
            "驗證碼錯誤"
        );

        return RedirectToAction(nameof(Index));
    }
#endif
            return null;
        }

        /// <summary>
        /// 若使用者已被鎖定，檢查是否還在鎖定期；
        /// 還在鎖定期則回傳 RedirectResult，否則自動解鎖並回傳 null。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="sec"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        [NonAction]
        private async Task<IActionResult?> HandleLockedUserIfNeededAsync(User user, PasswordPolicy sec)
        {

            DateTime now = DateTime.Now;

            if (!user.UserIsLocked)
            {
                return null;
            }

            if (user.UserLockedUntil.HasValue && user.UserLockedUntil.Value > now)
            {
                // 尚在鎖定期
                TempData["_JSShowAlert"] =
                 _loc.T("Auth.LockedRetry.Prefix")
                 + (sec.FailedLimit == int.MaxValue ? 0 : sec.FailedLimit)
                 + _loc.T("Auth.LockedRetry.Middle")
                 + sec.LockMinutes
                 + _loc.T("Auth.LockedRetry.Suffix");

                await _accessLog.NewLoginFailedAsync(
                    AccountType.Admin,
                    user.UserAccount,
                    user.UserId,
                    PageName,
                    "登入",
                    2,
                    $"登入失敗：帳號鎖定中，直到 {user.UserLockedUntil:yyyy-MM-dd HH:mm:ss}"
                );

                return RedirectToAction(nameof(Index));
            }

            // 鎖定已過期 → 自動解鎖
            user.UserIsLocked = false;
            user.UserLockedUntil = null;
            user.UserLoginFailedCount = 0;
            await _context.SaveChangesAsync();

            return null;
        }

        /// <summary>
        /// 驗證密碼；若失敗則依設定累加失敗次數 / 可能鎖定帳號 / 寫 log / 回傳 RedirectResult；
        /// 若成功則回傳 null。
        /// </summary>
        [NonAction]
        private async Task<IActionResult?> VerifyPasswordAndHandleFailedAsync(User user, string password, PasswordPolicy sec)
        {
            DateTime now = DateTime.Now;

            // 驗證密碼
            var hasher = new PasswordHasher<User>();
            var verify = hasher.VerifyHashedPassword(user, user.UserPasswordHash, password);

            if (verify == PasswordVerificationResult.Success)
            {
                return null;
            }

            // 失敗：累加計數
            user.UserLoginFailedCount = (user.UserLoginFailedCount < int.MaxValue)
                ? user.UserLoginFailedCount + 1
                : user.UserLoginFailedCount;

            string failMessage = "登入失敗：密碼錯誤";
            int severity = 1;

            // 達門檻 → 鎖定一段時間（只有當 FailedLimit != int.MaxValue 且 LockMinutes > 0 才有意義）
            if (sec.FailedLimit != int.MaxValue &&
                sec.LockMinutes > 0 &&
                user.UserLoginFailedCount >= sec.FailedLimit)
            {
                user.UserIsLocked = true;
                user.UserLockedUntil = now.AddMinutes(sec.LockMinutes);

                TempData["_JSShowAlert"] =
                    _loc.T("Auth.LockedRetry.Prefix")
                    + sec.FailedLimit
                    + _loc.T("Auth.LockedRetry.Middle")
                    + sec.LockMinutes
                    + _loc.T("Auth.LockedRetry.Suffix");

                failMessage =
                    $"密碼錯誤達 {sec.FailedLimit} 次以上，帳號鎖定至 {user.UserLockedUntil:yyyy-MM-dd HH:mm:ss}";
                severity = 2;

                // 達門檻後重設計數
                user.UserLoginFailedCount = 0;
            }
            else
            {
                // 一律回通用訊息（避免洩漏是帳號還是密碼問題）
                TempData["_JSShowAlert"] = _loc.T("Auth.InvalidCredentialsForgotHint");
            }

            // 密碼錯誤 → 記錄
            await _accessLog.NewLoginFailedAsync(
                AccountType.Admin,
                user.UserAccount,
                user.UserId,
                PageName,
                "登入",
                severity,
                failMessage
            );

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 開始二階段驗證流程：
        /// - 將可用的 2FA 方法等資訊存入 Session
        /// - 若只有 Email 且尚未產生 OTP，可在這裡先寄出一次
        /// </summary>
        [NonAction]
        private async Task StartTwoFactorFlowAsync(User user, PasswordPolicy sec, string? returnUrl)
        {
            var now = DateTime.UtcNow;

            // 這邊也要用「全域 + user 狀態」去算
            bool canUseEmail =
                sec.Sec2faEnabled &&
                sec.Sec2faEmailEnabled &&
                !string.IsNullOrWhiteSpace(user.UserEmail);

            bool canUseTotp =
                sec.Sec2faEnabled &&
                sec.Sec2faTotpEnabled &&
                !string.IsNullOrWhiteSpace(user.UserTotpSecret);

            // 如果真的兩種都沒有，就不應該進來（保險起見再防呆一次）
            if (!canUseEmail && !canUseTotp)
            {
                // 理論上 EvaluatePostLoginFlags 就不會把 need2Fa 設成 true
                // 這裡就直接當作沒 2FA，什麼都不做。
                return;
            }

            // 先決定預設 provider：有 Email 優先 Email，否則 Totp
            var defaultProvider = canUseEmail ? "Email" : "Totp";

            var state = new TwoFactorState
            {
                UserId = user.UserId,
                CanUseEmail = canUseEmail,
                CanUseTotp = canUseTotp,
                DefaultProvider = defaultProvider,
                MustChangePassword = false,           // 這三個是剛剛 EvaluatePostLoginFlags 的結果
                RequireFirstLoginChange = false,
                RequireExpireChange = false,
                ReturnUrl = returnUrl,
            };

            // 如果 Email 方式可用，就順便產生 OTP & 寄信 -> 先註解，如果選Authenticator驗證碼，但是每次進入頁面還是都會發email，浪費資源
            /*
            if (canUseEmail)
            {
                // 產生 6 碼 OTP
                var otp = GenerateRandomCode(6);              // 你可以自己實作，或用 RNGCryptoServiceProvider
                state.EmailOtpHash = ComputeSha256(otp);
                state.EmailOtpExpiresAt = now.AddMinutes(5);  // 例如 5 分鐘有效
                state.EmailOtpVerifyFailCount = 0;

                // 寄信給 user.UserEmail
                await SendTwoFactorEmailCodeAsync(user, otp);
            }
            */

            HttpContext.Session.SetObject(AppSettings.TwoFactorSessionKey, state);
        }

        /// <summary>
        /// 簡單的 SHA256 雜湊（用來儲存 OTP，不放明碼）
        /// </summary>
        [NonAction]
        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        /// <summary>
        /// 簡單的雜湊比對（避免大小寫問題）
        /// </summary>
        [NonAction]
        private static bool SecureEqualsHash(string? leftHash, string? rightHash)
        {
            if (string.IsNullOrEmpty(leftHash) || string.IsNullOrEmpty(rightHash))
                return false;

            var a = leftHash.ToUpperInvariant();
            var b = rightHash.ToUpperInvariant();

            if (a.Length != b.Length)
                return false;

            var result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }

        /// <summary>
        /// 驗證 TOTP 驗證碼（Google Authenticator 類）
        /// secretBase32：Base32 編碼的密鑰
        /// code：使用者輸入的 6 碼數字
        /// </summary>
        [NonAction]
        private static bool VerifyTotpCode(string secretBase32, string code, int timestepSeconds = 30, int allowedDriftSteps = 1)
        {
            if (string.IsNullOrWhiteSpace(secretBase32) || string.IsNullOrWhiteSpace(code))
                return false;

            code = code.Trim().Replace(" ", "");
            if (code.Length != 6 || !code.All(char.IsDigit))
                return false;

            var secret = Base32Decode(secretBase32);
            if (secret == null || secret.Length == 0)
                return false;

            var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var counter = unixTime / timestepSeconds;

            for (long offset = -allowedDriftSteps; offset <= allowedDriftSteps; offset++)
            {
                var totp = ComputeHotp(secret, (ulong)(counter + offset));
                if (totp == code)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// HOTP：HMAC-based One-Time Password
        /// </summary>
        [NonAction]
        private static string ComputeHotp(byte[] key, ulong counter, int digits = 6)
        {
            var counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(counterBytes);

            int offset = hash[hash.Length - 1] & 0x0F;
            int binaryCode =
                ((hash[offset] & 0x7f) << 24) |
                ((hash[offset + 1] & 0xff) << 16) |
                ((hash[offset + 2] & 0xff) << 8) |
                (hash[offset + 3] & 0xff);

            int otp = binaryCode % (int)Math.Pow(10, digits);
            return otp.ToString(new string('0', digits));
        }

        /// <summary>
        /// Base32 解碼（RFC4648，不含 padding）
        /// </summary>
        [NonAction]
        private static byte[] Base32Decode(string base32)
        {
            if (string.IsNullOrWhiteSpace(base32))
                return Array.Empty<byte>();

            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var input = base32.Trim().Replace(" ", "").TrimEnd('=').ToUpperInvariant();

            var bits = 0;
            var value = 0;
            var output = new List<byte>();

            foreach (var c in input)
            {
                int idx = alphabet.IndexOf(c);
                if (idx < 0)
                {
                    continue; // 忽略不在字表內的字元
                }

                value = (value << 5) | idx;
                bits += 5;

                if (bits >= 8)
                {
                    bits -= 8;
                    output.Add((byte)((value >> bits) & 0xFF));
                }
            }

            return output.ToArray();
        }

        /// <summary>
        /// 設定「登入後必須先變更密碼」的 Session 旗標（全域檢查用）
        /// </summary>
        [NonAction]
        private void SetForceChangePasswordSession(
            bool mustChangePassword,
            bool requireFirstLoginChange,
            bool requireExpireChange)
        {
            if (!mustChangePassword)
            {
                HttpContext.Session.Remove(AppSettings.ForceChangePasswordRequiredKey);
                HttpContext.Session.Remove(AppSettings.ForceChangePasswordReasonFirstKey);
                HttpContext.Session.Remove(AppSettings.ForceChangePasswordReasonExpireKey);
                return;
            }

            HttpContext.Session.SetString(AppSettings.ForceChangePasswordRequiredKey, "Y");
            HttpContext.Session.SetString(AppSettings.ForceChangePasswordReasonFirstKey, requireFirstLoginChange ? "Y" : "N");
            HttpContext.Session.SetString(AppSettings.ForceChangePasswordReasonExpireKey, requireExpireChange ? "Y" : "N");
        }

        /// <summary>
        /// 從 Parameter 讀取安全 / 密碼政策設定
        /// </summary>
        /// <returns>密碼政策設定</returns>
        [NonAction]
        private PasswordPolicy LoadSecuritySettingsFromParameter()
        {
            var sec = new PasswordPolicy();

            // 是否啟用密碼政策
            sec.PolicyEnabled = _param.GetBool("SEC_PASSWORD_POLICY_ENABLED");

            // 預設值（當 policyEnabled = false 或參數沒設好時的防呆）
            // 這裡是「程式內建 fallback」，已經不再使用 AppSettings。
            sec.FailedLimit = int.MaxValue;  // 預設視為「不鎖定」
            sec.LockMinutes = 0;             // 鎖定時間 0 = 不鎖定
            sec.PasswordExpireDays = null;   // 密碼過期天數（>0 才啟用）
            sec.ForceChangeFirstLoginFlag = false;

            sec.Sec2faEnabled = false;
            sec.Sec2faEmailEnabled = false;
            sec.Sec2faTotpEnabled = false;

            if (sec.PolicyEnabled)
            {
                // ===== 登入失敗鎖定門檻 / 鎖定時間 =====
                var paramFailedLimit = _param.GetInt("SEC_PASSWORD_MAX_FAILED_ATTEMPTS");
                var paramLockMinutes = _param.GetInt("SEC_PASSWORD_LOCKOUT_MINUTES");

                if (paramFailedLimit > 0 && paramLockMinutes > 0)
                {
                    sec.FailedLimit = paramFailedLimit.Value;
                    sec.LockMinutes = paramLockMinutes.Value;
                }
                else
                {
                    // 若未正確設定，就維持「不鎖定」行為
                    sec.FailedLimit = int.MaxValue;
                    sec.LockMinutes = 0;
                }

                // ===== 密碼過期 / 首次登入強制改密碼 =====
                var expDays = _param.GetInt("SEC_PASSWORD_EXPIRE_DAYS"); // 0=不過期
                if (expDays > 0)
                {
                    sec.PasswordExpireDays = expDays;
                }

                sec.ForceChangeFirstLoginFlag = _param.GetBool("SEC_PASSWORD_FORCE_CHANGE_FIRST_LOGIN");

                // ===== 2FA 設定 =====
                sec.Sec2faEnabled = false;// _param.GetBool("SEC_2FA_ENABLED");
                sec.Sec2faEmailEnabled = false; //                 _param.GetBool("SEC_2FA_EMAIL_ENABLED");
                sec.Sec2faTotpEnabled = false;// _param.GetBool("SEC_2FA_TOTP_ENABLED");
            }

            return sec;
        }

        [NonAction]
        private string GenerateRandomCode(int length)
        {
            //字母數字 已排除常混淆的
            const string Letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string Digits = "2346789";
            if (length < 2)
                throw new ArgumentException("長度必須至少為 2，才能包含至少一個英文與一個數字");

            string Pool = Letters + Digits;

            char[] code = new char[length];
            bool hasLetter = false;
            bool hasDigit = false;

            // 亂數產生器
            Random Rng = new Random();

            // 先填入亂數字元
            for (int i = 0; i < length; i++)
            {
                char c = Pool[Rng.Next(Pool.Length)];
                code[i] = c;
                if (Letters.Contains(c)) hasLetter = true;
                if (Digits.Contains(c)) hasDigit = true;
            }

            // 強制保證至少有一個字母與一個數字
            if (!hasLetter)
            {
                int replaceIndex = Rng.Next(length);
                code[replaceIndex] = Letters[Rng.Next(Letters.Length)];
            }
            if (!hasDigit)
            {
                int replaceIndex = Rng.Next(length);
                code[replaceIndex] = Digits[Rng.Next(Digits.Length)];
            }

            return new string(code);

        }

        [NonAction]
        private byte[] GenerateCaptchaImage(string code)
        {
            int width = 120;
            int height = 40;
            var bmp = new Bitmap(width, height);
            var graphics = Graphics.FromImage(bmp);
            var font = new Font("Consolas", 20, FontStyle.Bold);
            var brush = new SolidBrush(Color.Black);
            var pen = new Pen(Color.LightGray);

            graphics.Clear(Color.White);

            // Draw noise lines
            var rand = new Random();
            for (int i = 0; i < 5; i++)
            {
                graphics.DrawLine(pen,
                    new Point(rand.Next(width), rand.Next(height)),
                    new Point(rand.Next(width), rand.Next(height)));
            }

            // Draw captcha text
            graphics.DrawString(code, font, brush, new PointF(10, 5));

            // Add random dots as additional noise
            for (int i = 0; i < 100; i++)
            {
                int x = rand.Next(width);
                int y = rand.Next(height);
                bmp.SetPixel(x, y, System.Drawing.Color.LightGray);
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }




    }
}
