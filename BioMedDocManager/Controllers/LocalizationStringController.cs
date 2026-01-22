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
    /// 多語系文字管理
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    public class LocalizationStringController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "多語系文字管理";

        /// <summary>
        /// 預設排序依據
        /// </summary>
        public const string InitSort = "LocalizationStringKey";

        /// <summary>
        /// 清單表頭設定
        /// </summary>
        public Dictionary<string, string> TableHeaders = TableHeaderFactory.Build<LocalizationString>(
            includeRowNum: true,
            onlyProps: new[]
            {
                "LocalizationStringKey", "LocalizationStringCulture", "LocalizationStringValue", "LocalizationStringCategory", "LocalizationStringIsActiveText"
            }
        );

        // ======================= Index（清單頁） =======================
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber, CancellationToken ct)
        {
            var queryModel = GetSessionQueryModel<LocalizationStringQueryViewModel>();

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

            // 語系下拉選單(從DB distinct來)

            ViewBag.CultureOptions = await BuildCultureOptionsAsync(ct);
            /*
            ViewBag.CultureOptions = await _context.LocalizationStrings
                .AsNoTracking()
                .Select(x => x.LocalizationStringCulture)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);
            */
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示清單頁");

            return await BuildQueryLocalizationString(queryModel, ct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LocalizationStringQueryViewModel queryModel)
        {
            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Create =======================
        public async Task<IActionResult> Create()
        {
            var model = new LocalizationString
            {
                LocalizationStringIsActive = true,
                CreatedAt = DateTime.Now
            };

            ViewBag.CultureOptions = await BuildCultureOptionsAsync();

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示新增頁");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LocalizationString posted)
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

                // 基本防呆：Key + Culture 不可重複（同一語系同一Key只能一筆）
                var exists = await _context.LocalizationStrings
                    .AnyAsync(x => x.LocalizationStringKey == posted.LocalizationStringKey && x.LocalizationStringCulture == posted.LocalizationStringCulture);

                if (exists)
                {
                    ModelState.AddModelError(
                        nameof(LocalizationString.LocalizationStringKey),
                        _loc.T("LocalizationString.LocalizationStringKeyCulture.Duplicate")
                    );

                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增頁儲存", "錯誤，Key+Culture重複，不可儲存");
                    return RedirectToAction(nameof(Index));
                }

                await _context.LocalizationStrings.AddAsync(posted);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("LocalizationString.Create.Title") + "-" + posted.LocalizationStringKey + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("LocalizationString.Create.Title") + "-" + posted.LocalizationStringKey + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Edit =======================
        public async Task<IActionResult> Edit([FromRoute] long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.LocalizationStrings.FirstOrDefaultAsync(x => x.LocalizationStringId == id);
            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁", "錯誤，entity為null");
                return NotFound();
            }

            ViewBag.CultureOptions = await BuildCultureOptionsAsync();

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] long? id, LocalizationString posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.LocalizationStringId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            var dbEntity = await _context.LocalizationStrings.FirstOrDefaultAsync(x => x.LocalizationStringId == id);
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

                // Key + Culture 變更時，仍需避免重複
                var exists = await _context.LocalizationStrings.AnyAsync(x =>
                    x.LocalizationStringId != dbEntity.LocalizationStringId &&
                    x.LocalizationStringKey == posted.LocalizationStringKey &&
                    x.LocalizationStringCulture == posted.LocalizationStringCulture
                );

                if (exists)
                {
                    ModelState.AddModelError(
                        nameof(LocalizationString.LocalizationStringKey),
                        _loc.T("LocalizationString.LocalizationStringKeyCulture.Duplicate")
                    );

                    await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯頁儲存", "錯誤，Key+Culture重複，不可儲存");
                    return RedirectToAction(nameof(Index));
                }

                dbEntity.LocalizationStringKey = posted.LocalizationStringKey?.Trim() ?? "";
                dbEntity.LocalizationStringCulture = posted.LocalizationStringCulture?.Trim() ?? "";
                dbEntity.LocalizationStringValue = posted.LocalizationStringValue?.Trim() ?? "";
                dbEntity.LocalizationStringCategory = posted.LocalizationStringCategory?.Trim();
                dbEntity.LocalizationStringIsActive = posted.LocalizationStringIsActive;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("LocalizationString.Edit.Title") + "-" + dbEntity.LocalizationStringKey + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "更新【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("LocalizationString.Edit.Title") + "-" + dbEntity.LocalizationStringKey + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Details =======================
        public async Task<IActionResult> Details([FromRoute] long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.LocalizationStrings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.LocalizationStringId == id);

            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示明細頁", "錯誤，entity為null");
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示詳細資料");

            return View(entity);
        }

        // ======================= Delete（不檢查關聯，直接刪） =======================
        public async Task<IActionResult> Delete([FromRoute] long? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁", "錯誤，id小於等於0");
                return NotFound();
            }

            var entity = await _context.LocalizationStrings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.LocalizationStringId == id);

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
        public async Task<IActionResult> Delete([FromRoute] long? id, LocalizationString posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.LocalizationStringId)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除頁儲存", "錯誤，posted為null 或 id小於等於0 或 id與posted.id不符");
                return NotFound();
            }

            var entity = await _context.LocalizationStrings.FirstOrDefaultAsync(x => x.LocalizationStringId == posted.LocalizationStringId);
            if (entity == null)
            {
                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除頁儲存", "錯誤，entity為null");
                return NotFound();
            }

            try
            {
                _context.LocalizationStrings.Remove(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = _loc.T("LocalizationString.Delete.Title") + "-" + entity.LocalizationStringKey + _loc.T("Common.Failed");
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = _loc.T("LocalizationString.Delete.Title") + "-" + entity.LocalizationStringKey + _loc.T("Common.Success");

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= 查詢邏輯 =======================
        [NonAction]
        public async Task<IActionResult> BuildQueryLocalizationString(LocalizationStringQueryViewModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            FilterOrderBy(queryModel, TableHeaders, InitSort);

            IQueryable<LocalizationString> q = _context.LocalizationStrings.AsNoTracking();

            // Key
            if (!string.IsNullOrWhiteSpace(queryModel.LocalizationStringKey))
            {
                var s = $"%{queryModel.LocalizationStringKey.Trim()}%";
                q = q.Where(x => EF.Functions.Like(x.LocalizationStringKey, s));
            }

            // Culture
            if (!string.IsNullOrWhiteSpace(queryModel.LocalizationStringCulture))
            {
                var s = $"%{queryModel.LocalizationStringCulture.Trim()}%";
                q = q.Where(x => EF.Functions.Like(x.LocalizationStringCulture, s));
            }

            // Value
            if (!string.IsNullOrWhiteSpace(queryModel.LocalizationStringValue))
            {
                var s = $"%{queryModel.LocalizationStringValue.Trim()}%";
                q = q.Where(x => EF.Functions.Like(x.LocalizationStringValue, s));
            }

            // Category
            if (!string.IsNullOrWhiteSpace(queryModel.LocalizationStringCategory))
            {
                var s = $"%{queryModel.LocalizationStringCategory.Trim()}%";
                q = q.Where(x => EF.Functions.Like(x.LocalizationStringCategory!, s));
            }

            // IsActive
            if (queryModel.LocalizationStringIsActive.HasValue)
            {
                q = q.Where(x => x.LocalizationStringIsActive == queryModel.LocalizationStringIsActive.Value);
            }

            q = q.OrderByWhitelist(
                queryModel.OrderBy,
                queryModel.SortDir,
                TableHeaders,
                tiebreakerProperty: "LocalizationStringId"
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
                payloadProps: new[] { "LocalizationStringId" }
            );

            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }

        /// <summary>
        /// 語系下拉式選單
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        [NonAction]
        private async Task<List<string>> BuildCultureOptionsAsync(CancellationToken ct = default)
        {
            return await _context.LocalizationStrings
                .AsNoTracking()
                .Select(x => x.LocalizationStringCulture)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);
        }


    }
}
