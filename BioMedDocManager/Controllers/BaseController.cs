using Aspose.Cells;
using Aspose.Words;
using BioMedDocManager.Models;
using Dapper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;// HeaderUtilities
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;


namespace BioMedDocManager.Controllers
{

    /// <summary>
    /// 基本控制器，提供共用的功能和屬性給其他控制器使用。
    /// </summary>
    /// <remarks>
    /// 預設建構子
    /// </remarks>
    /// <param name="context">資料物件</param>
    public class BaseController(DocControlContext context, IWebHostEnvironment hostingEnvironment) : Controller
    {

        #region 靜態屬性

        /// <summary>
        /// 中文欄位排序比較器
        /// </summary>
        private CompareInfo comparer = CultureInfo.GetCultureInfo("zh-TW").CompareInfo;

        /// <summary>
        /// 合法的上傳檔案屬性
        /// </summary>
        private static readonly string[] AllowedExtensions = [".docx", ".xlsx", ".pptx"];

        /// <summary>
        /// 資料庫物件
        /// </summary>
        protected readonly DocControlContext _context = context;

        /// <summary>
        /// Hash工具
        /// </summary>
        protected static readonly PasswordHasher<object> _hasher = new();

        /// <summary>
        /// 網站環境相關資訊(例如wwwroot實體路徑)
        /// </summary>
        protected readonly IWebHostEnvironment _hostingEnvironment = hostingEnvironment;

        /// <summary>
        /// Word範本檔案清單
        /// </summary>
        public static readonly Dictionary<string, (string TemplateFile, string FileTitle)> WordTemplates =
        new Dictionary<string, (string, string)>
        {
            { "Purchase", ("請購單4.0_套版.docx", "請購單(V4.0)") },
            { "Acceptance", ("收貨驗收單4.0_套版.docx", "收貨驗收單(V4.0)") },
            { "FirstAssess", ("初次供應商評核表6.0_套版.docx", "初次供應商評核表(V6.0)") },
            { "SupplierEval", ("供應商評核表6.0_套版.docx", "供應商評核表(V6.0)") },
            { "DocumentManageList", ("品質紀錄領用入庫紀錄表4.0_套版.docx", "品質紀錄領用入庫紀錄表(V4.0)") }
        };

        /// <summary>
        /// View使用的SessionKey(預設使用)
        /// </summary>
        public virtual string SessionKey =>
            $"{ControllerContext.ActionDescriptor.ControllerName}:QueryModel";
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

            // 登入者ID
            ViewData["LoginUserId"] = GetLoginUserId();

            // *** MENU 與 頁面權限 ***
            // 1) 使用者資訊
            var user = context.HttpContext.User;
            var userName = user.FindFirst("FullName")?.Value ?? "訪客";

            // 2) 權限
            var hasDoc = HasRoleGroup(user, "文管");
            var hasPur = HasRoleGroup(user, "採購");
            var hasAdmin = HasRoleGroup(user, "系統");

            // 3) 控制器與目前頁
            var effectiveController = GetEffectiveController();

            // 合併所有頁面
            var allPages = AccountPages
                .Concat(DocControlPages)
                .Concat(PurchasingPages);

            // 目前所在頁面
            var currentPage = allPages.FirstOrDefault(p => Norm(p.Controller) == effectiveController);

            // 目前所在頁面標籤
            var controllerLabel = currentPage?.Label ?? string.Empty;

            // 4) 系統選單（預設 systemPages，依權限過濾）
            var sysFilter = SystemPages
                .Where(s =>
                    (hasPur || s.Label != "電子採購") &&
                    (hasDoc || s.Label != "文件管理"))
                .ToArray();

            PageLink[] navPages = sysFilter;
            string pageMode = string.Empty;

            // 符合文管/採購控制器 → 用模組頁面 + 設定 pageMode
            if (effectiveController == "control" ||
                DocControlPages.Any(p => Norm(p.Controller) == effectiveController))
            {
                navPages = GetAvailablePages(user, DocControlPages);
                pageMode = "Document";
            }
            else if (effectiveController == "purchase" ||
                     PurchasingPages.Any(p => Norm(p.Controller) == effectiveController))
            {
                navPages = GetAvailablePages(user, PurchasingPages);
                pageMode = "Purchase";
            }

            // 5) Title 與問候語
            var baseTitle = "文管與電子採購系統";
            // 若 Action/子頁面有自己設定 ViewData["Title"]，保留它；否則用 controllerLabel
            var existingTitle = ViewData["Title"]?.ToString();
            var suffix = !string.IsNullOrWhiteSpace(controllerLabel)
                ? controllerLabel
                : (!string.IsNullOrWhiteSpace(existingTitle) ? existingTitle : null);

            var fullTitle = suffix != null ? $"{baseTitle}-{suffix}" : baseTitle;
            var greeting = GreetingByHour(DateTime.Now.Hour);

            // 6) 統一丟到 ViewData（layout 讀取）
            ViewData["UserName"] = userName;
            ViewData["HasAdmin"] = hasAdmin;
            ViewData["hasDoc"] = hasDoc;
            ViewData["hasPur"] = hasPur;

            ViewData["EffectiveController"] = effectiveController;
            ViewData["ControllerLabel"] = controllerLabel;

            ViewData["NavPages"] = navPages;
            ViewData["PageMode"] = pageMode;

            ViewData["FullTitle"] = fullTitle;
            ViewData["Greeting"] = greeting;

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// 取得小寫文字
        /// </summary>
        /// <param name="s">文字</param>
        /// <returns>小寫文字</returns>
        protected static string Norm(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();

        /// <summary>
        /// 比對屬於哪個權限群組
        /// </summary>
        /// <param name="user">登入者</param>
        /// <param name="roleGroup">權限群組</param>
        /// <returns>true：登入者是這個群組、false：登入者不是這個群組</returns>
        protected static bool HasRoleGroup(ClaimsPrincipal user, string roleGroup) =>
            user.HasClaim(c => c.Type == "RoleGroup" && c.Value == roleGroup);

        /// <summary>
        /// 取得Controller名稱
        /// </summary>
        /// <returns>Controller名稱</returns>
        protected string? GetRefController()
        {
            var r = HttpContext?.Request?.GetTypedHeaders().Referer;
            return r?.Segments.Skip(1).FirstOrDefault()?.Trim('/');
        }

        /// <summary>
        /// 取得有效的Controller名稱
        /// </summary>
        /// <returns>Controller名稱</returns>
        protected string GetEffectiveController()
        {
            var path = HttpContext?.Request?.Path.ToString() ?? string.Empty; // e.g. "/CDocumentClaim/Index"
            var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var ctl = segs.Length >= 1 ? segs[0] : string.Empty;
            var nctl = Norm(ctl);

            if (nctl == "home")
            {
                var refCtl = GetRefController();
                return Norm(refCtl);
            }
            return nctl;
        }

        /// <summary>
        /// 依照時間取得問候語
        /// </summary>
        /// <param name="hour">小時</param>
        /// <returns>問候語文字</returns>
        protected static string GreetingByHour(int hour) =>
            hour < 12 ? "早安" : (hour < 18 ? "午安" : "晚安");


        /// <summary>
        /// 取得登入者ID
        /// </summary>
        /// <returns>登入者ID</returns>
        public string GetLoginUserId()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value;
        }

        /// <summary>
        /// 依照登入者身分，顯示對應選單頁面
        /// </summary>
        /// <param name="user">登入者</param>
        /// <param name="navPages">選單頁面陣列</param>
        /// <returns></returns>
        public static PageLink[] GetAvailablePages(ClaimsPrincipal user, PageLink[] navPages)
        {
            // 取得使用者角色
            var userRoles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value);

            // 如果沒有角色，則回傳空陣列
            var result = from page in navPages
                         let csvRole = page.Roles?.Length == 1 && page.Roles[0].Contains(',')
                             ? page.Roles[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                             : []
                         where page.Roles.Intersect(userRoles).Any() || csvRole.Intersect(userRoles).Any()
                         select page;


            return [.. result];
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
            string FormPath = GetFormPath();// 取得使用者指定的檔案儲存路徑
            string sourcefilePath_REAL = Path.Combine(FormPath, model.RealFormFileName);//到時候真實檔案名稱
            string sourcefilePath_default = Path.Combine(_hostingEnvironment.WebRootPath, "docs", "範例Excel.xlsx");

            // 判斷檔案是否存在，不存在就使用範例
            string finalSourcePath = System.IO.File.Exists(sourcefilePath_REAL)
                ? sourcefilePath_REAL
                : sourcefilePath_default;

            Workbook workbook = new Workbook(finalSourcePath);
            Worksheet worksheet = workbook.Worksheets["2020"];
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
            string FormPath = GetFormPath();// 取得使用者指定的檔案儲存路徑
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
            string formPath = GetFormPath(); // 取得使用者指定的檔案儲存路徑
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

        // 將像素轉 EMU（Open XML 使用的單位）；1px(96dpi)=9525 EMU
        private static long PxToEmu(int px) => px * 9525L;

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
            var x = PxToEmu(xPx);
            var y = PxToEmu(yPx);
            var w = PxToEmu(wPx);
            var h = PxToEmu(hPx);

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
        /// 取得表單檔案
        /// </summary>
        /// <param name="model">表單物件</param>
        /// <returns>表單檔案</returns>
        protected IActionResult GetFormFile(IssueTable model)
        {
            // 檢查 model 是否為 null
            if (model == null)
            {
                return NotFound();
            }

            // 取得實體檔案路徑
            var sourcefilePath_REAL = Path.Combine(GetFormPath(), model.RealFileName);//到時候的真實檔名
            string sourcefilePath_default = Path.Combine(_hostingEnvironment.WebRootPath, "docs", "範例Word.docx");

            // 判斷檔案是否存在，不存在就使用範例
            string finalSourcePath = System.IO.File.Exists(sourcefilePath_REAL)
                ? sourcefilePath_REAL
                : sourcefilePath_default;

            // 檢查檔案是否存在
            if (!System.IO.File.Exists(finalSourcePath))
            {
                return NotFound();
            }

            var asciiFileName = "download"; // ASCII-safe 備援名稱

            // 讀取檔案內容
            var fileBytes = System.IO.File.ReadAllBytes(finalSourcePath);

            // 轉成UTF-8檔名
            string encodedName = Uri.EscapeDataString(model.RealFileName);   // 轉成UTF-8

            // 撰寫檔名Disposition
            var disposition = $"attachment; filename=\"{asciiFileName}\"; filename*=UTF-8''{encodedName}";

            Response.Headers[HeaderNames.ContentDisposition] = disposition;

            return File(fileBytes, model.ContentType);

        }

        /// <summary>
        /// 儲存表單檔案到指定路徑
        /// </summary>
        /// <param name="file">上傳的檔案 (IFormFile)</param>
        /// <param name="model">IssueTable 物件，用於命名與寫入副檔名</param>
        /// <returns>成功儲存後的完整檔案路徑；若失敗則回傳 null</returns>
        protected string SaveFormFile(IFormFile file, IssueTable model)
        {
            if (file == null || file.Length == 0 || model == null)
                return null;

            try
            {
                // 取得儲存路徑
                var savePath = GetFormPath();

                // 確保資料夾存在
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                // 取得副檔名與儲存檔名
                var fileExt = Path.GetExtension(file.FileName); // e.g., ".docx"

                // 更新模型的副檔名
                model.FileExtension = fileExt.TrimStart('.'); // e.g., "docx"

                var fileName = $"{model.OriginalDocNo}(v{model.DocVer}).{model.FileExtension}";

                // 組成完整路徑
                var fullPath = Path.Combine(savePath, fileName);

                // 儲存檔案
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                return model.FileExtension;
            }
            catch (Exception ex)
            {
                // 這裡可以加上 log 或錯誤處理
                Console.WriteLine("檔案儲存失敗：" + ex.Message);
                return "";
            }
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
        /// 刪除表單檔案(真的會刪除檔案)
        /// </summary>
        /// <param name="model">IssueTable 物件，用於抓取檔名</param>
        protected void DeleteFormFile(IssueTable model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.OriginalDocNo) || string.IsNullOrWhiteSpace(model.DocVer) || string.IsNullOrWhiteSpace(model.FileExtension))
                return;

            try
            {
                // 組成檔案名稱與路徑
                var savePath = GetFormPath();
                var fileName = $"{model.OriginalDocNo}(v{model.DocVer}).{model.FileExtension}";
                var fullPath = Path.Combine(savePath, fileName);

                // 檢查檔案是否存在並刪除
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    Console.WriteLine("刪除表單檔案，檔案已刪除：" + fullPath);
                }
            }
            catch (Exception ex)
            {
                // 可以記錄 log
                Console.WriteLine("刪除表單檔案，檔案刪除失敗：" + ex.Message);
            }
        }


        /// <summary>
        /// 確認要領用的表單是存在的、領用日期>=表單發行日期
        /// </summary>
        /// <param name="date">領用日期</param>
        /// <param name="DocNo">表單編號</param>
        /// <param name="docver">表單版次</param>
        /// <returns>T：檔案存在、F：檔案不存在</returns>
        protected bool CheckIssueTablesExist(DateTime date, string? DocNo, string? docver)
        {
            var formIssue = _context.IssueTables
                    .FirstOrDefault(m => m.OriginalDocNo == DocNo && m.DocVer == docver && m.IssueDatetime <= date);

            if (formIssue == null)
            {
                return false;
            }

            return true;

        }

        /// <summary>
        /// 取得表單儲存路徑
        /// </summary>
        /// <returns>表單儲存路徑</returns>
        public string GetFormPath()
        {

            // 取得表單儲存路徑
            var form_path = _context.Bulletins.FirstOrDefault(b => b.Code == "form_path");
            return form_path?.Value ?? string.Empty;
        }

        /// <summary>
        /// 取得文件儲存路徑
        /// </summary>
        /// <returns>文件儲存路徑</returns>
        public string GetDocPath()
        {

            // 取得文件儲存路徑
            var doc_path = _context.Bulletins.FirstOrDefault(b => b.Code == "doc_path");
            return doc_path?.Value ?? string.Empty;
        }

        /// <summary>
        /// 取得登入公告設定
        /// </summary>
        /// <returns></returns>
        public string? GetLoginMessage()
        {
            var doc_path = _context.Bulletins.Where(b => b.Code == "login_message").FirstOrDefault();

            return (doc_path != null && !string.IsNullOrEmpty(doc_path.Value)) ? doc_path.Value : string.Empty;
        }


        /// <summary>
        /// 取得「文件領用」公告訊息與關閉文件領用日期。
        /// </summary>
        /// <param name="type">網址參數 type（例：demo）</param>
        /// <param name="key">網址參數 key（例：vbuWad_Gyr5j25f）</param>
        /// <param name="inDatetime">網址參數 inDatetime（日期字串）</param>
        /// <param name="previewContent">預覽模式下自行帶入的 BulletinContent</param>
        /// <returns>
        /// Tuple：<br/>
        /// - Item1 → messages (string) 公告訊息內容<br/>
        /// - Item2 → turnOffDate (DateOnly?) 關閉文件領用日期，若無則為 null
        /// </returns>
        public (string Message, DateTime? TurnOffDate) GetDocCtrlBulletin(
            string? type = "",
            string? key = "",
            string? inDatetime = "",
            string? previewContent = "")
        {
            // 先預設空值
            string messages = string.Empty;
            string stringDate = "";
            DateTime? turnOffDate = null;

            // 取得關閉文件領用日期、訊息內容
            var turnoff = _context.Bulletins.FirstOrDefault(b => b.Code == "turnoff_date");
            var turnoff_content = _context.Bulletins.FirstOrDefault(b => b.Code == "turnoff_content");

            // 是否為「管理預覽模式」？
            bool isPreview = !string.IsNullOrEmpty(type) &&
                             type.Equals("demo", StringComparison.OrdinalIgnoreCase) &&
                             !string.IsNullOrEmpty(key) &&
                             key == "vbuWad_Gyr5j25f" &&
                             !string.IsNullOrEmpty(inDatetime) &&
                             !string.IsNullOrEmpty(previewContent);

            if (isPreview)
            {
                // 預覽：使用網址帶入的內容
                messages = previewContent!;// 訊息內容
                stringDate = inDatetime ?? "2024-04-30"; ;// 關閉文件領用日期
            }
            else
            {
                // 一般模式：抓資料庫設定
                messages = turnoff_content?.Value ?? string.Empty;// 訊息內容
                stringDate = turnoff?.Value ?? "2024-04-30";
            }

            if (DateTime.TryParse(stringDate, out var dt))
            {
                turnOffDate = dt;// 關閉文件領用日期
            }

            turnOffDate = dt;// 關閉文件領用日期

            return (messages, turnOffDate);
        }

        /// <summary>
        /// 驗證領用日期是否在關閉日與當日之間
        /// </summary>
        /// <param name="claimDate">領用日期</param>
        /// <param name="turnOffDate">關閉日</param>
        /// <returns>true 表示合法</returns>
        public bool IsValidClaimDate(DateTime claimDate)
        {
            // 確認是否為管理設定的預覽模式，並取得相關參數(公告訊息、關閉文件領用日期)
            var (messages, turnOffDate) = GetDocCtrlBulletin();

            return IsDateAGreaterOrEqualThanB(claimDate, turnOffDate) && IsDateAGreaterOrEqualThanB(DateTime.Today, claimDate);
        }

        /// <summary>
        /// 依據輸入日期與文件類型，產生文件領用編號（純文字回傳）
        /// </summary>
        /// <param name="date">領用日期（yyyy-MM-dd）</param>
        /// <param name="docType">文件類別（如B/E）</param>
        /// <param name="error">錯誤訊息（若有錯）</param>
        /// <returns>文件編號，或空字串</returns>
        public string GetDocNumber(string date, string docType, string controllerType)
        {

            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                return "領用日期格式錯誤";
            }

            if (controllerType == "CDocumentClaim" && !IsValidClaimDate(parsedDate))
            {
                return "領用日期選擇錯誤，應於關閉日期~當日之間";
            }

            string monthString = parsedDate.Year.ToString() + parsedDate.Month.ToString("D2"); // 補0
            string docNoPrefix = docType + monthString;
            if (controllerType == "CDocumentClaim")
            {
                return NonReserveDocNos(docNoPrefix); // 一般領用取號
            }
            else if (controllerType == "CDocumentClaimReserve")
            {
                return ReserveDocNos(docNoPrefix); // 保留號領用取號
            }
            else
            {
                return "文件類別錯誤";
            }
        }

        /// <summary>
        /// 將表單資料填入 DocControlMaintable 模型中，依據不同欄位與類別判斷設定屬性。
        /// </summary>
        /// <param name="formData">Request.Form資料</param>
        /// <param name="model">DocControlMaintable Model</param>
        protected void BindDocControlModelFromForm(IFormCollection formData, DocControlMaintable model)
        {
            // 先抓文件類別（決定是B或E）
            model.Type = formData["rdbtype"];

            // 依據Key設定各欄位值
            foreach (var key in formData.Keys)
            {
                switch (key)
                {
                    case "rdbtype":
                        model.Type = formData[key];
                        break;

                    // 理論上要刪掉*******
                    //case "txt_person_name":
                    //    model.PersonName = formData[key];
                    //    break;
                    // *******************
                    case "DateTime":
                        DateTime.TryParse(formData[key], out DateTime parsedDate);
                        model.DateTime = parsedDate;
                        break;

                    case "txt_person_id":
                    case "Id":
                        model.Id = formData[key];
                        break;
                    case "txt_project_name":
                        model.ProjectName = formData[key];
                        break;

                    case "txt_Boriginal_doc_no" when model.Type == "B":
                        model.OriginalDocNo = formData[key];
                        break;

                    case "txt_Eoriginal_doc_no" when model.Type == "E":
                        model.OriginalDocNo = formData[key];
                        break;

                    case "txt_Bdoc_ver" when model.Type == "B":
                        model.DocVer = formData[key];
                        break;

                    case "txt_Bpurpose" when model.Type == "B":
                        model.Purpose = formData[key];
                        break;

                    case "txt_Epurpose" when model.Type == "E":
                        model.Purpose = formData[key];
                        break;

                    case "txt_Bname" when model.Type == "B":
                        model.Name = formData[key];
                        break;

                    case "txt_Ename" when model.Type == "E":
                        model.Name = formData[key];
                        break;

                    case "btnSend":
                    case "txt_nextIdNo":
                    case "__RequestVerificationToken":
                    default:
                        // 忽略不需綁定的欄位
                        break;
                }
            }

            // model變數是Call by Reference，所以沒有用return
        }

        /// <summary>
        /// 驗證 DocControlMaintable 表單內容是否合法，回傳錯誤訊息列表。
        /// </summary>
        /// <param name="model">DocControlMaintable 模型</param>
        /// <returns>錯誤訊息集合，如無錯誤則為空</returns>
        protected List<string> ValidateDocControlForm(DocControlMaintable model)
        {
            var errors = new List<string>();

            // 共用欄位驗證
            if (!model.DateTime.HasValue)
            {
                errors.Add("請選擇領用日期。");
            }
            if (string.IsNullOrWhiteSpace(model.Type))
            {
                errors.Add("請選擇文件類別。");
            }
            if (string.IsNullOrWhiteSpace(model.Id))
            {
                errors.Add("請選擇領用人。");
            }

            // 廠內文件驗證（B）
            if (model.Type == "B")
            {
                if (string.IsNullOrWhiteSpace(model.OriginalDocNo))
                {
                    errors.Add("請輸入表單編號。");
                }
                if (string.IsNullOrWhiteSpace(model.DocVer))
                {
                    errors.Add("請輸入表單版次。");
                }
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    errors.Add("請輸入紀錄名稱。");
                }
                if (string.IsNullOrWhiteSpace(model.Purpose))
                {
                    errors.Add("請輸入領用目的。");
                }

                // 檢查表單是否存在（假設 CheckIssueTablesExist 為可用的方法）
                if (!CheckIssueTablesExist(model.DateTime.Value, model.OriginalDocNo, model.DocVer))
                {
                    errors.Add("請選擇正確的表單編號與表單版次。");
                }
            }

            // 外來文件驗證（E）
            if (model.Type == "E")
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    errors.Add("請輸入文件名稱。");
                }
                if (string.IsNullOrWhiteSpace(model.Purpose))
                {
                    errors.Add("請輸入內容簡述。");
                }
            }

            return errors;
        }

        /// <summary>
        /// 新增文件領用紀錄
        /// </summary>
        /// <param name="input">資料</param>
        /// <returns></returns>
        protected async Task InsertDocControlMaintableAsync(DocControlMaintable input)
        {

            var IssueTable = _context.IssueTables.FirstOrDefault(m => m.OriginalDocNo == input.OriginalDocNo && m.DocVer == input.DocVer);
            input.FileExtension = IssueTable?.FileExtension ?? "docx"; // 預設為docx

            _context.DocControlMaintables.Add(input);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 轉跳頁面並顯示js訊息
        /// </summary>
        /// <param name="actionPath">轉跳路徑</param>
        /// <param name="msg">訊息</param>
        /// <param name="routeValues">路徑參數</param>
        /// <returns></returns>
        public IActionResult RedirectWithJsAlert(string actionPath, string msg = "", object routeValues = null)
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                TempData["_JSShowAlert"] = msg;
            }
            return RedirectToAction(actionPath, routeValues);
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
        /// 交換日期；若date1>date2則交換
        /// </summary>
        /// <param name="date1">日期1</param>
        /// <param name="date2">日期2</param>
        protected static (DateTime? Start, DateTime? End) GetOrderedDates(DateTime? date1, DateTime? date2)
        {
            if (date1 != null && date2 != null)
            {
                if (date1 > date2)
                {
                    return (date2, date1);
                }
                return (date1, date2);
            }

            return (date1, date2);
        }

        /// <summary>
        /// 交換數字；若 num1 > num2 則交換
        /// </summary>
        /// <typeparam name="T">可比較的數值型別（如 int、decimal、double）</typeparam>
        /// <param name="num1">數字1</param>
        /// <param name="num2">數字2</param>
        /// <returns>回傳 (較小, 較大)</returns>
        protected static (T? Min, T? Max) GetOrderedNumbers<T>(T? num1, T? num2) where T : struct, IComparable<T>
        {
            if (num1.HasValue && num2.HasValue)
            {
                if (num1.Value.CompareTo(num2.Value) > 0)
                {
                    return (num2, num1);
                }
                return (num1, num2);
            }

            return (num1, num2);
        }

        /// <summary>
        /// 判斷日期A是否大於日期B（支援nullable）
        /// </summary>
        /// <param name="a">日期A</param>
        /// <param name="b">日期B</param>
        /// <returns>true：A大於B，false：A小於B</returns>
        public static bool IsDateAGreaterOrEqualThanB(DateTime? a, DateTime? b)
        {
            if (!a.HasValue || !b.HasValue)
                return false;

            return a.Value >= b.Value;
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
        /// 匯出查詢結果Excel
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <param name="sqlDef">SQL查詢</param>
        /// <param name="parameters">SQL查詢參數</param>
        /// <param name="TableHeaders">表頭</param>
        /// <param name="sheetName">檔名</param>
        /// <returns>查詢結果Excel檔</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        // TODO Rename to GetExcelFileAsync?
        public async Task<IActionResult> GetExcelFile<T>(T queryModel, string sqlDef, DynamicParameters parameters, Dictionary<string, string> TableHeaders, string InitSort, string sheetName) where T : Pagination
        {
            try
            {

                FilterOrderBy<T>(queryModel, TableHeaders, InitSort);

                var queryOrderBy = $"{queryModel.OrderBy} {queryModel.SortDir ?? "desc"}".Trim();

                var excelQuery = await _context.ExportToExcelAsync($" {sqlDef}  ORDER BY {queryOrderBy} ", headers: TableHeaders, parameters, sheetName);

                // 設定匯出檔名
                return File(excelQuery, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{sheetName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            }
            catch (FileNotFoundException)
            {
                // 查無結果 不提供檔案
                return NotFound();
            }

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
        /// 取得所有角色資料(系統權限除外)
        /// </summary>
        /// <returns></returns>
        protected async Task<List<Role>> GetRoles()
        {
            // 取得所有角色資料(系統權限除外)
            return await _context.Roles
                  //.Where(r => r.RoleGroup != "系統")
                  .OrderBy(r => r.RoleGroup)
                  .ThenBy(r => r.RoleName)
                  .ToListAsync();
        }

        /// <summary>
        /// 將CreateUser轉回User
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected static User ToUserEntity(CreateUser model)
        {
            return new User
            {
                UserName = model.Username,
                FullName = model.FullName,
                Password = HashPassword(model, model.Password),
                IsActive = model.IsActive,
                CreatedAt = model.CreatedAt
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
        /// 檢查檔名副檔名是否合法(大小寫不敏感)
        /// </summary>
        public static bool IsValidFileExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }

        /// <summary>
        /// 透過請購編號取得合格供應商資料
        /// </summary>
        /// <param name="RequestNo">請購編號</param>
        /// <returns></returns>
        public async Task<QualifiedSupplier> GetQualifiedSupplierByRequestNo(string RequestNo)
        {
            var purchase = await _context.PurchaseRecords
                .Include(p => p.RequesterUser)
                .Include(p => p.PurchaserUser)
                .FirstOrDefaultAsync(s => s.RequestNo == RequestNo);

            // 複合條件抓對應供應商
            var supplierInfo = await _context.QualifiedSuppliers
                .FirstOrDefaultAsync(m =>
                    m.SupplierName == purchase.SupplierName &&
                    m.ProductClass == purchase.ProductClass)
                ?? new QualifiedSupplier();

            return supplierInfo;
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
            if (!WordTemplates.TryGetValue(code, out var config))
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
        /// 匯出日期小工具
        /// </summary>
        /// <param name="start">匯出起始日期</param>
        /// <param name="end">匯出結束日期</param>
        /// <returns>匯出日期文字</returns>
        public static string BuildExportDateRange(DateTime? start, DateTime? end)
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
            if (!WordTemplates.TryGetValue(code, out var config))
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
        public List<Dictionary<string, object>> FormatRowData(List<Dictionary<string, object>> rows)
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

        /// <summary>
        /// Object轉換成Dictionary格式
        /// </summary>
        /// <typeparam name="T">型態</typeparam>
        /// <param name="obj">Object物件</param>
        /// <returns>Dictionary格式資料</returns>
        public static Dictionary<string, object?> ToDictionary<T>(T obj)
        {
            var result = new Dictionary<string, object?>();

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead);

            foreach (var prop in props)
            {
                var value = prop.GetValue(obj);

                if (value is DateTime dt)
                {
                    result[prop.Name] = dt.ToString("yyyy-MM-dd");
                }
                else if (value is DateTime?)
                {
                    var nullable = (DateTime?)value;
                    result[prop.Name] = nullable.HasValue ? nullable.Value.ToString("yyyy-MM-dd") : null;
                }
                else
                {
                    result[prop.Name] = value;
                }
            }

            return result;
        }

        /// <summary>
        ///  標記小工具：滿足條件回傳 V ，否則回傳空字串
        /// </summary>
        /// <param name="condition">條件式</param>
        /// <returns></returns>
        private static string MarkCheck(bool condition) => condition ? "✔" : "　";

        /// <summary>
        /// 標記小工具：滿足條件回傳 ■ ，否則回傳 □
        /// </summary>
        /// <param name="condition">條件式</param>
        /// <returns></returns>
        private static string MarkCheckRadio(bool condition) => condition ? "■" : "□";

        /// <summary>
        /// 數值是否與選項內容相同（大小寫不敏感）
        /// </summary>
        /// <param name="value">數值</param>
        /// <param name="candidates">選項陣列</param>
        /// <returns></returns>
        private static bool EqualsAny(string? value, params string[] candidates)
        {
            if (value is null) return false;
            foreach (var c in candidates)
            {
                if (string.Equals(value.Trim(), c, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 將「單一分數欄位」展開成「多個 V/空白 欄位」並寫入 dict options 代表該欄位所有可能的固定分數（按你規則固定，不需檢查範圍）
        /// </summary>
        /// <param name="dict">資料</param>
        /// <param name="baseName">欄位名</param>
        /// <param name="value">數值</param>
        /// <param name="options">數值中的選項</param>
        private static void FillScoreFlags(IDictionary<string, object> dict, string baseName, int? value, params int[] options)
        {
            foreach (var score in options)
            {
                var key = $"{baseName}{score}";
                dict[key] = MarkCheck(value == score);
            }

            // 移除原本的單值欄位，避免干擾
            if (dict.ContainsKey(baseName))
                dict.Remove(baseName);
        }

        /// <summary>
        /// 填入評分旗標（依級距展開）
        /// </summary>
        /// <param name="purchaseRecord">原始請購資料</param>
        /// <param name="dict">要匯出的資料</param>
        protected static void ApplyScoreFlags(PurchaseRecord purchaseRecord, IDictionary<string, object> dict)
        {
            // 依你的固定級距展開
            FillScoreFlags(dict, "PriceSelect", purchaseRecord.PriceSelect, 10, 5);
            FillScoreFlags(dict, "SpecSelect", purchaseRecord.SpecSelect, 25, 15, 0);
            FillScoreFlags(dict, "ServiceSelect", purchaseRecord.ServiceSelect, 15, 10, 5, 0);
            FillScoreFlags(dict, "DeliverySelect", purchaseRecord.DeliverySelect, 10, 0);
            FillScoreFlags(dict, "QualitySelect", purchaseRecord.QualitySelect, 40, 25, 5);
        }

        /// <summary>
        /// 填入標記（依選項展開）
        /// </summary>
        /// <param name="dict">資料</param>
        /// <param name="baseName">欄位名</param>
        /// <param name="value">數值</param>
        /// <param name="options">選項</param>
        private static void FillEnumFlags(
            IDictionary<string, object> dict,
            string baseName,
            string? value,
            params (string Suffix, string[] Matches)[] options)
        {
            foreach (var (suffix, matches) in options)
            {
                var key = $"{baseName}{suffix}";
                dict[key] = MarkCheckRadio(EqualsAny(value, matches));
            }

            // 清掉原本單一欄位避免干擾
            if (dict.ContainsKey(baseName))
                dict.Remove(baseName);
        }

        /// <summary>
        /// 初供評核結果
        /// </summary>
        /// <param name="supplier1stAssess">原始初供資料</param>
        /// <param name="dict">要匯出的資料</param>
        protected static void ApplyQualityAgreementFlags(
            PurchaseRecord purchaseRecord,
            IDictionary<string, object> dict)
        {
            var v = purchaseRecord.QualityAgreement; // 例如 "是"、"否"
            FillEnumFlags(dict, "QualityAgreement", v,
                ("_Y", new[] { "是" }),
                ("_N", new[] { "否" })
            );
        }

        /// <summary>
        /// 初供評核結果
        /// </summary>
        /// <param name="supplier1stAssess">原始初供資料</param>
        /// <param name="dict">要匯出的資料</param>
        protected static void ApplyAssessResultFlags(
            Supplier1stAssess supplier1stAssess,
            IDictionary<string, object> dict)
        {
            var v = supplier1stAssess.AssessResult; // 例如 "合格"、"改善後合格"、"不合格"
            FillEnumFlags(dict, "AssessResult", v,
                ("Qualified", new[] { "合格" }),
                ("Requalified", new[] { "改善後合格" }),
                ("Unqualified", new[] { "不合格" })
            );
        }

        /// <summary>
        /// 供應商分類
        /// </summary>
        /// <param name="supplier1stAssess">原始初供資料</param>
        /// <param name="dict">要匯出的資料</param>
        protected static void ApplySupplierClassFlags(
            Supplier1stAssess supplier1stAssess,
            IDictionary<string, object> dict)
        {
            var v = supplier1stAssess.SupplierClass; // 可能是 "RM" 或 "原料供應商" 之類
            FillEnumFlags(dict, "SupplierClass", v,
                ("RM", new[] { "原料供應商" }),
                ("MI", new[] { "雜項供應商" }),
                ("SP", new[] { "特殊供應商" })
            );
        }

        /// <summary>
        /// 風險類型
        /// </summary>
        /// <param name="supplier1stAssess">原始初供資料</param>
        /// <param name="dict">要匯出的資料</param>
        protected static void ApplyRiskLevelFlags(
            Supplier1stAssess supplier1stAssess,
            IDictionary<string, object> dict)
        {
            var v = supplier1stAssess.RiskLevel; // 例如 "高風險"、"中風險"、"低風險"
            FillEnumFlags(dict, "RiskLevel", v,
                ("Height", new[] { "高" }),
                ("Medium", new[] { "中" }),
                ("Low", new[] { "低" })
            );
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
            string html = $@"<script{nonceAttr}>window.parent.dismiss(""{safeMsg}"");</script>";

            return Content(html, "text/html; charset=utf-8");
        }










        #endregion






        #region 變數
        /// <summary>
        /// 文管系統-前綴詞
        /// </summary>
        protected string _prefix
        {
            get
            {
                string? prefix = HttpContext.Session.GetString("SearchIssueModalPrefix");
                if (string.IsNullOrEmpty(prefix))
                {
                    prefix = "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    HttpContext.Session.SetString("SearchIssueModalPrefix", prefix);
                }
                return prefix;
            }
        }

        /// <summary>
        /// 文管系統-文件編號前綴詞
        /// </summary>
        protected string _CDprefix
        {
            get
            {
                string? prefix = HttpContext.Session.GetString("CDocumentPrefix");
                if (string.IsNullOrEmpty(prefix))
                {
                    prefix = "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    HttpContext.Session.SetString("CDocumentPrefix", prefix);
                }
                return prefix;
            }
        }

        public static class AdminRoleStrings
        {
            public const string 系統管理者 = "系統管理者";
        }

        public static class DocRoleStrings
        {
            public const string 領用人 = "領用人";
            public const string 負責人 = "負責人";

            public const string Anyone = $"{領用人},{負責人}";
        }

        public static class PurchaseRoleStrings
        {
            // 定義：本廠內任何需求人員皆可進行請購作業。
            public const string 請購人 = "請購人";

            // 定義：本廠內行政部人員皆可進行採購作業。
            public const string 採購人 = "採購人";

            // 定義：需具備新版醫療器材品質管理系統(QMS)或ISO 13485品質系統相關訓練證明，並依「人力資源管理作業程序書(BMP-QP04)」程序進行專業人員任命作業。
            public const string 評核人 = "評核人";

            /// <summary>
            /// 僅限特殊畫面使用
            /// </summary>
            public const string Anyone = $"{請購人},{採購人},{評核人}";
        }

        /// <summary>
        /// 系統初始選單
        /// </summary>
        public static readonly PageLink[] SystemPages =
        [
            new PageLink { Controller = "Purchase", Label = "電子採購" , Roles = [PurchaseRoleStrings.Anyone] },
            new PageLink { Controller = "Control",  Label = "文件管理" , Roles = [DocRoleStrings.Anyone] },
        ];

        /// <summary>
        /// 帳號管理頁面選單
        /// </summary>
        public static readonly PageLink[] AccountPages =
        [
            new PageLink { Controller = "AccountSettings", Label = "帳號設定", Roles = [AdminRoleStrings.系統管理者] }
        ];

        /// <summary>
        /// 文管系統-各頁面選單
        /// </summary>
        public static readonly PageLink[] DocControlPages =
        [
            new PageLink { Controller = "CDocumentClaim", Label = "文件領用", Roles = [DocRoleStrings.領用人] },
            new PageLink { Controller = "CFileQuery", Label = "文件查詢", Roles = [DocRoleStrings.領用人] },
            new PageLink { Controller = "CDocumentCancel", Label = "文件註銷", Roles = [DocRoleStrings.領用人] },
            new PageLink { Controller = "COldDocCtrlMaintables", Label = "2020年前表單查詢", Roles = [DocRoleStrings.領用人] },
            new PageLink { Controller = "CFormQuery", Label = "表單查詢", Roles = [DocRoleStrings.領用人] },
            new PageLink { Controller = "CDocumentClaimReserve", Label = "保留號文件領用", Roles = [DocRoleStrings.負責人] },
            new PageLink { Controller = "CIssueTables", Label = "表單發行", Roles = [DocRoleStrings.負責人] },
            new PageLink { Controller = "CDocumentManage", Label = "文件管制", Roles = [DocRoleStrings.負責人] },
            new PageLink { Controller = "CBatchStorage", Label = "批量入庫", Roles = [DocRoleStrings.負責人] },
            new PageLink { Controller = "CManagementSettings", Label = "管理設定", Roles = [DocRoleStrings.負責人] }
        ];

        /// <summary>
        /// 電子採購系統-各頁面選單
        /// </summary>
        public static readonly PageLink[] PurchasingPages =
        [
            new PageLink { Controller = "PSupplier1stAssess", Label = "初供評核", Roles = [PurchaseRoleStrings.評核人] },// 任何人請購人都可初供評核
            new PageLink { Controller = "PProductClass", Label = "品項選單維護",  Roles = [PurchaseRoleStrings.評核人]},// 限評核人才能維護品項選單
            new PageLink { Controller = "PPurchaseTables", Label = "請購", Roles = [PurchaseRoleStrings.Anyone] },// 任何人都可請購
            new PageLink { Controller = "PAcceptance", Label = "驗收", Roles = [PurchaseRoleStrings.Anyone] },// 任何人都可驗收
            new PageLink { Controller = "PAssessment", Label = "評核與其他紀錄", Roles = [PurchaseRoleStrings.評核人] },// 限評核人才能評核
            new PageLink { Controller = "PAssessmentResult", Label = "評核結果查詢", Roles = [PurchaseRoleStrings.Anyone] },// 任何人都可看評核結果
            new PageLink { Controller = "PPurchaseRecords", Label = "請購分析",  Roles = [PurchaseRoleStrings.Anyone]},// 任何人都可看請購分析
            new PageLink { Controller = "PQualifiedSuppliers", Label = "供應商清冊", Roles = [PurchaseRoleStrings.Anyone] },// 任何人都可查看供應商清冊、新增供應商
            new PageLink { Controller = "PSupplierReassessments", Label = "再評估",  Roles = [PurchaseRoleStrings.評核人] },// 限評核人才能再評估            
        ];

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
                    OptionValue = user.UserName,// 工號
                    OptionText = user.FullName + (user.IsActive ? "" : " (停用)")
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
        public SelectOption[] Requesters(bool IsEnabled = false)
        {
            // 資料表要加入「請購人」資訊
            var users = _context.Users
                .Where(user => (!IsEnabled || user.IsActive))
                .Select(
                user => new SelectOption
                {
                    OptionValue = user.UserName,// 工號
                    OptionText = user.FullName + (user.IsActive ? "" : " (停用)")
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
        public SelectOption[] Purchasers(bool IsEnabled = false)
        {
            var users = _context.Users
                .Where(user => user.UserRoles.Any(ur =>
                    ur.Role.RoleGroup == "採購" &&
                    (ur.Role.RoleName == "採購人" || ur.Role.RoleName == "評核人")) && (!IsEnabled || user.IsActive))
                .Select(
                user => new SelectOption
                {
                    OptionValue = user.UserName,// 工號
                    OptionText = user.FullName + (user.IsActive ? "" : " (停用)")
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
        public SelectOption[] ReceivePerson(bool IsEnabled = false)
        {
            var users = _context.Users
                .Where(user => (!IsEnabled || user.IsActive))
                .Select(
                user => new SelectOption
                {
                    OptionValue = user.UserName,// 工號
                    OptionText = user.FullName + (user.IsActive ? "" : " (停用)")
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
        public SelectOption[] VerifyPerson(bool IsEnabled = false)
        {
            var users = _context.Users
                .Where(user => (!IsEnabled || user.IsActive))
                .Select(
                user => new SelectOption
                {
                    OptionValue = user.UserName,// 工號
                    OptionText = user.FullName + (user.IsActive ? "" : " (停用)")
                })
                .AsEnumerable() // 將查詢從 DB 拉到記憶體中（因 EF Core 不支援 Culture-aware OrderBy）
                .OrderBy(u => u.OptionText, Comparer<string>.Create((x, y) => comparer.Compare(x, y, CompareOptions.StringSort)))
                .Distinct()
                .ToArray();
            return users;
        }

        /// <summary>
        /// 電子採購系統-品項編號
        /// </summary>
        /// <returns></returns>
        public List<ProductClass> ProductClassMenu(bool IsEnabled = false)
        {
            var list = _context.ProductClasses
                .Where(pc => !IsEnabled || !pc.ProductClassTitle.Contains("停用"))
                .AsEnumerable() // 將查詢從 DB 拉到記憶體中（因 EF Core 不支援 Culture-aware OrderBy）
                .OrderBy(u => u.ProductClassTitle, Comparer<string>.Create((x, y) => comparer.Compare(x, y, CompareOptions.StringSort)))
                .ToList();

            return list;
        }

        /// <summary>
        /// 電子採購系統-供應商
        /// </summary>
        /// <returns></returns>
        public List<QualifiedSupplier> SupplierMenu()
        {
            var list = _context.QualifiedSuppliers
                .AsEnumerable() // 將查詢從 DB 拉到記憶體中（因 EF Core 不支援 Culture-aware OrderBy）
                .OrderBy(u => u.SupplierName, Comparer<string>.Create((x, y) => comparer.Compare(x, y, CompareOptions.StringSort)))
                .ThenBy(u => u.SupplierClass, Comparer<string>.Create((x, y) => comparer.Compare(x, y, CompareOptions.StringSort)))
                .ToList();

            return list;
        }


        #endregion
    }
}
