using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace BioMedDocManager.Controllers
{
    class PythonPagination
    {
        string sort { get; set; }
        string order { get; set; }
        int page_size { get; set; } = 10;
        int page { get; set; } = 1;
    }

    [AllowAnonymous]
    public class ApprovalInstanceController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 登入後與左上角的入口畫面（首頁）
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Index([FromRoute] string id = "")
        {
            var userKey = await GetUserJwtKey("E2023007");


            id = id switch
            {
                "已完成" or "簽核中" => id,
                _ => ""
            };

            var url = $"approvalInstance/?page=1&sort=approval_instance_submitted_at&order=desc&page_size=10&status={id}";


            var historyJson = await EflowGet(url, userKey);

            var historyObj = JObject.Parse(historyJson);

            var historyList = historyObj["dataList"]
                ?.ToObject<List<Dictionary<string, object>>>();

            ViewData["HistoryList"] = historyList;

            return View();
        }

        public async Task<IActionResult> Details([FromRoute] string id)
        {
            // 👇 bounce immediately
            return RedirectToAction("Sign", "ApprovalProcess", new { id });


            try
            {
      

           
            }
            catch
            {
                TempData["_JSShowAlert"] = "流程不存在!";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}



