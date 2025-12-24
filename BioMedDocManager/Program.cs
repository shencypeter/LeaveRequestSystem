using BioMedDocManager.Handler;
using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Middleware;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
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
            var builder = WebApplication.CreateBuilder(args);

            // 存取系統設定值
            AppSettings.Initialize(builder.Configuration);

            // (第1階段) 啟用 Localization（就算你暫時還沒用 resx / DB，也先開）
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            // (第1階段) 設定 RequestLocalizationOptions
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("zh-TW"),
                    new CultureInfo("en-US"),
                };

                options.DefaultRequestCulture = new RequestCulture("zh-TW");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                // Culture用Route判斷
                options.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new RouteDataRequestCultureProvider
                    {
                        RouteDataStringKey = "culture",
                        UIRouteDataStringKey = "culture"
                    }
                };
            });

            // 移除 Kestrel Server Header
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AddServerHeader = false;
            });

            // 用iis執行要加這行(部屬到正式環境)
            builder.WebHost.UseIISIntegration();

            // 授權 & 自訂 PolicyProvider/Handler
            builder.Services.AddAuthorization();

            builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            //builder.Services.AddControllersWithViews();
            builder.Services.AddControllersWithViews(options =>
            {
                // 所有沒有 [AllowAnonymous] 的 action，都套用這個授權規則
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement())
                    .Build();

                options.Filters.Add(new AuthorizeFilter(policy));
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<DocControlContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<IAccessLogService, AccessLogService>();// 紀錄連線log
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20); // 20分鐘
            });

            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "RequestVerificationToken";
            });

            // 全站Cookie Policy：一律加Secure、設定SameSite與HttpOnly
            builder.Services.Configure<CookiePolicyOptions>(o =>
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
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Login";
                options.LogoutPath = "/Login/Logout";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20);// 20分鐘
                options.SlidingExpiration = true;
                options.AccessDeniedPath = "/Error/403"; // 未經授權者，顯示未授權頁面
            });


            builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(@"C:\DataProtection-Keys"))
            .SetApplicationName("itriDoc");


            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IParameterService, ParameterHelper>();
            builder.Services.AddScoped<IDbLocalizer, DbLocalizer>();

            builder.Services.AddScoped<IMailHelper, MailHelper>();

            // ==============================================
            // 建立web應用程式
            var app = builder.Build();

            // 在Utilities設定網站運作環境變數
            Utilities.ConfigurePaths(app.Services.GetRequiredService<IWebHostEnvironment>());

            // 自訂Headers
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
                // 視環境設定 KnownProxies/KnownNetworks 以避免安全警告
                // KnownProxies = { IPAddress.Parse("127.0.0.1") }
            });

            // 是否為開發環境
            var isDev = app.Environment.IsDevelopment();

            if (!isDev)
            {
                // 正式環境

                // 處理例外
                app.UseExceptionHandler("/Error/500");

                // 處理其他狀態碼錯誤（401, 403, 404）
                app.UseStatusCodePagesWithReExecute("/Error/{0}");

                // 使用HSTS技術
                app.UseHsts();
            }
            else
            {
                // 開發環境：顯示詳細錯誤頁
                app.UseDeveloperExceptionPage();
            }

            // === Clickjacking Protection（全站） ===
            app.Use(async (context, next) =>
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
            app.Use(async (context, next) =>
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
            app.UseHttpsRedirection();

            // 使用靜態檔案
            app.UseStaticFiles();

            // 使用路由
            app.UseRouting();

            // 語系切換
            var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(locOptions);

            // 指定Cookie策略(一定要在UseSession、UseAuthentication前呼叫)
            app.UseCookiePolicy();

            // 使用Session
            app.UseSession();

            // 使用認證(一定要先認證)
            app.UseAuthentication();

            // 檢查是否強制變更密碼(放在Auth後、Authorization前)
            app.UseMiddleware<ForceChangePasswordMiddleware>();

            // 使用授權(授權在後)
            app.UseAuthorization();

            // 使用控制器路由
            // 先處理多語系
            app.MapControllerRoute(
                name: "localized_root",
                pattern: "{culture}/",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // 特殊路由：CIssueTables Details（含 culture，雙參數）
            // Details: /{culture}/CIssueTables/Details/{docNo}/{docVer}
            app.MapControllerRoute(
                name: "localized_issue_details",
                pattern: "{culture}/CIssueTables/Details/{docNo}/{docVer}",
                defaults: new { controller = "CIssueTables", action = "Details" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // Edit (GET/POST 同一路徑): /{culture}/CIssueTables/Edit/{docNo}/{docVer}
            app.MapControllerRoute(
                name: "localized_issue_edit",
                pattern: "{culture}/CIssueTables/Edit/{docNo}/{docVer}",
                defaults: new { controller = "CIssueTables", action = "Edit" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // Delete (GET): /{culture}/CIssueTables/Delete/{docNo}/{docVer}
            app.MapControllerRoute(
                name: "localized_issue_delete",
                pattern: "{culture}/CIssueTables/Delete/{docNo}/{docVer}",
                defaults: new { controller = "CIssueTables", action = "Delete" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // DeleteConfirmed (POST): /{culture}/CIssueTables/DeleteConfirmed/{docNo}/{docVer}
            app.MapControllerRoute(
                name: "localized_issue_delete_confirmed",
                pattern: "{culture}/CIssueTables/DeleteConfirmed/{docNo}/{docVer}",
                defaults: new { controller = "CIssueTables", action = "DeleteConfirmed" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // History: /{culture}/CIssueTables/History/{docNo}/{docVer}
            app.MapControllerRoute(
                name: "localized_issue_history",
                pattern: "{culture}/CIssueTables/History/{docNo}/{docVer}",
                defaults: new { controller = "CIssueTables", action = "History" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // NewVersion (0 params): /{culture}/CIssueTables/NewVersion
            app.MapControllerRoute(
                name: "localized_issue_newversion_empty",
                pattern: "{culture}/CIssueTables/NewVersion",
                defaults: new { controller = "CIssueTables", action = "NewVersion" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // NewVersion (2 params): /{culture}/CIssueTables/NewVersion/{docNo}/{docVer}
            app.MapControllerRoute(
                name: "localized_issue_newversion_with_keys",
                pattern: "{culture}/CIssueTables/NewVersion/{docNo}/{docVer}",
                defaults: new { controller = "CIssueTables", action = "NewVersion" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // 特殊路由：Error（含 culture，參數）
            app.MapControllerRoute(
                name: "localized_error",
                pattern: "{culture}/Error/{statusCode?}",
                defaults: new { controller = "Error", action = "Index" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // 特殊路由：File（含 culture，參數）
            // GetClaimFile
            app.MapControllerRoute(
                name: "localized_file_getclaim",
                pattern: "{culture}/File/GetClaimFile/{idNo}",
                defaults: new { controller = "File", action = "GetClaimFile" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // GetClaimFileByAdmin
            app.MapControllerRoute(
                name: "localized_file_getclaim_admin",
                pattern: "{culture}/File/GetClaimFileByAdmin/{idNo}",
                defaults: new { controller = "File", action = "GetClaimFileByAdmin" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // 特殊路由：Role（含 culture，參數）
            // EditPermission
            app.MapControllerRoute(
                name: "localized_role_edit_permission",
                pattern: "{culture}/Role/EditPermission/{id}",
                defaults: new { controller = "Role", action = "EditPermission" },
                constraints: new { culture = "zh-TW|en-US" }
            );









            // 特殊路由：Tree（含 culture，參數）
            // GetTreeDataVerLatest
            app.MapControllerRoute(
                name: "localized_tree_get_tree_data_ver_latest",
                pattern: "{culture}/Tree/GetTreeDataVerLatest",
                defaults: new { controller = "Tree", action = "GetTreeDataVerLatestAsync" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // SearchAll
            app.MapControllerRoute(
                name: "localized_tree_search_all",
                pattern: "{culture}/Tree/SearchAll",
                defaults: new { controller = "Tree", action = "SearchAll" },
                constraints: new { culture = "zh-TW|en-US" }
            );

            // GetTreeDataVer
            app.MapControllerRoute(
                name: "localized_tree_get_tree_data_ver",
                pattern: "{culture}/Tree/GetTreeDataVer",
                defaults: new { controller = "Tree", action = "GetTreeDataVer" },
                constraints: new { culture = "zh-TW|en-US" }
            );


            // 一般多語系路由
            app.MapControllerRoute(
                name: "localized",
                pattern: "{culture}/{controller=Home}/{action=Index}/{id?}",
                constraints: new { culture = "zh-TW|en-US" }
            );

            // 一般預設路由（不含 culture）
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );



            // 執行web應用程式
            app.Run();

        }


    }
}
