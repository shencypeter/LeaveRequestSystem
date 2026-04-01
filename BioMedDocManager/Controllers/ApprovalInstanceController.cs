using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace BioMedDocManager.Controllers
{
    [AllowAnonymous]
    public class ApprovalInstanceController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 登入後與左上角的入口畫面（首頁）
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var userKey = await GetUserJwtKey("E2023007");
            var historyTask = GetSignHistoryAsync(userKey);

           

            //sync semaphor
            await Task.WhenAll(historyTask);

   
            var historyJson = await historyTask;
           



            var historyObj = JObject.Parse(historyJson);



            var historyList = historyObj["dataList"]
                ?.ToObject<List<Dictionary<string, object>>>();

            ViewData["HistoryList"] = historyList;

            return View();
        }


        public async Task< IActionResult> Details([FromRoute] string id)
        {
            JObject model;
            try
            {
                var userKey = await GetUserJwtKey("E2023007");

                var instanceDetail = await EflowGet($"approvalInstance/{id}", userKey);

                model = JObject.Parse(instanceDetail);
            }
            catch
            {
                model = [];

                TempData["_JSShowAlert"] = "流程不存在!";
                return RedirectToAction(nameof(Index));
            }

            return View(model ?? []);

        }


    }
}



