using BioMedDocManager.Helpers;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Claims;
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
    public class LoginController(IHttpContextAccessor httpAccessor, DocControlContext context, IWebHostEnvironment hostingEnvironment) : BaseController(context, hostingEnvironment)
    {
        /// <summary>
        /// 登入畫面
        /// </summary>
        /// <param name="returnUrl">導回原始頁面的URL</param>
        /// <returns></returns>
        [Route("/Login")]
        public IActionResult Index(string? returnUrl)
        {
            //密碼錯誤退回來時, 記得剛才的帳號
            TempData["lastUserId"] = httpAccessor.HttpContext.Session.GetString("try_login");

            if (returnUrl != null && returnUrl.Contains("Login"))
            {
                //return URL 防止回到登入頁面
                returnUrl = "";
            }

            // 將 returnUrl 存入 ViewData 或 ViewBag 或 TempData
            ViewBag.ReturnUrl = returnUrl;

            //var messages = context.Bulletins.Where(s => s.Name == "登入公告" && s.Value.Length > 0).ToList();

            TempData["Messages"] = "登入公告文字";// messages.Any() ? messages.FirstOrDefault()?.Value.ToString() : new List<Bulletin>();

            return View();
        }

        /// <summary>
        /// 初次遷移用：將所有明碼密碼轉換成hash密碼
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Migrate()
        {

#if DEBUG
            //只有DEBUG模式才可以使用
            var users = await context.Users
                .ToListAsync();

            foreach (var user in users)
            {
                user.UserPasswordHash = HashPassword(user, "Abcd" + user.UserAccount);
            }

            await context.SaveChangesAsync();
#endif


            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 登入功能
        /// </summary>
        /// <param name="userAccount">帳號</param>
        /// <param name="password">密碼</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string userAccount, string password, string? captcha, string? returnUrl)
        {
            try
            {

#if RELEASE
    var storedCaptcha = httpAccessor.HttpContext.Session.GetString("CaptchaCode");
    if (string.IsNullOrEmpty(captcha) || !string.Equals(captcha, storedCaptcha, StringComparison.OrdinalIgnoreCase))
    {
        TempData["_JSShowAlert"] = "驗證碼錯誤，請重新輸入。";
        return RedirectToAction(nameof(Index));
    }
#endif

                httpAccessor.HttpContext.Session.SetString("try_login", userAccount);

                // 取得使用者
                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.UserAccount == userAccount);

                // 不存在或未啟用（訊息仍給通用字樣）
                if (user is null || !user.UserIsActive)
                {
                    TempData["_JSShowAlert"] = "帳號或密碼錯誤!(若忘記密碼，請洽管理者重設密碼)";
                    return RedirectToAction(nameof(Index));
                }

                // 讀取設定
                var failedLimit = AppSettings.LoginFailedLimit;   // 例如 3 次
                var lockMinutes = AppSettings.LoginLockTime;      // 例如 15（分鐘）
                var now = DateTime.Now;

                // 若已鎖定，檢查是否過期
                if (user.UserIsLocked)
                {
                    if (user.UserLockedUntil.HasValue && user.UserLockedUntil.Value > now)
                    {
                        // 尚在鎖定期
                        TempData["_JSShowAlert"] = $"帳號或密碼錯誤次數達{failedLimit}次以上，請於{lockMinutes}分鐘後重試(或洽管理者解除鎖定)";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        // 鎖定已過期 → 自動解鎖
                        user.UserIsLocked = false;
                        user.UserLockedUntil = null;
                        user.UserLoginFailedCount = 0;
                        await context.SaveChangesAsync();
                    }
                }

                // 驗證密碼
                var hasher = new PasswordHasher<User>();
                var verify = hasher.VerifyHashedPassword(user, user.UserPasswordHash, password);

                if (verify != PasswordVerificationResult.Success)
                {
                    // 失敗：累加計數
                    user.UserLoginFailedCount = (user.UserLoginFailedCount < int.MaxValue)
                        ? user.UserLoginFailedCount + 1
                        : user.UserLoginFailedCount;

                    // 達門檻 → 鎖定一段時間
                    if (user.UserLoginFailedCount >= failedLimit)
                    {
                        user.UserIsLocked = true;
                        user.UserLockedUntil = now.AddMinutes(lockMinutes);

                        TempData["_JSShowAlert"] = $"帳號或密碼錯誤次數達{failedLimit}次以上，請於{lockMinutes}分鐘後重試(或洽管理者解除鎖定)";

                        // 重設計數
                        user.UserLoginFailedCount = 0;
                    }
                    else
                    {
                        // 一律回通用訊息（避免洩漏是帳號還是密碼問題）
                        TempData["_JSShowAlert"] = "帳號或密碼錯誤!(若忘記密碼，請洽管理者重設密碼)";
                    }

                    await context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

                // 成功：清除失敗與鎖定
                user.UserLoginFailedCount = 0;
                user.UserIsLocked = false;
                user.UserLockedUntil = null;
                await context.SaveChangesAsync();

                // 登入稽核（時間+IP）
                await SetUserLoginAuditAsync(user);

                // ===== 1. 透過 UserGroupMember → UserGroupRole → Role 算出有效角色 =====

                // 用導覽屬性的版本（有設定好 navigation 的話建議用這個）
                var rolesFromGroups = await _context.UserGroupMembers
                    .Where(ugm => ugm.UserId == user.UserId)
                    .SelectMany(ugm => ugm.UserGroup!.UserGroupRoles)   // 到 UserGroupRole
                    .Where(ugr => ugr.Role != null)
                    .Select(ugr => ugr.Role!)
                    .Distinct()
                    .ToListAsync();

                var allRoles = rolesFromGroups;   // 單純用群組的話，就這行就好


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
                    claims.Add(new Claim(ClaimTypes.Role, r.RoleName));
                }

                // 角色群組（自訂 ClaimType，方便你之後用來分群或顯示）
                foreach (var g in allRoles
                                     .Select(x => x.RoleGroup)
                                     .Where(x => !string.IsNullOrEmpty(x))
                                     .Distinct())
                {
                    claims.Add(new Claim("RoleGroup", g));
                }

                // ===== 3. SignIn 寫 Cookie =====
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // ===== 4. Redirect =====
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("Login"))
                {
                    var controller = returnUrl.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
                    return Redirect($"/{controller}");
                }

                // 建立 Claims
                /*
                var userRoles = await context.UserRoles
                    .Where(ur => ur.UserId == user.UserId)
                    .Include(ur => ur.Role)
                    .Select(ur => new { ur.Role.RoleName, ur.Role.RoleGroup })
                    .ToListAsync();
                */


                /*
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new("UserFullName", user.UserFullName),
                    new("UserAccount",user.UserAccount)
                };*/


                /*
                foreach (var r in userRoles.Select(x => x.RoleName))
                {
                    claims.Add(new(ClaimTypes.Role, r));
                }

                foreach (var g in userRoles.Select(x => x.RoleGroup).Distinct())
                {
                    claims.Add(new("RoleGroup", g));
                }
                */


                /*
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("Login"))
                {
                    var controller = returnUrl.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
                    return Redirect($"/{controller}");
                }
                */
            }
            catch (Exception ex)
            {
                Utilities.WriteExceptionIntoLogFile("登入異常", ex, this.HttpContext);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// 變更密碼頁面
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("/ChangePassword")]
        public IActionResult ChangePassword()
        {
            // 產生變更密碼模型
            var model = new ChangePasswordViewModel
            {
                UserAccount = User.Identity?.Name ?? "",
                UserFullName = User.FindFirst("UserFullName")?.Value ?? ""
            };

            return View(model);
        }

        /// <summary>
        /// 變更密碼功能
        /// </summary>
        /// <param name="model">資料</param>
        /// <returns></returns>
        [HttpPost]
        [Route("/ChangePassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 找出使用者id
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; ;
            var uid = int.Parse(userId);

            // 找出使用者實體
            var user = await context.Users.FindAsync(uid);

            // 確認原密碼正確
            var result = VerifyHashedPassword(user, user.UserPasswordHash, model.UserCurrentPassword);

            // 原密碼錯誤
            if (result != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("CurrentPassword", "原密碼錯誤");
                return View(model);
            }

            // 將新密碼寫入資料庫
            user.UserPasswordHash = HashPassword(user, model.UserNewPassword);
            user.UserPasswordChangedAt = DateTime.Now;

            await context.SaveChangesAsync();

            //ViewBag.Message = "密碼已成功變更";

            return DismissModal("密碼已成功變更!");

            //return View(model);
        }

        /// <summary>
        /// 登出
        /// </summary>
        /// <returns></returns>
        [Route("/Logout")]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            httpAccessor.HttpContext.Session.SetString("try_login", "");
            TempData["_JSShowAlert"] = "您已登出系統，謝謝您的使用!";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }




        [HttpGet]
        public IActionResult GetCaptcha()
        {
            string code = GenerateRandomCode(5); // e.g., "A3X9B"
            httpAccessor.HttpContext.Session.SetString("CaptchaCode", code);

            byte[] imageBytes = GenerateCaptchaImage(code);
            return File(imageBytes, "image/png");
        }

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

