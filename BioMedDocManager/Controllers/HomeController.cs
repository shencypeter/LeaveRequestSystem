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
    [Route("[controller]")]
    public class HomeController(ILogger<HomeController> logger, DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IParameterService _param) : BaseController(_context, _hostingEnvironment, _param)
    {

        /// <summary>
        /// 登入後與左上角的入口畫面（首頁）
        /// </summary>
        [HttpGet("")]           // /Home
        [HttpGet("Index")]      // /Home/Index
        [HttpGet("/")]          // 根路徑 /
        [HttpGet("/Welcome")]   // /Welcome
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

    }
}
