using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace BioMedDocManager.Controllers
{
    [AllowAnonymous]
    public class ApprovalProcessController(
        DocControlContext _context,
        IWebHostEnvironment _hostingEnvironment,
        IParameterService _param,
        IDbLocalizer _loc
    ) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var userKey = await GetUserJwtKey("E2023007");

            // 多個 task 可堆疊在此並行
            var approvalsTask = GetMyApprovalsAsync(userKey);
            var historyTask = GetSignHistoryAsync(userKey);

            // sync semaphore
            await Task.WhenAll(approvalsTask, historyTask);

            var approvalsJson = await approvalsTask;
            var historyJson = await historyTask;

            var approvalsObj = JObject.Parse(approvalsJson);
            var historyObj = JObject.Parse(historyJson);

            var approvalsList = approvalsObj["dataList"]
                ?.ToObject<List<Dictionary<string, object>>>();

            var historyList = historyObj["dataList"]
                ?.ToObject<List<Dictionary<string, object>>>();

            ViewData["ApprovalProcessList"] = approvalsList;
            ViewData["HistoryList"] = historyList;

            return View();
        }

        public IActionResult Details()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Sign([FromRoute] string id)
        {
            JObject model;
            try
            {
                var viewerEmployeeId = "E2023011"; //尚未升級 O365 實名登入
                ViewData["ViewerEmployeeId"] = viewerEmployeeId;

                var userKey = await GetUserJwtKey(viewerEmployeeId);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sign([FromRoute] string id, SignPostModel form)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["_JSShowAlert"] = "流程不存在!";
                return RedirectToAction(nameof(Index));
            }

            if (form == null)
            {
                TempData["_JSShowAlert"] = "送出資料無效。";
                return RedirectToAction(nameof(Sign), new { id });
            }

            if (!int.TryParse(id, out var approvalInstanceId))
            {
                TempData["_JSShowAlert"] = "流程編號格式錯誤。";
                return RedirectToAction(nameof(Index));
            }

            form.ApprovalInstanceId = approvalInstanceId;

            if (string.IsNullOrWhiteSpace(form.Decision))
            {
                TempData["_JSShowAlert"] = "請選擇處理決策。";
                return RedirectToAction(nameof(Sign), new { id });
            }

            var parsedDecision = ParseDecision(form.Decision);
            if (parsedDecision == null)
            {
                TempData["_JSShowAlert"] = "無法辨識處理決策。";
                return RedirectToAction(nameof(Sign), new { id });
            }

            var apiDecision = MapDecisionToApiValue(parsedDecision.Action);
            if (string.IsNullOrWhiteSpace(apiDecision))
            {
                TempData["_JSShowAlert"] = "無法辨識簽核決策。";
                return RedirectToAction(nameof(Sign), new { id });
            }

            var comment = (form.Comment ?? string.Empty).Trim();

            if (parsedDecision.Action == "reject" && string.IsNullOrWhiteSpace(comment))
            {
                TempData["_JSShowAlert"] = "退回時請填寫簽核意見。";
                return RedirectToAction(nameof(Sign), new { id });
            }

            int? returnToSequence = null;

            if (!string.Equals(apiDecision, "agree", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(parsedDecision.Target, "terminate", StringComparison.OrdinalIgnoreCase))
                {
                    returnToSequence = 1;
                }
                else if (int.TryParse(parsedDecision.Target, out var seq))
                {
                    returnToSequence = seq;
                }

                if (!returnToSequence.HasValue)
                {
                    TempData["_JSShowAlert"] = "退回時必須指定退回關卡。";
                    return RedirectToAction(nameof(Sign), new { id });
                }
            }

            try
            {
                // 1. 先用一般查詢身分拿 instance 最新狀態
                var userKey = await GetUserJwtKey("E2023007");
                var instanceDetail = await EflowGet($"approvalInstance/{id}", userKey);
                var instanceObj = JObject.Parse(instanceDetail);

                // 2. 從最新流程中找出目前待簽核的人
                var currentSignerEmployeeId = GetCurrentSignerEmployeeId(instanceObj);
                if (string.IsNullOrWhiteSpace(currentSignerEmployeeId))
                {
                    TempData["_JSShowAlert"] = "查無目前簽核人，流程可能已完成或資料異常。";
                    return RedirectToAction(nameof(Sign), new { id });
                }

                // 3. 用目前簽核人的身分取 token
                var signingKey = await GetUserJwtKey(currentSignerEmployeeId);

                // 4. 同步更新目前關卡，避免相信前端 hidden input
                form.CurrentSequence = GetCurrentSequence(instanceObj);
                if (!form.CurrentSequence.HasValue)
                {
                    TempData["_JSShowAlert"] = "查無目前簽核關卡。";
                    return RedirectToAction(nameof(Sign), new { id });
                }

                var pythonPayload = BuildPythonDecisionPayload(
                    form: form,
                    apiDecision: apiDecision,
                    comment: comment,
                    returnToSequence: returnToSequence
                );

                var apiResult = await EflowPost("approvalProcess/sign", pythonPayload, signingKey);

                bool success = true;
                string message = "簽核已送出。";

                if (!string.IsNullOrWhiteSpace(apiResult))
                {
                    try
                    {
                        var resultObj = JObject.Parse(apiResult);
                        success = resultObj["success"]?.Value<bool>() ?? true;
                        message = resultObj["message"]?.ToString()
                                  ?? (success ? "簽核已送出。" : "簽核失敗。");
                    }
                    catch
                    {
                        // Python 若不是回 JSON，就沿用成功預設
                    }
                }

                TempData["_JSShowAlert"] = message;

                return success
                    ? RedirectToAction(nameof(Index))
                    : RedirectToAction(nameof(Sign), new { id });
            }
            catch (Exception ex)
            {
                TempData["_JSShowAlert"] = $"簽核失敗：{ex.Message}";
                return RedirectToAction(nameof(Sign), new { id });
            }
        }


        private static int? GetCurrentSequence(JObject instanceObj)
        {
            var currentProcess = GetCurrentProcess(instanceObj);
            if (currentProcess == null)
            {
                return null;
            }

            return ParseNullableInt(currentProcess["approval_process_sequence"]?.ToString());
        }

        private static string? GetCurrentSignerEmployeeId(JObject instanceObj)
        {
            var currentProcess = GetCurrentProcess(instanceObj);
            if (currentProcess == null)
            {
                return null;
            }

            // 若流程資料之後支援代理簽核，可在這裡優先切 agent id 規則
            return currentProcess["approval_process_employee_id"]?.ToString();
        }

        private static JObject? GetCurrentProcess(JObject instanceObj)
        {
            var approvalProcess = instanceObj["approval_process"] as JArray;
            if (approvalProcess == null || !approvalProcess.Any())
            {
                return null;
            }

            var processRows = approvalProcess
                .OfType<JToken>()
                .Select(x => x as JObject)
                .Where(x => x != null)
                .OrderBy(x => ParseNullableInt(x!["approval_process_sequence"]?.ToString()) ?? int.MaxValue)
                .ToList();

            return processRows.FirstOrDefault(x => !IsSignedProcess(x));
        }

        private static bool IsSignedProcess(JObject? process)
        {
            if (process == null)
            {
                return false;
            }

            var agreementToken = process["approval_process_agreement"];
            if (agreementToken != null && int.TryParse(agreementToken.ToString(), out var agreement))
            {
                return agreement == 1 || agreement == 0;
            }

            return DateTime.TryParse(process["approval_process_sign_time"]?.ToString(), out _);
        }

        private static int? ParseNullableInt(string? value)
        {
            return int.TryParse(value, out var n) ? n : null;
        }

        private static ParsedDecisionModel? ParseDecision(string? decision)
        {
            if (string.IsNullOrWhiteSpace(decision))
            {
                return null;
            }

            var parts = decision.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return null;
            }

            var action = parts[0].Trim().ToLowerInvariant();
            var target = parts[1].Trim();

            if (action != "approve" && action != "reject")
            {
                return null;
            }

            return new ParsedDecisionModel
            {
                Action = action,
                Target = target
            };
        }

        private static string? MapDecisionToApiValue(string? action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return null;
            }

            return action.ToLowerInvariant() switch
            {
                "approve" => "agree",
                "reject" => "disagree",
                _ => null
            };
        }

        private static Dictionary<string, object?> BuildPythonDecisionPayload(
            SignPostModel form,
            string apiDecision,
            string comment,
            int? returnToSequence)
        {
            var payload = new Dictionary<string, object?>
            {
                ["approval_instance_id"] = form.ApprovalInstanceId!.Value,
                ["comment"] = comment,
                ["current_sequence"] = form.CurrentSequence!.Value,
                ["decision"] = apiDecision
            };

            if (!string.Equals(apiDecision, "agree", StringComparison.OrdinalIgnoreCase))
            {
                payload["return_to_sequence"] = returnToSequence;
            }

            return payload;
        }

        public sealed class SignPostModel
        {
            public int? ApprovalInstanceId { get; set; }
            public int? CurrentSequence { get; set; }
            public int? ReturnToSequence { get; set; }
            public string? Decision { get; set; }
            public string? Comment { get; set; }
        }

        private sealed class ParsedDecisionModel
        {
            public string Action { get; set; } = string.Empty;
            public string Target { get; set; } = string.Empty;
        }
    }
}