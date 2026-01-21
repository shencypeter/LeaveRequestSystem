using BioMedDocManager.Models;
using System.Globalization;

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
            var rawPath = context.Request.Path.HasValue ? context.Request.Path.Value! : string.Empty;

            // 去掉前綴 culture（例如 /zh-TW/login -> /login）
            var pathNoCulture = RemoveCulturePrefix(rawPath);

            // 1) 排除不需要檢查的路徑（用「去掉culture後」來判斷）
            if (IsBypassPath(pathNoCulture))
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
                    var culture = CultureInfo.CurrentUICulture.Name; // zh-TW / en-US ...
                    context.Response.Redirect($"/{culture}/AccountSettings/ChangePassword");
                    return;
                }
            }

            await _next(context);
        }

        private static bool IsBypassPath(string path)
        {
            path = (path ?? string.Empty).ToLowerInvariant();

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

        private static string RemoveCulturePrefix(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path[0] != '/')
            {
                return path ?? string.Empty;
            }

            // /{seg}/xxxx 取第一段 seg
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return path;
            }

            var firstSeg = parts[0];

            // 判斷第一段是不是 culture（zh-TW / en / en-US 之類）
            if (IsCultureSegment(firstSeg))
            {
                // 砍掉第一段 culture，重新組回 /xxxx
                if (parts.Length == 1)
                {
                    return "/";
                }
                return "/" + string.Join('/', parts.Skip(1));
            }

            return path;
        }

        private static bool IsCultureSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return false;
            }

            // 用 .NET 的 CultureInfo 來判斷是否為有效文化碼
            // (例如 zh-TW / en / en-US 等)
            try
            {
                _ = CultureInfo.GetCultureInfo(segment);
                return true;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }
    }
}
