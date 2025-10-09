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

            var messages = context.Bulletins.Where(s => s.Name == "登入公告" && s.Value.Length > 0).ToList();

            TempData["Messages"] = messages.Any() ? messages.FirstOrDefault()?.Value.ToString() : new List<Bulletin>();

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
                user.Password = HashPassword(user, "Abcd" + user.UserName);
            }

            await context.SaveChangesAsync();
#endif


            return RedirectToAction(nameof(Index));
        }


        /// <summary>
        /// 登入功能
        /// </summary>
        /// <param name="username">帳號</param>
        /// <param name="password">密碼</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? captcha, string? returnUrl)
        {
#if RELEASE
                //只有佈署出去要卡驗證碼
                var storedCaptcha = httpAccessor.HttpContext.Session.GetString("CaptchaCode");

                if (string.IsNullOrEmpty(captcha) || !string.Equals(captcha, storedCaptcha, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["_JSShowAlert"] = "驗證碼錯誤，請重新輸入。";
                    return RedirectToAction(nameof(Index));
                }
#endif

            //記住前一次的 login ID
            httpAccessor.HttpContext.Session.SetString("try_login", username);


            // 取得使用者資料
            var user = context.Users.
                    FirstOrDefault(u => u.UserName == username && u.IsActive);

            // 使用者不存在
            if (user == null)
            {
                TempData["_JSShowAlert"] = "帳號或密碼錯誤!(若忘記密碼，請洽管理者重設密碼)";// 錯誤訊息不可以寫「使用者不存在」
                return RedirectToAction(nameof(Index));
            }

            var hasher = new PasswordHasher<User>();
            var result = PasswordVerificationResult.Failed;
            //DB 密碼尚未加密但通過明碼驗證
            if (!PasswordUtil.AlreadyHashed(user.Password) && password == user.Password)
            {
                //登入成功, 逐次單筆升級加密 (小P本機)
                user.Password = PasswordUtil.Hash(user.Password);
                await context.SaveChangesAsync();

                result = PasswordVerificationResult.Success;
            }
            else
            {
                //雜湊登入
                result = hasher.VerifyHashedPassword(user, hashedPassword: user.Password, providedPassword: password);
            }

            // 密碼錯誤
            if (result != PasswordVerificationResult.Success)
            {
                TempData["_JSShowAlert"] = "帳號或密碼錯誤!(若忘記密碼，請洽管理者重設密碼)";// 錯誤訊息不可以寫「密碼錯誤」
                return RedirectToAction(nameof(Index));
            }



            // 密碼正確，開始建立Claims

            // 查使用者擁有的角色與系統群組
            var userRoles = context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .Select(ur => new { ur.Role.RoleName, ur.Role.RoleGroup })
                .ToList();

            // 拆分角色名稱與系統群組
            var roleNames = userRoles.Select(r => r.RoleName).ToList();
            var systems = userRoles.Select(r => r.RoleGroup).Distinct().ToList(); //導覽選單

            // 建立Claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new("FullName", user.FullName),
                new("UserId", user.Id.ToString())
            };

            foreach (var role in roleNames)
            {
                claims.Add(new(ClaimTypes.Role, role)); // 加入角色
            }

            foreach (var group in systems)
            {
                claims.Add(new("RoleGroup", group)); // 加入所屬系統
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // 如果有returnUrl，就轉跳網址
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("Login"))
            {
                var controller = returnUrl.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
                return Redirect($"/{controller}"); //controller only
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
            var model = new ChangePasswordModel
            {
                UserName = User.Identity?.Name ?? "",
                FullName = User.FindFirst("FullName")?.Value ?? ""
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
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 找出使用者id
            var userId = User.FindFirst("UserId")?.Value;
            var uid = int.Parse(userId);

            // 找出使用者實體
            var user = await context.Users.FindAsync(uid);

            // 確認原密碼正確
            var result = VerifyHashedPassword(user, user.Password, model.CurrentPassword);

            // 原密碼錯誤
            if (result != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("CurrentPassword", "原密碼錯誤");
                return View(model);
            }

            // 將新密碼寫入資料庫
            user.Password = HashPassword(user, model.NewPassword);

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

