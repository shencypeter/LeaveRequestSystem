using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BioMedDocManager
{
    public class Program
    {

        public static async Task Main(string[] args)
        {
            // 產生web建立工具
            var builderWeb = WebApplication.CreateBuilder(args);

            // 移除 Kestrel Server Header
            builderWeb.WebHost.ConfigureKestrel(options =>
            {
                options.AddServerHeader = false;
            });

            // 用iis執行要加這行(部屬到正式環境)
            builderWeb.WebHost.UseIISIntegration();

            builderWeb.Services.AddControllersWithViews();
            builderWeb.Services.AddHttpContextAccessor();
            builderWeb.Services.AddDbContext<DocControlContext>(options =>
                options.UseSqlServer(builderWeb.Configuration.GetConnectionString("DefaultConnection")));
            builderWeb.Services.AddDistributedMemoryCache();
            builderWeb.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20); // 20分鐘
            });

            // 全站Cookie Policy：一律加Secure、設定SameSite與HttpOnly
            builderWeb.Services.Configure<CookiePolicyOptions>(o =>
            {
                // 一律要求 Secure（僅 HTTPS 傳輸）
                o.Secure = CookieSecurePolicy.Always;

                // 嚴格 SameSite（防止 CSRF）
                // - Strict：完全禁止跨站送 Cookie（最安全，但可能影響跨站功能）
                // - Lax：允許部分跨站（例如 GET link），但阻擋 POST/iframe
                // 建議：若系統無跨站需求，使用Strict
                o.MinimumSameSitePolicy = SameSiteMode.Strict;

                // 一律 HttpOnly，防止JS讀取Cookie
                o.HttpOnly = HttpOnlyPolicy.Always;
            });

            // 登入驗證
            builderWeb.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Login";
                options.LogoutPath = "/Login/Logout";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20);// 20分鐘
                options.SlidingExpiration = true;
                options.AccessDeniedPath = "/Error/403"; // 未經授權者，顯示未授權頁面
            });

            // 帳號權限
            builderWeb.Services.AddAuthorization(); // 如果要用 [Authorize] 控制權限

            builderWeb.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(@"C:\DataProtection-Keys"))
            .SetApplicationName("itriDoc");

            // ==============================================
            // 建立web應用程式
            var appWeb = builderWeb.Build();

            // 自訂Headers
            appWeb.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
                // 視環境設定 KnownProxies/KnownNetworks 以避免安全警告
                // KnownProxies = { IPAddress.Parse("127.0.0.1") }
            });

            // 是否為開發環境
            var isDev = appWeb.Environment.IsDevelopment();

            if (!isDev)
            {
                // 正式環境

                // 處理例外
                appWeb.UseExceptionHandler("/Error/500");

                // 處理其他狀態碼錯誤（401, 403, 404）
                appWeb.UseStatusCodePagesWithReExecute("/Error/{0}");

                // 使用HSTS技術
                appWeb.UseHsts();
            }
            else
            {
                // 開發環境：顯示詳細錯誤頁
                appWeb.UseDeveloperExceptionPage();
            }

            // === Clickjacking Protection（全站） ===
            appWeb.Use(async (context, next) =>
            {
                // 1、防止Clickjacking
                context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

                // 2、防止 MIME Sniffing
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";

                // 3、移除其他多餘的 header
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.Remove("Server");
                    context.Response.Headers.Remove("X-Powered-By");
                    return Task.CompletedTask;
                });

                await next();
            });


            // Content Security Policy (CSP) 內容安全策略
            appWeb.Use(async (context, next) =>
            {
                // 1、為本次請求產生nonce，並存到Items給View/Controller用
                var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
                context.Items["CspNonce"] = nonce;

                // 2、依環境組出CSP字串
                var csp = new StringBuilder()
                    .Append("default-src 'self'  ;")
                    .Append($"script-src 'self' 'nonce-{nonce}' blob:  ;")
                    .Append("worker-src 'self' blob:  ;")
                    .Append("child-src 'self' blob:  ;") // child-src 已逐漸被 frame-src 取代，兩者都給
                    .Append("frame-src 'self'  ;") // child-src 已逐漸被 frame-src 取代，兩者都給
                    .Append($"style-src 'self'  ;")//'unsafe-inline''nonce-{nonce}';
                    .Append("img-src 'self' data: blob:  ;")
                    .Append(isDev ? "font-src 'self'  ;" : "font-src 'self' data:  ;")
                    .Append(isDev ? "connect-src 'self' ws: wss: http://localhost:* https://localhost:*  ;"
                                  : "connect-src 'self'  ;")
                    .Append("object-src 'none'  ;")
                    .Append("base-uri 'self'  ;")
                    .Append("frame-ancestors 'self'  ;");

                if (!isDev)
                {
                    // 正式站可加上這兩條更嚴
                    csp.Append("upgrade-insecure-requests;")
                       .Append("block-all-mixed-content;");
                }

                // 3、在回應要送出前設定Header（確保不被後續覆蓋）
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["Content-Security-Policy"] = csp.ToString();
                    return Task.CompletedTask;
                });

                await next();
            });

            // 自動跳轉HTTP到HTTPS
            appWeb.UseHttpsRedirection();

            // 使用靜態檔案
            appWeb.UseStaticFiles();

            // 使用路由
            appWeb.UseRouting();

            // 指定Cookie策略(一定要在UseSession、UseAuthentication前呼叫)
            appWeb.UseCookiePolicy();

            // 使用Session
            appWeb.UseSession();

            // 使用認證(一定要先認證)
            appWeb.UseAuthentication();

            // 使用授權(授權在後)
            appWeb.UseAuthorization();

            // 使用控制器路由
            appWeb.MapControllerRoute(
                name: "default",
                pattern: "{controller=control}/{action=Index}/{id?}");

            // 執行web應用程式
            appWeb.Run();

        }

       
    }
}
