using BioMedDocManager.Models;

namespace BioMedDocManager.Middleware
{
    /// <summary>
    /// 強制變更密碼 Middleware：
    /// 只要 Session 中標記需改密碼，就強制導向 ChangePassword，
    /// 除非本身就是在 ChangePassword 或 Logout 等少數頁面。
    /// </summary>
    public class ForceChangePasswordMiddleware
    {
        private readonly RequestDelegate _next;

        public ForceChangePasswordMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.HasValue ? context.Request.Path.Value! : string.Empty;

            // 1) 排除不需要檢查的路徑
            //    - 靜態檔案、登入、登出、2FA、變更密碼頁面…
            if (IsBypassPath(path))
            {
                await _next(context);
                return;
            }

            // 2) 必須是「已登入」的使用者才檢查強制改密碼
            if (context.User.Identity?.IsAuthenticated == true)
            {

                var flag = context.Session.GetString(AppSettings.ForceChangePasswordRequiredKey);

                if (string.Equals(flag, "Y", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect("/AccountSettings/ChangePassword");
                    return;
                }
            }

            await _next(context);
        }

        private static bool IsBypassPath(string path)
        {
            path = path.ToLowerInvariant();

            // 可依實際路由調整
            if (path.StartsWith("/login"))
            {
                return true;
            }

            if (path.StartsWith("/accountsettings/changepassword"))
            {
                return true;
            }

            // 如果 TwoFactor 是在 LoginController 裡：
            if (path.StartsWith("/login/twofactor"))
            {
                return true;
            }

            // 靜態檔案之類的也可以排除（/css, /js, /images ...）
            if (path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/images"))
            {
                return true;
            }

            return false;
        }
    }

}
