using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BioMedDocManager.Controllers
{

    /// <summary>
    /// 錯誤處理控制器
    /// </summary>
    /// <param name="logger">log紀錄器</param>
    /// <param name="context">資料庫查詢物件</param>
    [AllowAnonymous]
    public class ErrorController(ILogger<HomeController> logger, DocControlContext context, IWebHostEnvironment hostingEnvironment) : BaseController(context, hostingEnvironment)
    {

        [Route("Error/{statusCode?}")]
        public IActionResult Index(int? statusCode)
        {
            var sc = statusCode ?? HttpContext.Response?.StatusCode ?? 500;

            // 請求正確，但是誤入Error網址，回去首頁
            if (sc == 200)
            {
                return RedirectToAction("Index", "Home");
            }

            Response.StatusCode = sc; // 讓回應碼正確
            var vm = ErrorViewModel.FromStatusCode(sc);
            return View("_Error", vm);
        }
    }

}
