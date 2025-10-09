using CoreProj2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;
using Aspose.Words;
using Aspose.Cells;

namespace CoreProj2.Controllers
{
    public class HomeController(IWebHostEnvironment hostingEnvironment) : Controller
    {
        //private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment = hostingEnvironment;

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("/Home/WordExample", Name = "WordExample")]
        public string WordExample()
        {
            // 載入文檔            
            string contentRootPath = _hostingEnvironment.ContentRootPath;
            string sourcefilePath = System.IO.Path.Combine(contentRootPath, "Document", "BMP-QM01-TR005 會議紀錄表 V7.2-20240701發行.docx");
            Document doc = new Document(sourcefilePath);
            DocumentBuilder builder = new DocumentBuilder(doc);

            // 設定頁面邊距
            Aspose.Words.PageSetup pageSetup = builder.PageSetup;
            pageSetup.HeaderDistance = ConvertUtil.MillimeterToPoint(4);

            // 設定首頁不同
            //builder.PageSetup.DifferentFirstPageHeaderFooter = true;

            // 設定頁首
            builder.MoveToHeaderFooter(HeaderFooterType.HeaderPrimary);
            builder.ParagraphFormat.Alignment = ParagraphAlignment.Center;
            builder.ParagraphFormat.LineSpacingRule = LineSpacingRule.AtLeast; //設置間距
            builder.ParagraphFormat.LineSpacing = 5;
            builder.Font.Name = "Calibri";
            builder.Font.Size = 16;
            builder.Font.Bold = true;
            builder.Write("B202408028");

            // 儲存為新檔案
            string outFilePath = System.IO.Path.Combine(contentRootPath, "output", "Output.docx");
            doc.Save(outFilePath);
            Process.Start(new ProcessStartInfo(outFilePath) { UseShellExecute = true });

            return "完成";
        }

        [Route("/Home/ExcelExample", Name = "ExcelExample")]
        public string ExcelExample()
        {
            // 開啟Excel檔案
            string contentRootPath = _hostingEnvironment.ContentRootPath;
            string sourcefilePath = System.IO.Path.Combine(contentRootPath, "Document", "BMP-QP14-TR002 設備總覽表 v4.0.xlsx");
            Workbook workbook = new Workbook(sourcefilePath);

            // 獲取指定的工作表
            Worksheet worksheet = workbook.Worksheets["2020"];

            // 設定頁首
            Aspose.Cells.PageSetup pageSetup = worksheet.PageSetup;

            // 搜尋並替換頁首中的文字
            string leftHeader = pageSetup.GetHeader(0);
            string centerHeader = pageSetup.GetHeader(1);
            string rightHeader = pageSetup.GetHeader(2);

            if (leftHeader != null && leftHeader.Contains("BYYYYMMNNN"))
            {
                leftHeader = leftHeader.Replace("BYYYYMMNNN", "B20240828");
                pageSetup.SetHeader(0, leftHeader);
            }

            if (centerHeader != null && centerHeader.Contains("BYYYYMMNNN"))
            {
                centerHeader = centerHeader.Replace("BYYYYMMNNN", "B20240828");
                pageSetup.SetHeader(1, centerHeader);
            }

            if (rightHeader != null && rightHeader.Contains("BYYYYMMNNN"))
            {
                rightHeader = rightHeader.Replace("BYYYYMMNNN", "B20240828");
                pageSetup.SetHeader(2, rightHeader);
            }

            // 儲存為新檔案
            string outFilePath = System.IO.Path.Combine(contentRootPath, "output", "Output.xlsx");
            workbook.Save(outFilePath);
            Process.Start(new ProcessStartInfo(outFilePath) { UseShellExecute = true });

            return "完成";
        }
    }
}
