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
    public class HomeController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 登入後與左上角的入口畫面（首頁）
        /// </summary>
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

    }
}



