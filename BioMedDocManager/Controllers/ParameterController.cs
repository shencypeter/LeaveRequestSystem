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
    [Route("[controller]")]
    public class ParameterController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param) : BaseController(_context, _hostingEnvironment, _param)
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

        [HttpGet("")]
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

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ParameterQueryViewModel queryModel)
        {
            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Create =======================

        [HttpGet("Create")]
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

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Parameter posted)
        {
            if (posted == null)
            {
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(posted);
                }

                // 基本防呆：Code 不可重複
                var exists = await _context.Parameters.AnyAsync(p => p.ParameterCode == posted.ParameterCode);
                if (exists)
                {
                    ModelState.AddModelError(nameof(Parameter.ParameterCode), "參數代碼已存在，請更換。");
                    return View(posted);
                }

                await _context.Parameters.AddAsync(posted);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"系統參數-{posted.ParameterCode} 新增【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"系統參數-{posted.ParameterCode} 新增成功";
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Edit =======================

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await _context.Parameters.FirstOrDefaultAsync(p => p.ParameterId == id);
            if (entity == null)
            {
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");
            return View(entity);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int? id, Parameter posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.ParameterId)
            {
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            var dbEntity = await _context.Parameters.FirstOrDefaultAsync(p => p.ParameterId == id);
            if (dbEntity == null)
            {
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(posted);
                }

                // Code 變更時，仍需避免重複
                var exists = await _context.Parameters.AnyAsync(p => p.ParameterId != dbEntity.ParameterId && p.ParameterCode == posted.ParameterCode);
                if (exists)
                {
                    ModelState.AddModelError(nameof(Parameter.ParameterCode), "參數代碼已存在，請更換。");
                    return View(posted);
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
                var msg = $"系統參數管理-更新【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "更新【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"系統參數管理-更新成功";
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Details =======================

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await _context.Parameters
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParameterId == id);

            if (entity == null)
            {
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示詳細資料");

            return View(entity);
        }

        // ======================= Delete（不檢查關聯，直接刪） =======================

        [HttpGet("Delete/{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await _context.Parameters
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParameterId == id);

            if (entity == null)
            {
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁");

            return View(entity);
        }

        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromRoute] int? id, Parameter posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.ParameterId)
            {
                return NotFound();
            }

            var entity = await _context.Parameters.FirstOrDefaultAsync(p => p.ParameterId == posted.ParameterId);
            if (entity == null)
            {
                return NotFound();
            }

            try
            {
                _context.Parameters.Remove(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"系統參數-{entity.ParameterCode} 刪除【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"系統參數-{entity.ParameterCode} 已刪除";
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

            // Soft delete（如果你有全域 QueryFilter 可移除這段）
            q = q.Where(p => p.DeletedAt == null);

            q = q.OrderByWhitelist(
                queryModel.OrderBy,
                queryModel.SortDir,
                TableHeaders,
                tiebreakerProperty: "ParameterId"
            );

            var (entities, totalCount) =
                await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

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
