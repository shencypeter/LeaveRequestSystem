using BioMedDocManager.Extensions;
using BioMedDocManager.Factory;
using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 系統參數管理
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>   
    public class ParameterController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "系統參數管理";

        /// <summary>
        /// 預設排序依據
        /// </summary>
        public const string InitSort = "ParameterCode";

        /// <summary>
        /// 清單表頭設定
        /// </summary>
        public Dictionary<string, string> TableHeaders = TableHeaderFactory.Build<Parameter>(
            includeRowNum: true,
            onlyProps: new[]
            {
                "ParameterCode", "ParameterName", "ParameterFormat", "ParameterValue", "ParameterIsActiveText"
            }
        );

        // ======================= Index（清單頁） =======================
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber, CancellationToken ct)
        {
            var queryModel = GetSessionQueryModel<ParameterQueryViewModel>();

            if (PageSize.HasValue)
            {
                queryModel.PageSize = PageSize.Value;
            }
            if (PageNumber.HasValue)
            {
                queryModel.PageNumber = PageNumber.Value;
            }

            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示清單頁");

            return await BuildQueryParameter(queryModel, ct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ParameterQueryViewModel queryModel)
        {
            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Create =======================
        public async Task<IActionResult> Create()
        {
            var model = new Parameter
            {
                ParameterIsActive = true,
                CreatedAt = DateTime.Now
            };

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示新增頁");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Parameter posted)
        {
            if (posted == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", "錯誤，posted為null");
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            try
            {
                if (!ModelState.IsValid)
                {
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", "錯誤，必填資料未填寫");
                    return RedirectToAction(nameof(Index));
                }

                // 基本防呆：Code 不可重複
                var exists = await _context.Parameters.AnyAsync(p => p.ParameterCode == posted.ParameterCode);
                if (exists)
                {
                    ModelState.AddModelError(
                        nameof(Parameter.ParameterCode),
                        _loc.T("Parameter.ParameterCode.Duplicate")
                    );

                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", "錯誤，Parameter Code重複，不可儲存");

                    return RedirectToAction(nameof(Index));
                }

                await _context.Parameters.AddAsync(posted);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("Parameter.Create.Title") + "-" + posted.ParameterCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("Parameter.Create.Title") + "-" + posted.ParameterCode + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Edit =======================
        public async Task<IActionResult> Edit([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Parameters.FirstOrDefaultAsync(p => p.ParameterId == id);
            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，entity為null");
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute]  long? id, Parameter posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.ParameterId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            var dbEntity = await _context.Parameters.FirstOrDefaultAsync(p => p.ParameterId == id);
            if (dbEntity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，dbEntity為null");
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，必填資料未填寫");
                    return RedirectToAction(nameof(Index));
                }

                // Code 變更時，仍需避免重複
                var exists = await _context.Parameters.AnyAsync(p => p.ParameterId != dbEntity.ParameterId && p.ParameterCode == posted.ParameterCode);
                if (exists)
                {
                    ModelState.AddModelError(
                        nameof(Parameter.ParameterCode),
                        _loc.T("Parameter.ParameterCode.Duplicate")
                    );

                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，Parameter Code重複，不可儲存");

                    return RedirectToAction(nameof(Index));
                }

                dbEntity.ParameterCode = posted.ParameterCode?.Trim() ?? "";
                dbEntity.ParameterFormat = posted.ParameterFormat;
                dbEntity.ParameterName = posted.ParameterName;
                dbEntity.ParameterValue = posted.ParameterValue; // 允許空白/JSON/HTML                
                dbEntity.ParameterIsActive = posted.ParameterIsActive;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("Parameter.Edit.Title") + "-" + dbEntity.ParameterCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "更新【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("Parameter.Edit.Title") + "-" + dbEntity.ParameterCode + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Details =======================
        public async Task<IActionResult> Details([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Parameters
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParameterId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁", "錯誤，entity為null");
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示詳細資料");

            return View(entity);
        }

        // ======================= Delete（不檢查關聯，直接刪） =======================
        public async Task<IActionResult> Delete([FromRoute]  long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.Parameters
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParameterId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁", "錯誤，entity為null");
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁");

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromRoute]  long? id, Parameter posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.ParameterId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            var entity = await _context.Parameters.FirstOrDefaultAsync(p => p.ParameterId == posted.ParameterId);
            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除頁儲存", "錯誤，entity為null");
                return NotFound();
            }

            try
            {
                _context.Parameters.Remove(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("Parameter.Delete.Title") + "-" + entity.ParameterCode + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("Parameter.Delete.Title") + "-" + entity.ParameterCode + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= 查詢邏輯 =======================
        [NonAction]
        public async Task<IActionResult> BuildQueryParameter(ParameterQueryViewModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            FilterOrderBy(queryModel, TableHeaders, InitSort);

            IQueryable<Parameter> q = _context.Parameters.AsNoTracking();

            // ParameterCode
            if (!string.IsNullOrWhiteSpace(queryModel.ParameterCode))
            {
                var s = $"%{queryModel.ParameterCode.Trim()}%";
                q = q.Where(p => EF.Functions.Like(p.ParameterCode, s));
            }

            // ParameterName
            if (!string.IsNullOrWhiteSpace(queryModel.ParameterName))
            {
                var s = $"%{queryModel.ParameterName.Trim()}%";
                q = q.Where(p => EF.Functions.Like(p.ParameterName, s));
            }

            // ParameterFormat
            if (!string.IsNullOrWhiteSpace(queryModel.ParameterFormat))
            {
                var s = $"%{queryModel.ParameterFormat.Trim()}%";
                q = q.Where(p => EF.Functions.Like(p.ParameterFormat, s));
            }

            // ParameterIsActive
            if (queryModel.ParameterIsActive.HasValue)
            {
                q = q.Where(p => p.ParameterIsActive == queryModel.ParameterIsActive.Value);
            }

            q = q.OrderByWhitelist(
                queryModel.OrderBy,
                queryModel.SortDir,
                TableHeaders,
                tiebreakerProperty: "ParameterId"
            );

            var (entities, totalCount) =
                await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

            // 讓 NotMapped 計算屬性可以用多語系 Loc.T(...)
            entities.WithLoc(_loc);

            var result = BuildRows(
                entities: entities,
                tableHeaders: TableHeaders,
                pageNumber: queryModel.PageNumber,
                pageSize: queryModel.PageSize,
                keyMode: KeyMode.PropertyName,
                includeRowNum: true,
                payloadProps: new[] { "ParameterId" }
            );

            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }




    }
}
