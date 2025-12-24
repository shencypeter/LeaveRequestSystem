using Aspose.Words;
using BioMedDocManager.Extensions;
using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using ClosedXML.Excel;
using Dapper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;// HeaderUtilities
using OtpNet;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using A = DocumentFormat.OpenXml.Drawing;

namespace BioMedDocManager.Controllers
{

    /// <summary>
    /// 基本控制器，提供共用的功能和屬性給其他控制器使用。
    /// </summary>
    /// <remarks>
    /// 預設建構子
    /// </remarks>
    /// <param name="context">資料物件</param>
    public class BaseController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IParameterService _param, IDbLocalizer _loc) : Controller
    {

        #region 靜態屬性

        /// <summary>
        /// 中文欄位排序比較器
        /// </summary>
        private CompareInfo comparer = CultureInfo.GetCultureInfo("zh-TW").CompareInfo;

        /// <summary>
        /// Hash工具
        /// </summary>
        protected static readonly PasswordHasher<object> _hasher = new();

        /// <summary>
        /// View使用的SessionKey(預設使用)
        /// </summary>
        public virtual string SessionKey =>
            $"{ControllerContext.ActionDescriptor.ControllerName}:QueryModel";

        /// <summary>
        /// 包裝RowNum用
        /// </summary>
        public enum KeyMode { DisplayName, PropertyName }


        #endregion



        #region 方法

        /// <summary>
        /// 在每個Action前的動作
        /// 1、在每個 Action 執行前，將 CSP nonce 存入 ViewBag，以便在視圖中使用。
        /// 2、自動取得Action的Controller，組出View用的SessionKey
        /// 3、取得登入者ID
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 給<script>用的CSP Nonce值
            if (HttpContext.Items.TryGetValue("CspNonce", out var n))
            {
                ViewBag.CspNonce = n as string;
            }

            // 每個Controller的SessionKey
            context.HttpContext.Items["SessionKey"] = SessionKey;

            var menuTree = HttpContext.Session.GetObject<List<MenuItemGroupViewModel>>("MenuTree") ?? new List<MenuItemGroupViewModel>();

            // 已登入但沒有選單的話，重建選單 (因為重新compiler會讓session消失，但是login cookie還在)
            if (User?.Identity?.IsAuthenticated == true && menuTree.Count == 0)
            {
                menuTree = BuildMenuTreeForUser(User);
                HttpContext.Session.SetObject("MenuTree", menuTree);
            }

            // 選單(Login就存入Session中了)

            ViewData["MenuTree"] = menuTree;

            // 使用者資訊
            var user = GetLoginUser();
            if (user != null)
            {
                ViewData["UserFullName"] = user.UserFullName;
            }

            // 目前頁面
            var (controllerLabel, fullTitle) = ResolvePageTitleFromMenuItem(context);
            ViewData["ControllerLabel"] = controllerLabel;   // 目前這頁的標題（ResourceDisplayName）
            ViewData["FullTitle"] = fullTitle;               // 頂端 <title>

            // 問候語
            ViewData["Greeting"] = GreetingByHour(DateTime.Now.Hour);

            // 是否啟用2FA
            ViewData["2FA_ENABLED"] = _param.GetBool("SEC_2FA_ENABLED");

            base.OnActionExecuting(context);
        }

        [NonAction]
        protected List<MenuItemGroupViewModel> BuildMenuTreeForUser(ClaimsPrincipal user)
        {
            // 1) 取得角色名稱（已在 Login 寫入 Claims Cookie）
            var userRoles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            if (!userRoles.Any())
            {
                return new List<MenuItemGroupViewModel>();
            }

            // 2) 取得這些角色允許的 ResourceId
            var allowedResourceIds = _context.RolePermissions
                .Where(rp => userRoles.Contains(rp.Role.RoleName))
                .Select(rp => rp.ResourceId)
                .Distinct()
                .ToList();

            if (!allowedResourceIds.Any())
            {
                return new List<MenuItemGroupViewModel>();
            }

            // 3) 抓出符合 ResourceId 的 MenuItem（可被顯示的子選單們）
            var menuItems = _context.MenuItems
                .Include(m => m.Resource)
                .Where(m =>
                    m.MenuItemIsActive &&
                    m.ResourceId != null &&
                    allowedResourceIds.Contains(m.ResourceId.Value))
                .ToList();

            if (!menuItems.Any())
            {
                return new List<MenuItemGroupViewModel>();
            }

            // 4) 依 MenuItemParentId 分組：lookup 的 key = 父選單Id，value = 這個父底下所有子選單
            var childrenLookup = menuItems
                .Where(m => m.MenuItemParentId != null)
                .ToLookup(m => m.MenuItemParentId!.Value);

            var parentIds = childrenLookup
                .Select(g => g.Key)
                .Distinct()
                .ToList();

            if (!parentIds.Any())
            {
                return new List<MenuItemGroupViewModel>();
            }

            // 5) 把這些父選單抓出來（不需要管它有沒有 ResourceId，只要是啟用即可）
            var parents = _context.MenuItems
                .Include(m => m.Resource)
                .Where(m => m.MenuItemIsActive && parentIds.Contains(m.MenuItemId))
                .ToList();

            if (!parents.Any())
            {
                return new List<MenuItemGroupViewModel>();
            }

            // 6) 排序 + 組成想要的結構
            var result = parents
                .OrderBy(p => p.MenuItemDisplayOrder) // 父與父之間先看顯示順序
                .ThenBy(p => p.MenuItemId)
                .Select(p => new MenuItemGroupViewModel
                {
                    MenuItemParentId = p.MenuItemId,
                    Parent = p,
                    Children = childrenLookup[p.MenuItemId]
                        .OrderBy(c => c.MenuItemDisplayOrder)
                        .ThenBy(c => c.MenuItemId)
                        .ToList()
                })
                .ToList();

            return result;
        }

        /// <summary>
        /// 從目前路由反查 MenuItem，組出頁面標題
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [NonAction]
        protected (string ControllerLabel, string FullTitle) ResolvePageTitleFromMenuItem(ActionExecutingContext ctx)
        {
            var route = ctx.RouteData;
            var controllerKey = route.Values["controller"]?.ToString() ?? string.Empty;

            // 反查：MenuItem → Resource → ResourceKey == controller 名稱
            var menuItem = _context.MenuItems
                .Include(m => m.Resource)
                .AsNoTracking()
                .FirstOrDefault(m => m.Resource != null
                                     && m.Resource.ResourceKey == controllerKey);

            var baseTitle = _loc.T("Home.Welcome.Title");

            if (menuItem == null)
            {
                return (string.Empty, baseTitle);
            }

            var label = menuItem.MenuItemTitle;     // ← 改抓 MenuItem 標題
            var fullTitle = $"{baseTitle}-{label}";

            return (label, fullTitle);
        }

        /// <summary>
        /// 依照時間取得問候語
        /// </summary>
        /// <param name="hour">小時</param>
        /// <returns>問候語文字</returns>
        protected static string GreetingByHour(int hour) =>
            hour < 12 ? "早安" : (hour < 18 ? "午安" : "晚安");

        /// <summary>
        /// 取得登入者實體
        /// </summary>
        /// <returns>登入者ID</returns>
        protected User? GetLoginUser()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; ;

            if (int.TryParse(claimValue, out int userId))
            {
                return _context.Users.FirstOrDefault(u => u.UserId == userId);
            }

            // 找不到或格式不對，回傳 null
            return null;
        }

        /// <summary>
        /// 取得登入者ID
        /// </summary>
        /// <returns>登入者ID</returns>
        protected int? GetLoginUserId()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; ;

            if (int.TryParse(claimValue, out int userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// 寫入資料庫：使用者的最後登入時間及IP
        /// </summary>
        protected async Task SetUserLoginAuditAsync(User user, CancellationToken ct = default)
        {
            // 取 IP
            var ip = Utilities.GetClientIpAddress(HttpContext.Request);

            if (user is null)
            {
                return;
            }

            user.UserLastLoginAt = DateTime.Now;
            user.UserLastLoginIp = ip;

            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// 從 Session 中取得指定型別的查詢 Model
        /// </summary>
        /// <typeparam name="T">class類型</typeparam>
        /// <param name="sessionKey">session名稱</param>
        /// <returns></returns>
        protected T GetSessionQueryModel<T>(string sessionKey) where T : class, new()
        {
            return QueryableExtensions.GetSessionQueryModel<T>(HttpContext, sessionKey);
        }

        /// <summary>
        /// 從 Session 中取得指定型別的查詢 Model
        /// </summary>
        /// <typeparam name="T">class類型</typeparam>
        protected T GetSessionQueryModel<T>() where T : class, new()
        {
            return QueryableExtensions.GetSessionQueryModel<T>(HttpContext);
        }

        protected void FilterOrderBy<T>(T queryModel, Dictionary<string, string> TableHeaders, string InitSort) where T : Pagination
        {
            // 允許清單（大小寫不敏感）
            var allowed = new HashSet<string>(TableHeaders.Keys, StringComparer.OrdinalIgnoreCase);

            // 取使用者要求的 Key
            var key = (queryModel?.OrderBy ?? string.Empty).Trim();

            // 非法或空 → 退回預設欄位 + ASC
            if (string.IsNullOrEmpty(key) || !allowed.Contains(key))
            {
                key = InitSort;
                queryModel.SortDir = "asc";
            }

            // 方向只允許 ASC/DESC；其他一律 ASC
            queryModel.SortDir = string.Equals(queryModel.SortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

            // 特殊欄位：版次字串轉數字排序（NULL 永遠最後）
            // 先不考慮

            // 一般欄位：直接使用白名單中的欄位名
            queryModel.OrderBy = $"{key}";

        }

        /// <summary>
        /// 取得所有角色資料
        /// </summary>
        /// <returns></returns>
        protected async Task<List<Role>> GetRoles()
        {
            // 取得所有角色資料
            return await _context.Roles
                  .OrderBy(r => r.RoleGroup)
                  .ThenBy(r => r.RoleName)
                  .ToListAsync();
        }

        /// <summary>
        /// 將CreateUser轉回User
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected static User ToUserEntity(CreateUserViewModel model)
        {
            return new User
            {
                UserAccount = model.UserAccount,
                UserFullName = model.UserFullName,
                UserPasswordHash = HashPassword(model, model.UserPasswordHash),
                UserIsActive = model.UserIsActive,
                CreatedAt = DateTime.Now,
            };
        }

        /// <summary>
        /// 密碼Hash
        /// </summary>
        /// <param name="model"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        protected static string HashPassword(object model, string Password)
        {
            return _hasher.HashPassword(model, Password);
        }

        /// <summary>
        /// 密碼驗證
        /// </summary>
        /// <param name="model"></param>
        /// <param name="Password1"></param>
        /// <param name="Password2"></param>
        /// <returns></returns>
        protected static PasswordVerificationResult VerifyHashedPassword(object model, string Password1, string Password2)
        {
            return _hasher.VerifyHashedPassword(model, Password1, Password2);
        }

        /// <summary>
        /// 關閉Modal並回傳訊息給父視窗
        /// </summary>
        /// <param name="alertMsg">訊息</param>
        /// <returns></returns>
        protected IActionResult DismissModal(string alertMsg = "")
        {
            string? nonce = HttpContext?.Items["CspNonce"] as string;

            // 安全轉義（避免斷行 / 反斜線 / 雙引號造成 JS 字串壞掉）
            static string JsString(string? s) =>
                (s ?? string.Empty)
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n");

            string safeMsg = JsString(alertMsg);
            string nonceAttr = string.IsNullOrWhiteSpace(nonce) ? "" : $@" nonce=""{nonce}""";
            //string html = $@"<script{nonceAttr}>window.parent.dismiss(""{safeMsg}"");</script>";

            string html = $@"
                <script src=""/js/site.js""{nonceAttr}></script>
                <script{nonceAttr}>
                    window.parent.dismiss(""{safeMsg}"");
                </script>";

            return Content(html, "text/html; charset=utf-8");
        }

        /// <summary>
        /// 包裝RowNum用，依 TableHeaders 將實體列表轉為 List<Dictionary<string, object>>，
        /// 可自動加入 RowNum（#），並可選擇使用「顯示名或屬性名」當輸出鍵。
        /// </summary>
        protected static List<Dictionary<string, object>> BuildRows<T>(
            IEnumerable<T> entities,
            IReadOnlyDictionary<string, string> tableHeaders, // Key=屬性名, Value=顯示文字；含 "RowNum" => "#"
            int pageNumber,
            int pageSize,
            KeyMode keyMode = KeyMode.DisplayName,
            bool includeRowNum = true,
            IEnumerable<string>? payloadProps = null // 額外要放進 row 的隱藏鍵（例如 UserId）
        )
        {
            var list = entities as IList<T> ?? entities.ToList();
            var baseNo = Math.Max(0, (pageNumber - 1)) * Math.Max(0, pageSize);

            var rows = new List<Dictionary<string, object>>(list.Count);
            foreach (var row in list.Select((entity, i) => new { entity, i }))
            {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                // 1) 先處理可見欄位（依 headers 決定顯示與順序）
                foreach (var kv in tableHeaders)
                {
                    var propName = kv.Key;
                    var header = kv.Value;

                    if (propName.Equals("RowNum", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var val = Utilities.GetProp(row.entity!, propName) ?? "";
                    var outKey = keyMode == KeyMode.DisplayName ? header : propName;
                    dict[outKey] = val;
                }

                // 2) RowNum
                if (includeRowNum && tableHeaders.TryGetValue("RowNum", out var rowNumDisplay))
                {
                    var outKey = keyMode == KeyMode.DisplayName ? rowNumDisplay : "RowNum";
                    dict[outKey] = baseNo + row.i + 1;
                }

                // 3) ★ 額外塞 payload（不影響可見欄位與順序；用屬性名當 key）
                if (payloadProps != null)
                {
                    foreach (var p in payloadProps)
                    {
                        // 用屬性名存，避免被 DisplayName 模式吃掉
                        if (!string.IsNullOrWhiteSpace(p) && !dict.ContainsKey(p))
                        {
                            dict[p] = Utilities.GetProp(row.entity!, p) ?? "";
                        }
                    }
                }

                rows.Add(dict);
            }
            return rows;
        }

        /// <summary>
        /// 一次抓所有密碼政策參數
        /// </summary>
        /// <returns></returns>
        [NonAction]
        protected PasswordPolicy GetPasswordPolicy()
        {
            // 這裡做「合理預設值」，避免 DB 沒設參數時出事
            var enabled = _param.GetBool("SEC_PASSWORD_POLICY_ENABLED");

            int? minLen = _param.GetInt("SEC_PASSWORD_MIN_LENGTH");
            if (minLen <= 0)
            {
                minLen = 8;
            }

            var sets = _param.GetString("SEC_PASSWORD_SPECIAL_CHAR_SETS");
            if (string.IsNullOrWhiteSpace(sets))
            {
                sets = "!@#$%&*()_+-=.`";
            }

            var hist = _param.GetInt("SEC_PASSWORD_HISTORY_COUNT");
            if (hist < 0)
            {
                hist = 0;
            }

            var minAge = _param.GetInt("SEC_PASSWORD_MIN_AGE_DAYS");
            if (minAge < 0)
            {
                minAge = 0;
            }

            return new PasswordPolicy
            {
                PolicyEnabled = enabled,
                MinLength = minLen.Value,
                RequireUpper = _param.GetBool("SEC_PASSWORD_REQUIRE_UPPER"),
                RequireLower = _param.GetBool("SEC_PASSWORD_REQUIRE_LOWER"),
                RequireDigit = _param.GetBool("SEC_PASSWORD_REQUIRE_DIGIT"),
                RequireSpecial = _param.GetBool("SEC_PASSWORD_REQUIRE_SPECIAL"),
                SpecialCharSets = sets,
                HistoryCount = hist.Value,
                MinAgeDays = minAge.Value
            };
        }

        [NonAction]
        protected void SetPasswordPolicyToViewBag()
        {
            var enabled = _param.GetBool("SEC_PASSWORD_POLICY_ENABLED");

            var minLength = _param.GetInt("SEC_PASSWORD_MIN_LENGTH");
            if (minLength <= 0)
            {
                minLength = 8; // 防呆
            }

            var requireUpper = _param.GetBool("SEC_PASSWORD_REQUIRE_UPPER");
            var requireLower = _param.GetBool("SEC_PASSWORD_REQUIRE_LOWER");
            var requireDigit = _param.GetBool("SEC_PASSWORD_REQUIRE_DIGIT");
            var requireSpecial = _param.GetBool("SEC_PASSWORD_REQUIRE_SPECIAL");
            var specialCharSets = _param.GetString("SEC_PASSWORD_SPECIAL_CHAR_SETS") ?? string.Empty;

            ViewBag.PasswordPolicy = new
            {
                Enabled = enabled,
                MinLength = minLength,
                RequireUpper = requireUpper,
                RequireLower = requireLower,
                RequireDigit = requireDigit,
                RequireSpecial = requireSpecial,
                SpecialCharSets = specialCharSets
            };
        }


        /// <summary>
        /// 新密碼檢核
        /// </summary>
        /// <param name="modelFieldName">欄位名稱</param>
        /// <param name="newPassword">新密碼</param>
        /// <param name="policy">密碼政策內容</param>
        [NonAction]
        protected void ValidateNewPasswordByPolicy(
            string modelFieldName,
            string? newPassword,
            PasswordPolicy policy
        )
        {
            // 未啟用政策就不檢核
            if (!policy.PolicyEnabled)
            {
                return;
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                // 這個通常由 Required 處理，但保險起見
                ModelState.AddModelError(modelFieldName, "新密碼不可為空。");
                return;
            }

            if (newPassword.Length < policy.MinLength)
            {
                ModelState.AddModelError(modelFieldName, $"新密碼長度至少需 {policy.MinLength} 碼。");
            }

            if (policy.RequireUpper && !newPassword.Any(char.IsUpper))
            {
                ModelState.AddModelError(modelFieldName, "新密碼需包含至少 1 個大寫字母。");
            }

            if (policy.RequireLower && !newPassword.Any(char.IsLower))
            {
                ModelState.AddModelError(modelFieldName, "新密碼需包含至少 1 個小寫字母。");
            }

            if (policy.RequireDigit && !newPassword.Any(char.IsDigit))
            {
                ModelState.AddModelError(modelFieldName, "新密碼需包含至少 1 個數字。");
            }

            if (policy.RequireSpecial)
            {
                // 特殊符號：必須存在於允許集合內
                var allowed = policy.SpecialCharSets ?? "";
                bool hasAllowedSpecial = newPassword.Any(ch => allowed.Contains(ch));
                if (!hasAllowedSpecial)
                {
                    ModelState.AddModelError(modelFieldName, $"新密碼需包含至少 1 個特殊符號（允許符號：{allowed}）。");
                }
            }
        }

        /// <summary>
        /// 驗證 TOTP 驗證碼（與 Google Authenticator 相容）
        /// </summary>
        /// <remarks>
        /// 前提：
        /// 1. 使用者的 TOTP Secret 以 Base32 格式儲存在 user.TotpSecret（你可以換成自己的欄位名稱）。
        /// 2. Google Authenticator 預設使用：6 碼、30 秒一個 time step、SHA1。
        /// 3. 這裡允許前後各 1 個 time step 的誤差（約 ±30 秒）。
        /// </remarks>
        [NonAction]
        protected Task<bool> VerifyTotpCodeAsync(User user, string code)
        {
            // 基本輸入檢查
            if (user == null)
            {
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(user.UserTotpSecret))
            {
                // 沒有設定 TOTP 密鑰 → 無法驗證
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return Task.FromResult(false);
            }

            var trimmedCode = code.Trim();

            // 僅接受 6 碼數字（Google Authenticator 預設）
            if (trimmedCode.Length != 6 || !trimmedCode.All(char.IsDigit))
            {
                return Task.FromResult(false);
            }

            try
            {
                // user.TotpSecret 假設是 Base32 字串（例如 "JBSWY3DPEHPK3PXP" 這種）
                var secretBase32 = user.UserTotpSecret.Trim();

                // 轉成 bytes（Otp.NET 有 Base32Encoding）
                byte[] secretBytes = Base32Encoding.ToBytes(secretBase32);

                // 建立 TOTP 物件：6 碼、30 秒、SHA1 → 與 Google Authenticator 相同設定
                var totp = new Totp(
                    secretBytes,
                    step: 30,                    // 30 秒一格
                    mode: OtpHashMode.Sha1,      // Google Auth 預設 SHA1
                    totpSize: 6                  // 6 碼
                );

                // 驗證時允許前後各 1 個 time window（避免裝置時間差）
                long timeStepMatched;
                var isValid = totp.VerifyTotp(
                    trimmedCode,
                    out timeStepMatched,
                    new VerificationWindow(previous: 1, future: 1)
                );

                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                // 若 Secret 格式壞掉或其他錯誤，直接視為驗證失敗即可
                Utilities.WriteExceptionIntoLogFile("VerifyTotpCodeAsync 錯誤", ex, this.HttpContext);
                return Task.FromResult(false);
            }
        }

























































        /// <summary>
        /// 取得表單儲存路徑
        /// </summary>
        /// <returns>表單儲存路徑</returns>
        protected string GetFormPath()
        {

            // 取得表單儲存路徑
            var form_path = "D:/form";// 範例
            return form_path;
        }

        /// <summary>
        /// 刪除表單檔案（實際為重新命名前加上 DEL_）
        /// </summary>
        /// <param name="model">IssueTable 物件，用於抓取檔名</param>
        protected void RenameDeleteFormFile(IssueTable model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.OriginalDocNo) || string.IsNullOrWhiteSpace(model.DocVer) || string.IsNullOrWhiteSpace(model.FileExtension))
                return;

            try
            {
                // 組成檔案名稱與路徑
                var savePath = GetFormPath();

                // 原始檔名 (不帶副檔名)
                var baseFileName = $"{model.OriginalDocNo}(V{model.DocVer})";
                var fullPath = Path.Combine(savePath, baseFileName + "." + model.FileExtension);

                if (System.IO.File.Exists(fullPath))
                {
                    // 新檔案名稱 (DEL_前綴 + 原檔名 + 刪除時間 + 副檔名)
                    var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var newFileName = $"DEL_{baseFileName}_{timeStamp}.{model.FileExtension}";
                    var newFullPath = Path.Combine(savePath, newFileName);

                    System.IO.File.Move(fullPath, newFullPath);
                    Console.WriteLine("刪除表單檔案，檔案已重新命名：" + newFullPath);
                }

            }
            catch (Exception ex)
            {
                // 可以記錄 log
                Console.WriteLine("刪除表單檔案，檔案重新命名失敗：" + ex.Message);
            }
        }

        /// <summary>
        /// 檢查檔名副檔名是否合法(大小寫不敏感)
        /// </summary>
        protected static bool IsValidFileExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AppSettings.AllowedExtensions.Contains(extension);
        }

        /// <summary>
        /// 文件領用：預查 領用後會打在文件上面的流水號 (非保留號)
        /// </summary>
        /// <param name="docNoPrefix">表單前綴詞</param>
        /// <returns></returns>
        protected string NonReserveDocNos(string docNoPrefix)
        {
            //加上安全判斷
            if (String.IsNullOrEmpty(docNoPrefix))
            {
                return "ERROR";
            }

            var nonReservedSuffixes = _context.DocControlMaintables
                                            .Where(d => d.IdNo.StartsWith(docNoPrefix))
                                            .Select(d => d.IdNo.Substring(docNoPrefix.Length))
                                            .Where(s => !s.EndsWith("0")) // Exclude '000', '010', etc.
                                            .Select(int.Parse)
                                            .ToList();

            int nextSuffix = nonReservedSuffixes.Any() ? nonReservedSuffixes.Max() + 1 : 1;

            // 10的倍數編號保留下來給保留號使用，所以跳過(自動加1的意思)
            while (nextSuffix % 10 == 0)
            {
                nextSuffix++;
            }

            // 組合成編號(補足3位數)
            var nextDocNo = $"{docNoPrefix}{nextSuffix:D3}";

            return nextDocNo;
        }

        /// <summary>
        /// 保留號文件領用：預查下一組保留文件號 (XXX 010)
        /// </summary>
        /// <param name="docNoPrefix">表單前綴詞</param>
        /// <returns></returns>
        protected string ReserveDocNos(string docNoPrefix)
        {
            // B or E + yyyyMM 尾號為0
            var existingDocNos = _context.DocControlMaintables
                                         .Where(d => d.IdNo.StartsWith(docNoPrefix) && d.IdNo.EndsWith("0"))
                                         .Select(d => d.IdNo)
                                         .ToList();

            // 擷取尾號並轉為數字
            var suffixes = existingDocNos
                .Select(dn => dn.Substring(docNoPrefix.Length))
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToList();

            // 取最大尾號，沒有則預設為0
            int maxSuffix = suffixes.Any() ? suffixes.Max() : 0;

            // 下一個尾號：必須為10的倍數（且最小為10）
            int nextSuffix = Math.Max(10, maxSuffix + 10);

            // 補足3位數格式
            string newSuffix = nextSuffix.ToString("D3");

            // 回傳組合後的文件編號
            return $"{docNoPrefix}{newSuffix}";
        }

        /// <summary>
        /// 產生匯出的Excel檔
        /// </summary>
        /// <param name="docNumber">文件編號</param>
        /// <param name="originalDocNo">表單編號</param>
        /// <param name="docVer">表單版次</param>
        /// <returns>合成文件編號後的Excel檔</returns>
        protected byte[] GenerateExcelDocument(DocControlMaintable model)
        {
            string FormPath = "";// GetFormPath();// 取得使用者指定的檔案儲存路徑
            string sourcefilePath_REAL = Path.Combine(FormPath, model.RealFormFileName);//到時候真實檔案名稱
            string sourcefilePath_default = Path.Combine(_hostingEnvironment.WebRootPath, "docs", "範例Excel.xlsx");

            // 判斷檔案是否存在，不存在就使用範例
            string finalSourcePath = System.IO.File.Exists(sourcefilePath_REAL)
                ? sourcefilePath_REAL
                : sourcefilePath_default;

            Aspose.Cells.Workbook workbook = new Aspose.Cells.Workbook(finalSourcePath);
            Aspose.Cells.Worksheet worksheet = workbook.Worksheets["2020"];
            Aspose.Cells.PageSetup pageSetup = worksheet.PageSetup;

            // Replace header placeholders
            for (int i = 0; i < 3; i++)
            {
                string header = pageSetup.GetHeader(i);
                if (!string.IsNullOrEmpty(header) && header.Contains("BYYYYMMNNN"))
                {
                    pageSetup.SetHeader(i, header.Replace("BYYYYMMNNN", model.IdNo));
                }
            }

            using var stream = new MemoryStream();
            workbook.Save(stream, Aspose.Cells.SaveFormat.Xlsx);
            return stream.ToArray();
        }

        /// <summary>
        /// 產生匯出的Word檔
        /// <param name="docNumber">文件編號</param>
        /// <param name="originalDocNo">表單編號</param>
        /// <param name="docVer">表單版次</param>
        /// <returns>合成文件編號後的Word檔</returns>
        protected byte[] GenerateWordDocument(DocControlMaintable model)
        {
            string FormPath = "";// GetFormPath();// 取得使用者指定的檔案儲存路徑
            string sourcefilePath_REAL = Path.Combine(FormPath, model.RealFormFileName);//到時候真實檔案名稱
            string sourcefilePath_default = Path.Combine(_hostingEnvironment.WebRootPath, "docs", "範例Word.docx");

            // 判斷檔案是否存在，不存在就使用範例
            string finalSourcePath = System.IO.File.Exists(sourcefilePath_REAL)
                ? sourcefilePath_REAL
                : sourcefilePath_default;

            // 載入文件
            Aspose.Words.Document doc = new Aspose.Words.Document(finalSourcePath);
            DocumentBuilder builder = new DocumentBuilder(doc);

            // 先取得 HeaderPrimary 內容
            Aspose.Words.HeaderFooter header = doc.FirstSection.HeadersFooters[Aspose.Words.HeaderFooterType.HeaderPrimary];
            string headerText = header?.GetText() ?? "";

            // 移除所有連續的 \r（包含單個）
            headerText = Regex.Replace(headerText, @"\r+", "").Trim();

            // 判斷是否包含佔位字串
            if (headerText.Contains("BYYYYMMNNN"))
            {
                headerText = headerText.Replace("BYYYYMMNNN", model.IdNo);
            }
            else
            {
                headerText = string.IsNullOrEmpty(headerText) ? model.IdNo : headerText;
            }

            // 清空並寫回
            header.RemoveAllChildren();
            builder.MoveToHeaderFooter(Aspose.Words.HeaderFooterType.HeaderPrimary);
            builder.ParagraphFormat.Alignment = ParagraphAlignment.Center;
            builder.ParagraphFormat.LineSpacingRule = LineSpacingRule.AtLeast;
            builder.ParagraphFormat.LineSpacing = 5;
            builder.Font.Name = "Calibri";
            builder.Font.Size = 16;
            builder.Font.Bold = true;
            builder.Write(headerText);

            using var stream = new MemoryStream();
            doc.Save(stream, Aspose.Words.SaveFormat.Docx);
            return stream.ToArray();
        }

        /// <summary>
        /// 產生匯出的PPT檔（使用 Open XML SDK）
        /// </summary>
        protected byte[] GeneratePowerPointDocument(DocControlMaintable model)
        {
            string formPath = "";// GetFormPath(); // 取得使用者指定的檔案儲存路徑
            string sourcefilePath_REAL = Path.Combine(formPath, model.RealFormFileName); // 真實檔案名稱
            string sourcefilePath_default = Path.Combine(_hostingEnvironment.WebRootPath, "docs", "範例PPT.pptx");

            // 判斷檔案是否存在，不存在就使用範例
            string finalSourcePath = System.IO.File.Exists(sourcefilePath_REAL)
                ? sourcefilePath_REAL
                : sourcefilePath_default;

            // 為了不直接改到來源檔，先複製到 MemoryStream 再操作
            using var input = System.IO.File.OpenRead(finalSourcePath);
            using var ms = new MemoryStream();
            input.CopyTo(ms);
            ms.Position = 0;

            using (var ppt = PresentationDocument.Open(ms, true))
            {
                var presPart = ppt.PresentationPart!;
                var pres = presPart.Presentation;

                // 投影片 Id 列表
                foreach (var slideId in pres.SlideIdList!.Elements<SlideId>())
                {
                    var slidePart = (SlidePart)presPart.GetPartById(slideId.RelationshipId!);
                    AddTextBoxToSlide(slidePart, model.IdNo, 428, 0, 103, 30);
                }

                ppt.Save();
            }

            return ms.ToArray();
        }

        private static void AddTextBoxToSlide(SlidePart slidePart, string text, int xPx, int yPx, int wPx, int hPx)
        {
            var slide = slidePart.Slide;

            // 形狀樹
            var shapeTree = slide.CommonSlideData!.ShapeTree!;

            // 取得目前已用的最大 shape Id，新的要遞增
            uint maxId = 1;
            foreach (var nv in shapeTree.Descendants<NonVisualDrawingProperties>())
            {
                if (nv.Id != null && nv.Id > maxId) maxId = nv.Id;
            }
            uint newId = maxId + 1;

            // 建立文字方塊（Rectangle + TextBox）
            var shape = new DocumentFormat.OpenXml.Presentation.Shape();

            // 非視覺屬性（Id/Name）
            shape.NonVisualShapeProperties = new NonVisualShapeProperties(
                new NonVisualDrawingProperties() { Id = newId, Name = $"TextBox {newId}" },
                new NonVisualShapeDrawingProperties(new A.ShapeLocks() { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties());

            // 位置與大小（Transform2D）
            var x = Utilities.PxToEmu(xPx);
            var y = Utilities.PxToEmu(yPx);
            var w = Utilities.PxToEmu(wPx);
            var h = Utilities.PxToEmu(hPx);

            shape.ShapeProperties = new ShapeProperties(
                new A.Transform2D(
                    new A.Offset() { X = x, Y = y },
                    new A.Extents() { Cx = w, Cy = h }
                ),
                // 無填滿
                new A.NoFill(),
                // 無邊線
                new A.Outline(new A.NoFill())
            );

            // 文字內容
            var runProps = new A.RunProperties()
            {
                // 字體大小：OpenXML 用 1/100 pt；12pt → 1200
                FontSize = 1200,
                Bold = true
            };
            // 指定拉丁字型（Calibri）
            runProps.Append(new A.LatinFont() { Typeface = "Calibri" });

            // 字色：黑色
            runProps.Append(
                new A.SolidFill(
                    new A.RgbColorModelHex() { Val = "000000" }
                )
            );

            var run = new A.Run(runProps, new A.Text(text ?? string.Empty));

            var para = new A.Paragraph(
                new A.ParagraphProperties() { Alignment = A.TextAlignmentTypeValues.Left },
                run
            );

            shape.TextBody = new TextBody(
                new A.BodyProperties(),          // 預設即可
                new A.ListStyle(),
                para
            );

            shapeTree.Append(shape);
            slide.Save();
        }

        /// <summary>
        /// 取得文件檔案
        /// </summary>
        /// <param name="model">文件物件</param>
        /// <returns>合成文件編號後的檔案</returns>
        protected IActionResult GetDocument(DocControlMaintable model)
        {
            byte[] fileBytes;
            string contentType;
            var asciiFileName = "download"; // ASCII-safe 備援名稱

            if (model.FileExtension == "docx")
            {
                // 產生Word文件
                fileBytes = GenerateWordDocument(model);
                contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            }
            else if (model.FileExtension == "xlsx")
            {
                // 產生Excel文件
                fileBytes = GenerateExcelDocument(model);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }
            else if (model.FileExtension == "pptx")
            {
                // 產生Excel文件
                fileBytes = GeneratePowerPointDocument(model);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }
            else
            {
                // 用範例Word文件
                fileBytes = GenerateWordDocument(model);
                contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            }

            // 轉成UTF-8檔名
            string encodedName = Uri.EscapeDataString(model.RealFileName);   // 轉成UTF-8

            // 撰寫檔名Disposition
            var disposition = $"attachment; filename=\"{asciiFileName}\"; filename*=UTF-8''{encodedName}";

            Response.Headers[HeaderNames.ContentDisposition] = disposition;

            return File(fileBytes, contentType);
        }

        /// <summary>
        /// 轉跳頁面並顯示js訊息
        /// </summary>
        /// <param name="actionPath">轉跳路徑</param>
        /// <param name="msg">訊息</param>
        /// <param name="routeValues">路徑參數</param>
        /// <returns></returns>
        protected IActionResult RedirectWithJsAlert(string actionPath, string msg = "", object routeValues = null)
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                TempData["_JSShowAlert"] = msg;
            }
            return RedirectToAction(actionPath, routeValues);
        }



        /// <summary>
        /// 文件編號(年月)：如果DocNoA比DocNoB大，則交換兩者順序（字典順序），確保A<=B。
        /// </summary>
        /// <param name="docNoA">文件編號(年月)A</param>
        /// <param name="docNoB">文件編號(年月)B</param>
        /// <returns>Tuple：已排序的(A,B)</returns>
        protected (string? DocNoA, string? DocNoB) GetOrderedDocNo(string? docNoA, string? docNoB)
        {
            if (!string.IsNullOrEmpty(docNoA) &&
                !string.IsNullOrEmpty(docNoB) &&
                string.Compare(docNoA, docNoB, StringComparison.Ordinal) > 0)
            {
                return (docNoB, docNoA); // 交換順序
            }

            return (docNoA, docNoB); // 不改順序
        }


        /// <summary>
        /// 將查詢後的 rows（List&lt;Dictionary&lt;string,object&gt;&gt;）依 TableHeaders 的順序輸出成 Excel。
        /// Key = 屬性名/RowNum，Value = 顯示名稱。
        /// </summary>
        protected FileContentResult GetExcelFile(
            List<Dictionary<string, object>> rows,
            Dictionary<string, string> tableHeaders,
            string initSort,
            string sheetName)
        {
            if (rows == null || rows.Count == 0)
            {
                throw new FileNotFoundException("No data to export");
            }

            var headerKeys = tableHeaders.Keys.ToList();   // 屬性名/RowNum
            var headerTexts = tableHeaders.Values.ToList(); // 顯示名

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(sheetName);

            var dataTable = Utilities.ToDataTable(rows, tableHeaders);
            ws.Cell(1, 1).InsertTable(dataTable);

            // Auto-adjust column widths
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"{sheetName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// 計算下一個主版本與次版本
        /// </summary>
        /// <param name="docVer">目前版次（格式如 "1.2", "2.0"）</param>
        /// <returns>(NextMajorVersion, NextMinorVersion)</returns>
        protected (string NextMajorVersion, string NextMinorVersion) GetNextDocVersionsNoReserve(string? docVer)
        {
            int major = 0, minor = -1; // 次版預設為 -1，這樣一開始遞增會是 0

            if (!string.IsNullOrWhiteSpace(docVer))
            {
                var parts = docVer.Split('.');
                if (parts.Length > 0)
                    int.TryParse(parts[0], out major);
                if (parts.Length > 1)
                    int.TryParse(parts[1], out minor);
                else
                    minor = -1; // 若沒有次版，從 -1 開始，等一下會 +1 成為 0
            }

            // 下一個主版次：major + 1.0
            string nextMajorVersion = $"{major + 1}.0";

            // 下一個次版次：minor + 1（可從 0 開始）
            int nextMinor = minor + 1;
            if (nextMinor > 99)
                nextMinor = 99; // 若要限制上限

            string nextMinorVersion = $"{major}.{nextMinor}";

            return (nextMajorVersion, nextMinorVersion);
        }


        /// <summary>
        /// 計算下一個主版本與次版本（跳過保留號碼：x.0, x.5）。
        /// </summary>
        /// <param name="docVer">目前版次（格式如 "1.2", "2", "3.05"）</param>
        /// <returns>(NextMajorVersion, NextMinorVersion)</returns>
        [Obsolete]
        protected (string NextMajorVersion, string NextMinorVersion) GetNextDocVersionsNoReserve_old(string? docVer)
        {
            int major = 0, minor = 0;

            if (!string.IsNullOrWhiteSpace(docVer))
            {
                var parts = docVer.Split('.');
                // 解析主版次與次版次
                if (parts.Length > 0)
                {
                    int.TryParse(parts[0], out major);
                }
                if (parts.Length > 1)
                {
                    int.TryParse(parts[1], out minor);
                }
            }

            // 下一個主版次固定為 major+1.0
            string nextMajorVersion = $"{major + 1}.0";

            // 次版次預設 +1，再跳過保留版次（如 x.0、x.5）
            int nextMinor = minor + 1;

            bool IsReserved(int n) => (n % 10 == 0 || n % 10 == 5);

            // 確保次版次不為保留號碼
            while (IsReserved(nextMinor) && nextMinor <= 99)
            {
                nextMinor++;
            }

            if (nextMinor > 99)
            {
                nextMinor = 99;
            }

            // 組合次版次字串
            string nextMinorVersion = $"{major}.{nextMinor}";

            // 特別處理只有 major（如 "2"）的情況：視為 "2.0"，次版從 2.1 開始
            if (docVer?.Contains('.') == false)
            {
                nextMinor = 1;
                while (IsReserved(nextMinor) && nextMinor <= 99)
                    nextMinor++;
                nextMinorVersion = $"{major}.{nextMinor}";
            }

            return (nextMajorVersion, nextMinorVersion);
        }

        /// <summary>
        /// 取得清理過的文件編號清單
        /// </summary>
        /// <param name="docNoRaw">原始文件編號</param>
        /// <returns></returns>
        protected List<string> GetCleanedDocNos(string docNoRaw)
        {
            /*
            第 1 碼：B 或 E
            第 2~7 碼：年月 (yyyyMM，共 6 碼數字)
            第 8~10 碼：流水號 (001–999)
            */
            var regex = new Regex(@"^[BE](\d{4})(0[1-9]|1[0-2])(0[0-9]{2}|[1-9][0-9]{2})$");

            return docNoRaw?
                .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim().ToUpperInvariant())
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Where(d =>
                {
                    var match = regex.Match(d);
                    if (!match.Success) return false;

                    // 驗證年月是否真的是合法日期
                    var year = int.Parse(match.Groups[1].Value);
                    var month = int.Parse(match.Groups[2].Value);

                    try
                    {
                        // 嘗試建立日期，例如 yyyyMM 的第一天
                        var _ = new DateTime(year, month, 1);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .Distinct()
                .ToList() ?? new List<string>();
        }



        /// <summary>
        /// 匯出Word樣板檔案
        /// </summary>
        /// <param name="code">樣板檔案代號</param>
        /// <param name="data">資料</param>
        /// <returns></returns>
        protected IActionResult ExportWordFileSingleData(string code, Dictionary<string, object> data)
        {
            // 驗證樣板是否存在
            if (!AppSettings.WordTemplates.TryGetValue(code, out var config))
            {
                return DismissModal("找不到對應的Word樣板設定（code: " + code + "）");
            }

            string templateFile = config.TemplateFile;
            string fileTitle = config.FileTitle;

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", templateFile);

            if (!System.IO.File.Exists(filePath))
            {
                return DismissModal("遺失Word樣板檔案：" + templateFile);
            }

            // 抓取請購編號用於組合檔名
            string RequestNo = data.TryGetValue("RequestNo", out var val) ? val?.ToString() ?? "" : "";

            string fileName = $"{RequestNo}_{fileTitle}.docx";

            byte[] fileBytes;
            using (var mem = new MemoryStream())
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    fs.CopyTo(mem);
                mem.Position = 0;

                using (var doc = WordprocessingDocument.Open(mem, true))
                {
                    var body = doc.MainDocumentPart.Document.Body;

                    // 將 object → string
                    var values = data.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");

                    // 1、取代本文：直接套用 SetManyByTag（全部純文字控制項）
                    WordExportHelper.SetManyByTag(body, values);

                    // 2. 取代頁首編號
                    foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
                    {
                        var header = headerPart.Header;
                        if (header != null)
                        {
                            WordExportHelper.SetManyByTag(header, values);
                        }
                    }

                    // 3、儲存
                    doc.MainDocumentPart.Document.Save();
                }

                fileBytes = mem.ToArray();
            }


            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        /// <summary>
        /// 匯出日期
        /// </summary>
        /// <param name="start">匯出起始日期</param>
        /// <param name="end">匯出結束日期</param>
        /// <returns>匯出日期文字</returns>
        protected static string BuildExportDateRange(DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue)
            {
                // 兩個都有
                return $"{start:yyyy年M月d日}～{end:yyyy年M月d日}";
            }
            else if (start.HasValue)
            {
                // 只有開始日
                return $"自 {start:yyyy年M月d日} 起";
            }
            else if (end.HasValue)
            {
                // 只有結束日
                return $"至 {end:yyyy年M月d日} 止";
            }
            else
            {
                // 兩個都沒有
                return "所有時間範圍";
            }
        }

        /// <summary>
        /// 【客製化模板】匯出Word樣板檔案
        /// </summary>
        /// <param name="code">樣板檔案代號</param>
        /// <param name="data">資料</param>
        /// <returns></returns>
        protected IActionResult ExportWordFileListData(string code, string DateRange, List<Dictionary<string, object>> BRowData, List<Dictionary<string, object>> ERowData)
        {
            if (!AppSettings.WordTemplates.TryGetValue(code, out var config))
            {
                return DismissModal("找不到對應的Word樣板設定（code: " + code + "）");
            }

            string templateFile = config.TemplateFile;
            string fileTitle = config.FileTitle;
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", templateFile);

            if (!System.IO.File.Exists(filePath))
            {
                return DismissModal("遺失Word樣板檔案：" + templateFile);
            }

            using var mem = new MemoryStream();
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fs.CopyTo(mem);
            }
            mem.Position = 0;

            using (var doc = WordprocessingDocument.Open(mem, true))
            {
                var body = doc.MainDocumentPart.Document.Body;

                // ====== 處理 Brow 區塊 ======
                if (BRowData != null && BRowData.Any())
                {


                    // 轉成 string dictionary (因為 SdtSimple 需要 string)
                    var bRows = BRowData.Select(d => d.ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value?.ToString() ?? string.Empty));

                    WordExportHelper.FillRepeatRowsByTag(body, "Brow", bRows);
                }

                // ====== 處理 Erow 區塊 ======
                if (ERowData != null && ERowData.Any())
                {
                    var eRows = ERowData.Select(d => d.ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value?.ToString() ?? string.Empty));

                    WordExportHelper.FillRepeatRowsByTag(body, "Erow", eRows);
                }


                // ====== 處理一般的單一欄位（例如日期區間、標題） ======
                if (!string.IsNullOrWhiteSpace(DateRange))
                {
                    WordExportHelper.SetTextByTag(body, "DateRange", DateRange);
                }

                // == 移除空區塊 

                var keys = BRowData.Concat(ERowData).SelectMany(d => d.Keys).Distinct(StringComparer.OrdinalIgnoreCase);


                // 1) 清除指定控制項內的文字（不影響「密／敏」）
                WordExportHelper.ClearSdtTextByTagOrAlias(doc, keys);

                // 2) 拆掉所有控制項外殼
                WordExportHelper.StripAllContentControlsSafe(doc);

                // 3) 補救空儲存格，避免 Word 跳「無法讀取的內容」
                WordExportHelper.EnsureEachCellHasParagraph(doc);


                doc.MainDocumentPart.Document.Save();
            }

            // 回傳檔案給瀏覽器下載
            //mem.Position = 0;
            mem.Seek(0, SeekOrigin.Begin);
            var fileName = $"{fileTitle}_{DateTime.Now:yyyyMMdd}.docx";
            return File(mem.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        /// <summary>
        /// 匯出Word前，做資料顯示轉換
        /// </summary>
        /// <param name="rows">資料</param>
        /// <returns></returns>
        protected List<Dictionary<string, object>> FormatRowData(List<Dictionary<string, object>> rows)
        {
            foreach (var row in rows)
            {
                var keys = row.Keys.ToList(); // 先列出所有 key，避免 foreach 修改中錯誤

                foreach (var key in keys)
                {
                    var val = row[key];

                    // 日期欄位格式化
                    if (key.Equals("in_time", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("unuse_time", StringComparison.OrdinalIgnoreCase))
                    {
                        if (DateTime.TryParse(val?.ToString(), out var dt))
                            row[key] = dt.ToString("yyyy-MM-dd");
                        else
                            row[key] = "";
                    }

                    // 文字型態的 是/否/null → ✔ / 全形空白
                    else if (key.Equals("is_confidential", StringComparison.OrdinalIgnoreCase) ||
                             key.Equals("is_sensitive", StringComparison.OrdinalIgnoreCase))
                    {
                        var str = val?.ToString()?.Trim();
                        if (str == "是")
                            row[key] = "✔";
                        else
                            row[key] = "　"; // 包含 否、null、空白，這邊的空白要用【全形空白】
                    }

                    // 文件編號
                    else if (key.Equals("original_doc_no", StringComparison.OrdinalIgnoreCase) && row["id_no"].ToString().StartsWith("E"))
                    {
                        if (val == null || string.IsNullOrEmpty((string?)val))
                        {
                            row[key] = "N/A";
                        }
                    }

                    // 其他欄位 → 空值就留空白
                    else
                    {
                        var str = val?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(str))
                            row[key] = "";
                    }
                }
            }

            return rows;
        }



        #endregion






        #region 變數

        /// <summary>
        /// 取得「上層部門」用的下拉選單
        /// </summary>
        /// <param name="onlyActive">只取啟用</param>
        /// <param name="excludeId">排除自己（防止自我參照）</param>
        /// <param name="withInactiveSuffix">停用部門是否加註 (停用)</param>
        protected SelectOption[] DepartmentParentOptions(
            bool onlyActive = true,
            int? excludeId = null,
            bool withInactiveSuffix = true)
        {
            var query = _context.Departments.AsQueryable();

            if (onlyActive)
            {
                query = query.Where(d => d.DepartmentIsActive);
            }
            if (excludeId.HasValue)
            {
                query = query.Where(d => d.DepartmentId != excludeId.Value);
            }

            var items = query
                .Select(d => new
                {
                    d.DepartmentId,
                    d.DepartmentName,
                    d.DepartmentIsActive
                })
                .AsEnumerable() // EF Core 不支援 Culture-aware OrderBy
                .Select(d => new SelectOption
                {
                    OptionValue = d.DepartmentId.ToString(),
                    OptionText = d.DepartmentName + (withInactiveSuffix && !d.DepartmentIsActive ? " (停用)" : "")
                })
                .OrderBy(x => x.OptionText, Comparer<string>.Create((a, b) =>
                    comparer.Compare(a, b, CompareOptions.StringSort)))
                .ToArray();

            return items;
        }

        /// <summary>
        /// 部門名稱下拉選單
        /// </summary>
        protected SelectOption[] DepartmentNameOptions(bool onlyActive = true, bool withInactiveSuffix = false)
        {
            var query = _context.Departments.AsQueryable();
            if (onlyActive)
                query = query.Where(d => d.DepartmentIsActive);

            var items = query
                .Select(d => new { d.DepartmentName, d.DepartmentIsActive })
                .Distinct() // 名稱如有重複可保留一筆
                .AsEnumerable()
                .Select(d => new SelectOption
                {
                    OptionValue = d.DepartmentName,
                    OptionText = d.DepartmentName + (withInactiveSuffix && !d.DepartmentIsActive ? " (停用)" : "")
                })
                .OrderBy(x => x.OptionText, Comparer<string>.Create((a, b) =>
                    comparer.Compare(a, b, CompareOptions.StringSort)))
                .ToArray();

            return items;
        }


        /// <summary>
        /// 文管系統-領用人 select
        /// </summary>
        /// <returns></returns>
        protected SelectOption[] DocAuthors()
        {
            var docAuthors = _context.Users
                .Where(user => user.UserRoles.Any(ur =>
                    ur.Role.RoleGroup == "文管" &&
                    (ur.Role.RoleName == "領用人" || ur.Role.RoleName == "負責人")))
                .Select(
                user => new SelectOption
                {
                    OptionValue = user.UserAccount,// 工號
                    OptionText = user.UserFullName + (user.UserIsActive ? "" : " (停用)")
                })
                .AsEnumerable() // 將查詢從 DB 拉到記憶體中（因 EF Core 不支援 Culture-aware OrderBy）
                .OrderBy(u => u.OptionText, Comparer<string>.Create((x, y) => comparer.Compare(x, y, CompareOptions.StringSort)))
                .Distinct()
                .ToArray();
            return docAuthors;
        }

        /// <summary>
        /// 電子採購系統-請購人 select
        /// </summary>
        /// <returns></returns>
        protected SelectOption[] Requesters(bool IsEnabled = false)
        {
            // 資料表要加入「請購人」資訊
            var users = _context.Users
                .Where(user => (!IsEnabled || user.UserIsActive))
                .Select(
                user => new SelectOption
                {
                    OptionValue = user.UserAccount,// 工號
                    OptionText = user.UserFullName + (user.UserIsActive ? "" : " (停用)")
                })
                .AsEnumerable() // 將查詢從 DB 拉到記憶體中（因 EF Core 不支援 Culture-aware OrderBy）
                .OrderBy(u => u.OptionText, Comparer<string>.Create((x, y) => comparer.Compare(x, y, CompareOptions.StringSort)))
                .Distinct()
                .ToArray();
            return users;
        }

        /// <summary>
        /// 電子採購系統-採購人 select
        /// </summary>
        /// <returns></returns>
        protected SelectOption[] Purchasers(bool IsEnabled = false)
        {
            var users = _context.Users
                .Where(user => user.UserRoles.Any(ur =>
                    ur.Role.RoleGroup == "採購" &&
                    (ur.Role.RoleName == "採購人" || ur.Role.RoleName == "評核人")) && (!IsEnabled || user.UserIsActive))
                .Select(
                user => new SelectOption
                {
                    OptionValue = user.UserAccount,// 工號
                    OptionText = user.UserFullName + (user.UserIsActive ? "" : " (停用)")
                })
                .AsEnumerable() // 將查詢從 DB 拉到記憶體中（因 EF Core 不支援 Culture-aware OrderBy）
                .OrderBy(u => u.OptionText, Comparer<string>.Create((x, y) => comparer.Compare(x, y, CompareOptions.StringSort)))
                .Distinct()
                .ToArray();
            return users;
        }

        /// <summary>
        /// 電子採購系統-收貨人 select
        /// </summary>
        /// <returns></returns>
        protected SelectOption[] ReceivePerson(bool IsEnabled = false)
        {
            var users = _context.Users
                .Where(user => (!IsEnabled || user.UserIsActive))
                .Select(
                user => new SelectOption
                {
                    OptionValue = user.UserAccount,// 工號
                    OptionText = user.UserFullName + (user.UserIsActive ? "" : " (停用)")
                })
                .AsEnumerable() // 將查詢從 DB 拉到記憶體中（因 EF Core 不支援 Culture-aware OrderBy）
                .OrderBy(u => u.OptionText, Comparer<string>.Create((x, y) => comparer.Compare(x, y, CompareOptions.StringSort)))
                .Distinct()
                .ToArray();
            return users;
        }

        /// <summary>
        /// 電子採購系統-驗收人 select
        /// </summary>
        /// <returns></returns>
        protected SelectOption[] VerifyPerson(bool IsEnabled = false)
        {
            var users = _context.Users
                .Where(user => (!IsEnabled || user.UserIsActive))
                .Select(
                user => new SelectOption
                {
                    OptionValue = user.UserAccount,// 工號
                    OptionText = user.UserFullName + (user.UserIsActive ? "" : " (停用)")
                })
                .AsEnumerable() // 將查詢從 DB 拉到記憶體中（因 EF Core 不支援 Culture-aware OrderBy）
                .OrderBy(u => u.OptionText, Comparer<string>.Create((x, y) => comparer.Compare(x, y, CompareOptions.StringSort)))
                .Distinct()
                .ToArray();
            return users;
        }

        #endregion
    }
}
