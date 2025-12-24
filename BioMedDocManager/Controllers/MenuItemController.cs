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
    /// 選單項目管理
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    
    public class MenuItemController(DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param, IDbLocalizer _loc) : BaseController(_context, _hostingEnvironment, _param, _loc)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "選單項目管理";

        /// <summary>
        /// 預設排序依據 (不需要，選單頁就是依照選單順序顯示)
        /// </summary>
        //public const string InitSort = "MenuItemDisplayOrder";

        /// <summary>
        /// 清單表頭設定
        /// </summary>
        public Dictionary<string, string> TableHeaders = TableHeaderFactory.Build<MenuItem>(
            includeRowNum: false,
            onlyProps: new[]
            {
                "MenuItemTitle",
                "MenuItemIcon",
                "ResourceKey",
                "MenuItemDisplayOrder",
                "MenuItemIsActiveText",
                "CreatedAt",
                "UpdatedAt"
            }
        );

        // ======================= Index（清單頁） =======================        
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber, CancellationToken ct)
        {
            var queryModel = GetSessionQueryModel<MenuItemQueryViewModel>();

            if (PageSize.HasValue)
            {
                queryModel.PageSize = PageSize.Value;
            }
            if (PageNumber.HasValue)
            {
                queryModel.PageNumber = PageNumber.Value;
            }

            queryModel.PageSize = 50;// 固定50筆
            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示清單頁");

            return await BuildQueryMenuItem(queryModel, ct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(MenuItemQueryViewModel queryModel)
        {
            QueryableExtensions.TrimStringProperties(queryModel);
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "清單頁送出查詢");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Create =======================       
        public async Task<IActionResult> Create()
        {
            var model = new MenuItem
            {
                MenuItemIsActive = true,
                MenuItemDisplayOrder = 0,
                CreatedAt = DateTime.Now
            };

            ViewBag.ParentMenuItems = await _context.MenuItems.Where(m => m.MenuItemParentId == null && m.DeletedAt == null).OrderBy(m => m.MenuItemTitle).ToListAsync();
            ViewBag.Resources = await _context.Resources.OrderBy(r => r.ResourceKey).ToListAsync();

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示新增頁");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItem posted)
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

                await _context.MenuItems.AddAsync(posted);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"選單-{posted.MenuItemTitle} 新增【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"選單-{posted.MenuItemTitle} 新增成功";
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "新增成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Edit =======================
        public async Task<IActionResult> Edit([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (entity == null)
            {
                return NotFound();
            }

            ViewBag.ParentMenuItems = await _context.MenuItems.Where(m => m.MenuItemParentId == null && m.DeletedAt == null).OrderBy(m => m.MenuItemTitle).ToListAsync();
            ViewBag.Resources = await _context.Resources.OrderBy(r => r.ResourceKey).ToListAsync();

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示編輯頁");

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int? id, MenuItem posted)
        {
            if (posted == null || id != posted.MenuItemId)
            {
                return NotFound();
            }

            QueryableExtensions.TrimStringProperties(posted);

            var dbEntity = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (dbEntity == null)
            {
                return NotFound();
            }

            try
            {
                dbEntity.MenuItemParentId = posted.MenuItemParentId;
                dbEntity.MenuItemTitle = posted.MenuItemTitle?.Trim() ?? string.Empty;
                dbEntity.MenuItemIcon = posted.MenuItemIcon?.Trim();
                dbEntity.MenuItemDisplayOrder = posted.MenuItemDisplayOrder;
                dbEntity.MenuItemIsActive = posted.MenuItemIsActive;
                dbEntity.ResourceId = posted.ResourceId;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"選單-{dbEntity.MenuItemTitle} 更新【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "更新【失敗】", msg, true);
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"選單-{dbEntity.MenuItemTitle} 更新成功";
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "編輯成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= Details =======================
        public async Task<IActionResult> Details([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await _context.MenuItems
                .Include(m => m.Parent)
                .Include(m => m.Resource)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (entity == null)
            {
                return NotFound();
            }

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示詳細資料");

            return View(entity);
        }

        // ======================= Delete =======================
        public async Task<IActionResult> Delete([FromRoute] int? id)
        {
            if (id.GetValueOrDefault() <= 0)
            {
                return NotFound();
            }

            var entity = await _context.MenuItems
                .Include(m => m.Parent)
                .Include(m => m.Resource)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (entity == null)
            {
                return NotFound();
            }

            // 把底下子選單撈出來（含 Resource，方便顯示 ResourceKey）
            var children = await _context.MenuItems
                .Where(m => m.MenuItemParentId == entity.MenuItemId)
                .Include(m => m.Resource)
                .AsNoTracking()
                .OrderBy(m => m.MenuItemDisplayOrder)
                .ThenBy(m => m.MenuItemTitle)
                .ToListAsync();

            bool hasChildren = children.Count > 0;

            ViewBag.HasChildren = hasChildren;
            ViewBag.ChildrenCount = children.Count;
            ViewBag.ChildrenList = children;

            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示刪除頁");

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromRoute] int? id, MenuItem posted)
        {
            if (posted == null || id.GetValueOrDefault() <= 0 || id != posted.MenuItemId)
            {
                return NotFound();
            }

            var entity = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.MenuItemId == posted.MenuItemId);

            if (entity == null)
            {
                return NotFound();
            }

            try
            {
                var hasChildren = await _context.MenuItems
                    .AnyAsync(m => m.MenuItemParentId == entity.MenuItemId);

                if (hasChildren)
                {
                    var msg = $"選單-{entity.MenuItemTitle} 目前仍有子選單，無法刪除。";
                    TempData["_JSShowAlert"] = msg;

                    await _accessLog.NewActionAsync(
                        GetLoginUser(),
                        PageName,
                        "刪除【失敗-仍有子選單】",
                        msg,
                        true
                    );

                    return RedirectToAction(nameof(Index));
                }

                _context.MenuItems.Remove(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = $"選單-{entity.MenuItemTitle} 刪除【失敗】";
                Utilities.WriteExceptionIntoLogFile(msg, ex, HttpContext);
                TempData["_JSShowAlert"] = msg;

                await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除【失敗】", msg, true);

                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = $"選單-{entity.MenuItemTitle} 已刪除";
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "刪除成功");

            return RedirectToAction(nameof(Index));
        }

        // ======================= 查詢邏輯 =======================
        [NonAction]
        public async Task<IActionResult> BuildQueryMenuItem(MenuItemQueryViewModel queryModel, CancellationToken ct)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            // 這裡還是保留，主要是維持 QueryModel / 隱藏欄位等一致性
            FilterOrderBy(queryModel, TableHeaders, null);

            // 基礎查詢（含 Resource）
            IQueryable<MenuItem> q = _context.MenuItems
                .Include(m => m.Resource)
                .AsNoTracking();

            // ===== 條件過濾 =====

            // 選單標題
            if (!string.IsNullOrWhiteSpace(queryModel.MenuItemTitle))
            {
                var s = $"%{queryModel.MenuItemTitle.Trim()}%";
                q = q.Where(m => EF.Functions.Like(m.MenuItemTitle, s));
            }

            // ResourceKey（原本的 MenuItemUrl 改成查 Resource.ResourceKey）
            if (!string.IsNullOrWhiteSpace(queryModel.ResourceKey))
            {
                var s = $"%{queryModel.ResourceKey.Trim()}%";
                q = q.Where(m => m.Resource != null && EF.Functions.Like(m.Resource.ResourceKey!, s));
            }

            // 是否啟用
            if (queryModel.MenuItemIsActive.HasValue)
            {
                q = q.Where(m => m.MenuItemIsActive == queryModel.MenuItemIsActive.Value);
            }

            // ===== 樹狀排序：父層依自己的 DisplayOrder，子層跟在各自父層後面 =====
            q =
                from m in q
                join p in _context.MenuItems.AsNoTracking()
                    on m.MenuItemParentId equals p.MenuItemId into parentJoin
                from parent in parentJoin.DefaultIfEmpty()
                orderby
                    // 1) 父群組排序 key：父層 → 用自己的 DisplayOrder；子層 → 用父層的 DisplayOrder
                    (parent == null ? m.MenuItemDisplayOrder : parent.MenuItemDisplayOrder),
                    // 2) 父群組內固定順序：用父層的 Id（父層自己就用自己的 Id）
                    (parent == null ? m.MenuItemId : parent.MenuItemId),
                    // 3) 同一群組中：父(0) 在前、子(1) 在後
                    (m.MenuItemParentId == null ? 0 : 1),
                    // 4) 同一層中的顯示順序（子選單之間）
                    m.MenuItemDisplayOrder,
                    // 5) 穩定排序，避免同 DisplayOrder 亂跳
                    m.MenuItemId
                select m;

            var (entities, totalCount) =
                await q.PaginateWithCountAsync(queryModel.PageNumber, queryModel.PageSize, ct);

            var result = BuildRows(
                entities: entities,
                tableHeaders: TableHeaders,
                pageNumber: queryModel.PageNumber,
                pageSize: queryModel.PageSize,
                keyMode: KeyMode.PropertyName,
                includeRowNum: true,
                payloadProps: new[] { "MenuItemId", "MenuItemParentId" }
            );

            ViewData["totalCount"] = totalCount;
            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }



    }
}
