using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 首頁
    /// </summary>
    /// <param name="logger">log紀錄器</param>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>    
    public class IframeLoaderController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {

        [AllowAnonymous]
        public IActionResult Index(string src = "/leaveform")
        {
            TempData["src"] = src;

            var documentControl = new[]
            {
            "CDocumentClaim",
            "CDocumentClaimReserve",
            "CFileQuery",
            "CDocumentCancel",
            "COldDocCtrlMaintables",
            "CIssueTables",
            "CFormQuery",
            "CDocumentControl",
            "CBatchStorage",
            "CManagementSettings"
        };

            // Check if the src contains any of the doc control controllers
            bool isDocControl = documentControl.Any(controller => src.Contains(controller, StringComparison.OrdinalIgnoreCase));

            TempData["Menu"] = isDocControl ? "Document" : "Purchase";

            return View();
        }

       

    }
}



