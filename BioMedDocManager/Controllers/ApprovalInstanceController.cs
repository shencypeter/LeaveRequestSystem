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
            /*{"page":1,"page_size":10,"pageSize":10,"total":43,"pending":6,"returned":0,"dataList":[{"form_type":"請假單","form_id":"64","approval_instance_status":"已完成","approval_instance_current_revision":3,"approval_instance_completed_at":"2026-02-12T11:08:57","created_at":"2026-02-12T11:00:50","created_by":"E2023007","deleted_at":null,"approval_instance_id":65,"employee_id":"E2023007","approval_instance_current_sequence":9,"approval_instance_submitted_at":"2026-02-12T11:00:50","approval_instance_step_islock":0,"updated_at":"2026-02-12T11:08:57","updated_by":"E2023001","deleted_by":null,"employee_name":"吳七妤","approval_instance_current_step_name":null,"approval_instance_current_sign_user_name":null,"approval_instance_sign_path":"✓ 👤吳七妤(您) ➜ ✓ 周六宇 ➜ ✓ 張八豪 ➜ ✓ 王三豪 ➜ ✓ 林二萱 ➜ ✓ 林二萱 ➜ ✓ 黃十一 ➜ ✓ 陳一偉"},{"form_type":"請假單","form_id":"0212A","approval_instance_status":"簽核中","approval_instance_current_revision":1,"approval_instance_completed_at":null,"created_at":"2026-02-12T10:51:32","created_by":"E2023007","deleted_at":null,"approval_instance_id":64,"employee_id":"E2023007","approval_instance_current_sequence":2,"approval_instance_submitted_at":"2026-02-12T10:51:32","approval_instance_step_islock":0,"updated_at":"2026-02-12T10:53:01","updated_by":"E2023003","deleted_by":null,"employee_name":"吳七妤","approval_instance_current_step_name":"其他經辦","approval_instance_current_sign_user_name":"周六宇","approval_instance_sign_path":"✓ 👤吳七妤(您) ➜ 【● 周六宇 · ○ 張八豪】 ➜ ○ 王三豪 ➜ ○ 人資部 ➜ ○ 陳一偉"},{"form_type":"請假單","form_id":"63","approval_instance_status":"已完成","approval_instance_current_revision":2,"approval_instance_completed_at":"2026-02-12T09:57:36","created_at":"2026-02-12T09:52:41","created_by":"E2023007","deleted_at":null,"approval_instance_id":63,"employee_id":"E2023007","approval_instance_current_sequence":9,"approval_instance_submitted_at":"2026-02-12T09:52:41","approval_instance_step_islock":0,"updated_at":"2026-02-12T09:57:36","updated_by":"E2023001","deleted_by":null,"employee_name":"吳七妤","approval_instance_current_step_name":null,"approval_instance_current_sign_user_name":null,"approval_instance_sign_path":"✓ 👤吳七妤(您) ➜ ✓ 周六宇 ➜ ✓ 張八豪 ➜ ✓ 王三豪 ➜ ✓ 黃十一 ➜ ✓ 林二萱 ➜ ✓ 黃十一 ➜ ✓ 陳一偉"},{"form_type":"請假單","form_id":"62","approval_instance_status":"簽核中","approval_instance_current_revision":1,"approval_instance_completed_at":null,"created_at":"2026-02-11T14:54:22","created_by":"E2023007","deleted_at":null,"approval_instance_id":62,"employee_id":"E2023007","approval_instance_current_sequence":5,"approval_instance_submitted_at":"2026-02-11T14:54:22","approval_instance_step_islock":0,"updated_at":"2026-02-11T14:56:17","updated_by":"E2023003","deleted_by":null,"employee_name":"吳七妤","approval_instance_current_step_name":"會辦部門","approval_instance_current_sign_user_name":"黃十一","approval_instance_sign_path":"✓ 👤吳七妤(您) ➜ ✓ 周六宇 ➜ ✓ 張八豪 ➜ ✓ 王三豪 ➜ ● 黃十一 ➜ ○ 林二萱 ➜ ○ 黃十一 ➜ ○ 陳一偉"},{"form_type":"請假單","form_id":"60","approval_instance_status":"已完成","approval_instance_current_revision":2,"approval_instance_completed_at":"2026-02-11T14:39:34","created_at":"2026-02-11T14:32:30","created_by":"E2023007","deleted_at":null,"approval_instance_id":60,"employee_id":"E2023007","approval_instance_current_sequence":9,"approval_instance_submitted_at":"2026-02-11T14:32:30","approval_instance_step_islock":0,"updated_at":"2026-02-11T14:39:34","updated_by":"E2023001","deleted_by":null,"employee_name":"吳七妤","approval_instance_current_step_name":null,"approval_instance_current_sign_user_name":null,"approval_instance_sign_path":"✓ 👤吳七妤(您) ➜ ✓ 周六宇 ➜ ✓ 張八豪 ➜ ✓ 王三豪 ➜ ✓ 黃十一 ➜ ✓ 林二萱 ➜ ✓ 黃十一 ➜ ✓ 陳一偉"},{"form_type":"請假單","form_id":"0211-DelegateTest-2","approval_instance_status":"簽核中","approval_instance_current_revision":0,"approval_instance_completed_at":null,"created_at":"2026-02-11T13:52:57","created_by":"E2023007","deleted_at":null,"approval_instance_id":59,"employee_id":"E2023007","approval_instance_current_sequence":6,"approval_instance_submitted_at":"2026-02-11T13:52:57","approval_instance_step_islock":0,"updated_at":"2026-02-11T13:54:19","updated_by":"E2023011","deleted_by":null,"employee_name":"吳七妤","approval_instance_current_step_name":"會辦部門","approval_instance_current_sign_user_name":"黃十一","approval_instance_sign_path":"✓ 👤吳七妤(您) ➜ 【○ 周六宇 · ✓ 張八豪】 ➜ ✓ 王三豪 ➜ ✓ 黃十一 ➜ ● 黃十一 ➜ ○ 林二萱 ➜ ○ 陳一偉"},{"form_type":"請假單","form_id":"0211-DelegateTest","approval_instance_status":"簽核中","approval_instance_current_revision":0,"approval_instance_completed_at":null,"created_at":"2026-02-11T13:26:03","created_by":"E2023007","deleted_at":null,"approval_instance_id":58,"employee_id":"E2023007","approval_instance_current_sequence":5,"approval_instance_submitted_at":"2026-02-11T13:26:03","approval_instance_step_islock":0,"updated_at":"2026-02-11T13:27:14","updated_by":"E2023011","deleted_by":null,"employee_name":"吳七妤","approval_instance_current_step_name":"會辦部門","approval_instance_current_sign_user_name":"林二萱","approval_instance_sign_path":"✓ 👤吳七妤(您) ➜ 【○ 周六宇 · ✓ 張八豪】 ➜ ✓ 王三豪 ➜ ✓ 黃十一 ➜ ● 林二萱 ➜ ● 黃十一 ➜ ○ 陳一偉"},{"form_type":"請假單","form_id":"57","approval_instance_status":"簽核中","approval_instance_current_revision":4,"approval_instance_completed_at":null,"created_at":"2026-02-09T15:04:48","created_by":"E2023007","deleted_at":null,"approval_instance_id":57,"employee_id":"E2023007","approval_instance_current_sequence":6,"approval_instance_submitted_at":"2026-02-09T15:04:48","approval_instance_step_islock":0,"updated_at":"2026-02-09T15:13:27","updated_by":"E2023002","deleted_by":null,"employee_name":"吳七妤","approval_instance_current_step_name":"陳核","approval_instance_current_sign_user_name":"陳一偉","approval_instance_sign_path":"✓ 👤吳七妤(您) ➜ ✓ 周六宇 ➜ ✓ 張八豪 ➜ ✓ 王三豪 ➜ ✓ 黃十一 ➜ ✓ 林二萱 ➜ ○ 黃十一 ➜ ● 陳一偉"},{"form_type":"請假單","form_id":"56","approval_instance_status":"簽核中","approval_instance_current_revision":2,"approval_instance_completed_at":null,"created_at":"2026-02-03T15:18:09","created_by":"E2023007","deleted_at":null,"approval_instance_id":56,"employee_id":"E2023007","approval_instance_current_sequence":2,"approval_instance_submitted_at":"2026-02-03T15:18:09","approval_instance_step_islock":0,"updated_at":"2026-02-03T15:20:05","updated_by":"E2023008","deleted_by":null,"employee_name":"吳七妤","approval_instance_current_step_name":"其他經辦","approval_instance_current_sign_user_name":"周六宇","approval_instance_sign_path":"✓ 👤吳七妤(您) ➜ ● 周六宇 ➜ ○ 張八豪 ➜ ○ 王三豪 ➜ ○ 人資部 ➜ ○ 陳一偉"},{"form_type":"請假單","form_id":"55","approval_instance_status":"已完成","approval_instance_current_revision":3,"approval_instance_completed_at":"2026-02-03T15:14:14","created_at":"2026-02-03T15:08:54","created_by":"E2023007","deleted_at":null,"approval_instance_id":55,"employee_id":"E2023007","approval_instance_current_sequence":20,"approval_instance_submitted_at":"2026-02-03T15:08:54","approval_instance_step_islock":0,"updated_at":"2026-02-03T15:14:14","updated_by":"E2023001","deleted_by":null,"employee_name":"吳七妤","approval_instance_current_step_name":null,"approval_instance_current_sign_user_name":null,"approval_instance_sign_path":"✓ 👤吳七妤(您) ➜ 【✓ 周六宇 · ○ 張八豪】 ➜ ✓ 王三豪 ➜ ✓ 黃十一 ➜ ✓ 林二萱 ➜ ✓ 黃十一 ➜ ✓ 陳一偉"}]}*/



            var historyObj = JObject.Parse(historyJson);



            var historyList = historyObj["dataList"]
                ?.ToObject<List<Dictionary<string, object>>>();

            ViewData["HistoryList"] = historyList;

            return View();
        }



        public async Task< IActionResult> Details()
        {

            var userKey = await GetUserJwtKey("E2023007");

            var instanceDetail = await EflowGet("approvalInstance/65", userKey);

            JObject model;
            try
            {
                model = JObject.Parse(instanceDetail);
            }
            catch
            {
                model = new JObject();
            }

            return View(model);

        }


    }
}



