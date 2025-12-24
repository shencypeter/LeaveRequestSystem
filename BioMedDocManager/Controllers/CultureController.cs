using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BioMedDocManager.Controllers
{
    [AllowAnonymous]
    public class CultureController : Controller
    {
        public IActionResult Set([FromQuery]string culture, [FromQuery] string? returnUrl = "/")
        {
            // 僅允許支援語系（第1階段）
            var newCulture = string.Equals(culture, "en-US", StringComparison.OrdinalIgnoreCase)
                ? "en-US"
                : "zh-TW";

            // 安全：只允許站內相對路徑
            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            // returnUrl 只要確保是 "/xxx" 開頭
            if (!returnUrl.StartsWith("/"))
            {
                returnUrl = "/" + returnUrl;
            }

            // 組合：/{newCulture}{returnUrl}
            var redirectUrl = $"/{newCulture}{returnUrl}";

            return LocalRedirect(redirectUrl);
        }
    }
}
