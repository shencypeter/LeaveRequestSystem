using BioMedDocManager.Extensions;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace BioMedDocManager.Controllers
{

    /// <summary>
    /// 文管-查詢樹狀控制器
    /// </summary>
    /// <param name="logger">log紀錄器</param>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    /// <param name="accessLog">紀錄連線Log</param>
    [Route("[controller]")]
    public class TreeController(ILogger<HomeController> logger, DocControlContext _context, IWebHostEnvironment _hostingEnvironment, IAccessLogService _accessLog, IParameterService _param) : BaseController(_context, _hostingEnvironment, _param)
    {
        /// <summary>
        /// 頁面名稱
        /// </summary>
        public const string PageName = "資源管理";

        /// <summary>
        /// double click tree 版次階層加上表單發行日期 只抓最新一筆version
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示最新版本的查詢樹");
            return View();
        }

        /// <summary>
        /// 抓最新版本的查詢樹
        /// </summary>
        /// <param name="date">資料</param>
        /// <param name="search">關鍵字</param>
        /// <returns>json資料物件</returns>
        [HttpGet("GetTreeDataVerLatest")]
        public JsonResult GetTreeDataVerLatestAsync(string? date = null, string? search = null)
        {
            // 過濾文字
            QueryableExtensions.TrimStringProperties(date);
            QueryableExtensions.TrimStringProperties(search);

            List<object> tree = GetTreeByIssueTable(date, search, true);

            return Json(tree);
        }

        /// <summary>
        /// double click tree 版次階層加上表單發行日期 顯示所有版本
        /// </summary>
        /// <returns></returns>
        [HttpGet("SearchAll")]
        public async Task<IActionResult> SearchAll()
        {
            await _accessLog.NewActionAsync(GetLoginUser(), PageName, "顯示所有版本的查詢樹");
            return View();
        }

        /// <summary>
        /// 抓所有版本的查詢樹
        /// </summary>
        /// <param name="date">資料</param>
        /// <param name="search">關鍵字</param>
        /// <returns>json資料物件</returns>
        [HttpGet("GetTreeDataVer")]
        public JsonResult GetTreeDataVer(string? date = null, string? search = null)
        {
            // 過濾文字
            QueryableExtensions.TrimStringProperties(date);
            QueryableExtensions.TrimStringProperties(search);

            List<object> tree = GetTreeByIssueTable(date, search, false);
            return Json(tree);
        }

        /// <summary>
        /// 從 IssueTables 建樹
        /// - 2025-07-11：版次階層加上表單發行日期
        /// - 2025-08-07：新增 latestOnly 只抓各 Level1~Level3 路徑的最新版本
        /// </summary>
        private List<object> GetTreeByIssueTable(
            string? dateString = null,
            string? name = null,
            bool latestOnly = false)
        {
            // 1) 基礎查詢（盡量留在 DB 端）
            var query = _context.IssueTables
                .Where(i => i.OriginalDocNo != null && i.OriginalDocNo.StartsWith("BMP"));

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(i => i.Name.Contains(name) || i.OriginalDocNo.Contains(name));

            if (!string.IsNullOrWhiteSpace(dateString) && DateTime.TryParse(dateString, out var date))
                query = query.Where(i => i.IssueDatetime <= date);

            // 2) 轉成可切字串的序列（需進記憶體才能 Split）
            var allDocs = query
                .AsEnumerable()
                .Select(items =>
                {
                    var parts = (items.OriginalDocNo ?? string.Empty).ToUpper().Split('-');
                    return new DocRowViewModel
                    {
                        OriginalDocNo = items.OriginalDocNo!,
                        Level1 = parts.ElementAtOrDefault(0),   // BMP
                        Level2 = parts.ElementAtOrDefault(1),
                        Level3 = parts.ElementAtOrDefault(2),
                        Level4 = parts.ElementAtOrDefault(3),
                        TRCode = parts.LastOrDefault(),
                        DocVer = items.DocVer ?? "0",
                        DocVerNumber = double.TryParse(items.DocVer, out var ver) ? ver : 0,
                        Name = items.Name ?? string.Empty,
                        IssueDatetime = items.IssueDatetime
                    };
                })
                .ToList();

            // 3) 只要最新版本？就先挑出每個 Level1~3 的最大版本
            IEnumerable<DocRowViewModel> docTree = allDocs;
            if (latestOnly)
            {
                var latestMap = allDocs
                    .GroupBy(x => new { x.Level1, x.Level2, x.Level3 })
                    .Select(g => new
                    {
                        g.Key.Level1,
                        g.Key.Level2,
                        g.Key.Level3,
                        MaxVer = g.Max(x => x.DocVerNumber)
                    })
                    .ToList();

                docTree = allDocs.Where(x =>
                    latestMap.Any(v =>
                        v.Level1 == x.Level1 &&
                        v.Level2 == x.Level2 &&
                        v.Level3 == x.Level3 &&
                        v.MaxVer == x.DocVerNumber));
            }

            // 4) 排序（同你原本邏輯）
            docTree = docTree
                .OrderBy(x => x.Level1)
                .ThenBy(x => x.Level2)
                .ThenBy(x => x.Level3)
                .ThenByDescending(x => x.DocVerNumber)
                .ThenBy(x => x.TRCode)
                .ThenBy(x => x.Level4)
                .ThenBy(x => x.Name);

            // 5) 建樹
            var tree = new List<object>();
            var idCounter = 1;
            var nodeMap = new Dictionary<string, string>();

            // 說明節點
            tree.Add(new { id = "-1", parent = "#", text = "雙擊可將階層資訊推送到前台畫面" });

            // Root → BMP
            var rootId = (idCounter++).ToString();
            tree.Add(new { id = rootId, parent = "#", text = "文管系統" + $"{(!docTree.Any() ? " (查無結果!) " : "")}", partialDocNo = "BMP" });
            nodeMap["BMP"] = rootId;

            foreach (var doc in docTree)
            {
                // 逐層補節點：Level1~Level4（排除 TRCode）
                var segments = GetSegments(doc);
                var parentKey = doc.Level1;                   // "BMP"
                var parentId = nodeMap[parentKey];            // 先指到 BMP

                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    parentKey += "\\" + segment;
                    var partialDocNo = string.Join("-", segments.Take(i + 1));

                    EnsureNode(tree, nodeMap, ref idCounter,
                        key: parentKey,
                        parentId: parentId,
                        text: segment,
                        partialDocNo: partialDocNo);

                    parentId = nodeMap[parentKey];
                }

                // 版次節點（加發行日期）
                var verKey = parentKey + "\\" + doc.DocVer;
                EnsureNode(tree, nodeMap, ref idCounter,
                    key: verKey,
                    parentId: parentId,
                    text: $"{doc.DocVer}&ensp;&ensp;({doc.IssueDatetime?.ToString("yyyy/MM/dd")}發行)");

                parentId = nodeMap[verKey];

                // TRCode 節點
                var trKey = parentKey + "\\" + doc.DocVer + "\\" + doc.TRCode;
                var trPartialDocNo = string.Join("-", segments) + "-" + doc.TRCode;
                EnsureNode(tree, nodeMap, ref idCounter,
                    key: trKey,
                    parentId: parentId,
                    text: doc.TRCode,
                    partialDocNo: trPartialDocNo);

                parentId = nodeMap[trKey];

                // 葉節點（文件名稱）
                var fullPathString = $"{doc.OriginalDocNo}\\{doc.DocVer}\\{doc.Name}";
                var leafId = (idCounter++).ToString();
                tree.Add(new
                {
                    id = leafId,
                    parent = parentId,
                    text = doc.Name,
                    icon = "fas fa-file text-secondary",
                    fullPath = fullPathString,
                    partialDocNo = fullPathString,
                    doc.IssueDatetime
                });
            }

            return tree;
        }

        // ———————— 內部小工具 ————————
        /// <summary>
        /// 取得文件編號各階層資料
        /// </summary>
        /// <param name="d">資料</param>
        /// <returns>切分各階層後的資料</returns>
        private static List<string> GetSegments(DocRowViewModel d)
        {
            var segs = new List<string>();
            if (!string.IsNullOrEmpty(d.Level1) && d.Level1 != d.TRCode) segs.Add(d.Level1);
            if (!string.IsNullOrEmpty(d.Level2) && d.Level2 != d.TRCode) segs.Add(d.Level2);
            if (!string.IsNullOrEmpty(d.Level3) && d.Level3 != d.TRCode) segs.Add(d.Level3);
            if (!string.IsNullOrEmpty(d.Level4) && d.Level4 != d.TRCode) segs.Add(d.Level4);
            return segs;
        }

        /// <summary>
        /// 確保樹狀結構中的某個節點存在，若不存在則建立新節點並加入到樹中
        /// </summary>
        /// <param name="tree">整棵樹的節點清單（List<object>），用來累積所有節點</param>
        /// <param name="map">節點快取對照表（Dictionary），Key = 節點唯一識別字串，Value = 節點 ID</param>
        /// <param name="idCounter">節點 ID 的流水號計數器，會被遞增以確保 ID 唯一性</param>
        /// <param name="key">節點唯一 Key（通常是由階層字串拼湊出來，例如 BMP\BGI\AP01\V1）</param>
        /// <param name="parentId">父節點的 ID（樹狀結構用來指定層級關係）</param>
        /// <param name="text">節點顯示文字（會顯示在樹狀清單上）</param>
        /// <param name="partialDocNo">文件的部分編號（選填，可用於點擊節點後傳遞參數或查詢）</param>
        private static void EnsureNode(
            List<object> tree,
            Dictionary<string, string> map,
            ref int idCounter,
            string key,
            string parentId,
            string text,
            string? partialDocNo = null)
        {
            if (map.ContainsKey(key)) return; // 已存在就不再新增

            var newId = (idCounter++).ToString();
            tree.Add(new
            {
                id = newId,        // 節點唯一 ID
                parent = parentId, // 父節點 ID
                text,              // 節點顯示文字
                partialDocNo       // 文件部分編號（可選）
            });
            map[key] = newId;
        }

        /*
        /// <summary>
        /// 2025-05-21 從文館總表建樹
        /// </summary>
        /// <returns></returns>
        [Obsolete("可能不需要了")]
        private List<object> GetTreeByDocMainTable(string purpose = null)
        {


            var query = context.DocControlMaintables
                .Where(items => items.OriginalDocNo != null && items.OriginalDocNo.StartsWith("BMP"));

            if (!string.IsNullOrEmpty(purpose))
            {
                query = query.Where(items => items.Purpose.Contains(purpose) || items.OriginalDocNo.Contains(purpose));
            }

            var docTree = query
           .AsEnumerable()
           .Select(items =>
           {
               var parts = items.OriginalDocNo.ToUpper().Split('-');
               return new
               {
                   items.OriginalDocNo,
                   Level1 = parts.ElementAtOrDefault(0), //always BMP
                   Level2 = parts.ElementAtOrDefault(1),
                   Level3 = parts.ElementAtOrDefault(2),
                   Level4 = parts.ElementAtOrDefault(3),
                   TRCode = parts.LastOrDefault(),
                   DocVerNumber = double.TryParse(items.DocVer, out var ver) ? ver : 0,
                   items.DocVer,
                   items.Purpose,//領用目的
                   items.Name, //文件名稱
               };
           })
           .OrderBy(x => x.Level2) // group folders (BGI, BCG, etc.) alphabetically
           .ThenByDescending(x => x.DocVerNumber)// version folders go newest-first
           .ThenBy(x => x.Level3)
           .ThenBy(s => s.Level4)
           .ThenBy(s => s.Name)
           .ThenBy(s => s.Purpose)
           .ToList();

            var tree = new List<object>();
            var idCounter = 1;
            var nodeMap = new Dictionary<string, string>();

            // Root node
            var rootId = (idCounter++).ToString();
            tree.Add(new { id = "-1", parent = "#", text = "雙擊可將階層資訊推送到底層畫面" });
            tree.Add(new { id = rootId, parent = "#", text = "文管系統" + $"{(!docTree.Any() ? " (查無結果!) " : "")}", partialDocNo = "BMP" });
            nodeMap["BMP"] = rootId;

            //old
            foreach (var doc in docTree)
            {
                var parentKey = doc.Level1;
                var parentId = nodeMap[parentKey];

                var segments = new List<string>();
                if (!string.IsNullOrEmpty(doc.Level1) && doc.Level1 != doc.TRCode)
                    segments.Add(doc.Level1);
                if (!string.IsNullOrEmpty(doc.Level2) && doc.Level2 != doc.TRCode)
                    segments.Add(doc.Level2);
                if (!string.IsNullOrEmpty(doc.Level3) && doc.Level3 != doc.TRCode)
                    segments.Add(doc.Level3);
                if (!string.IsNullOrEmpty(doc.Level4) && doc.Level4 != doc.TRCode)
                    segments.Add(doc.Level4);

                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    parentKey += "\\" + segment;
                    var partialDocNo = string.Join("-", segments.Take(i + 1));

                    if (!nodeMap.ContainsKey(parentKey))
                    {
                        var newId = (idCounter++).ToString();
                        tree.Add(new
                        {
                            id = newId,
                            parent = parentId,
                            text = segment,
                            partialDocNo
                        });
                        nodeMap[parentKey] = newId;
                    }

                    parentId = nodeMap[parentKey];
                }

                // DocVer node
                var verKey = parentKey + "\\" + doc.DocVer;
                if (!nodeMap.ContainsKey(verKey))
                {
                    var verId = (idCounter++).ToString();
                    tree.Add(new
                    {
                        id = verId,
                        parent = parentId,
                        text = doc.DocVer
                    });
                    nodeMap[verKey] = verId;
                }

                parentId = nodeMap[verKey];

                // TRCode node (now includes Name directly in label)
                var trKey = parentKey + "\\" + doc.DocVer + "\\" + doc.TRCode;
                var trPartialDocNo = string.Join("-", segments) + "-" + doc.TRCode;
                if (!nodeMap.ContainsKey(trKey))
                {
                    var trId = (idCounter++).ToString();
                    tree.Add(new
                    {
                        id = trId,
                        parent = parentId,
                        text = $"{doc.TRCode} 【{doc.Name}】", // merged name into label
                        partialDocNo = trPartialDocNo
                    });
                    nodeMap[trKey] = trId;
                }

                parentId = nodeMap[trKey];

                // 📄 Leaf node (Purpose under TRCode)
                var fullPathString = $"{doc.OriginalDocNo}\\{doc.DocVer}\\{doc.Name}";
                var leafId = (idCounter++).ToString();
                tree.Add(new
                {
                    id = leafId,
                    parent = parentId,
                    text = doc.Name,
                    icon = "fas fa-file text-secondary",
                    fullPath = fullPathString,
                    partialDocNo = fullPathString
                });
            }

            if (false)
            {
                //20250703 版本
                foreach (var doc in docTree)
                {
                    var parentKey = doc.Level1;
                    var parentId = nodeMap[parentKey];

                    var segments = new List<string>();
                    if (!string.IsNullOrEmpty(doc.Level1) && doc.Level1 != doc.TRCode)
                        segments.Add(doc.Level1);
                    if (!string.IsNullOrEmpty(doc.Level2) && doc.Level2 != doc.TRCode)
                        segments.Add(doc.Level2);
                    if (!string.IsNullOrEmpty(doc.Level3) && doc.Level3 != doc.TRCode)
                        segments.Add(doc.Level3);
                    if (!string.IsNullOrEmpty(doc.Level4) && doc.Level4 != doc.TRCode)
                        segments.Add(doc.Level4);

                    for (int i = 0; i < segments.Count; i++)
                    {
                        var segment = segments[i];
                        parentKey += "\\" + segment;
                        var partialDocNo = string.Join("-", segments.Take(i + 1));

                        if (!nodeMap.ContainsKey(parentKey))
                        {
                            var newId = (idCounter++).ToString();
                            tree.Add(new
                            {
                                id = newId,
                                parent = parentId,
                                text = segment,
                                partialDocNo
                            });
                            nodeMap[parentKey] = newId;
                        }

                        parentId = nodeMap[parentKey];
                    }

                    // DocVer node (no partialDocNo here)
                    var verKey = parentKey + "\\" + doc.DocVer;
                    if (!nodeMap.ContainsKey(verKey))
                    {
                        var verId = (idCounter++).ToString();
                        tree.Add(new
                        {
                            id = verId,
                            parent = parentId,
                            text = doc.DocVer
                        });
                        nodeMap[verKey] = verId;
                    }

                    parentId = nodeMap[verKey];

                    // TRCode node (with full partialDocNo)
                    var trKey = parentKey + "\\" + doc.TRCode;
                    var trPartialDocNo = string.Join("-", segments) + "-" + doc.TRCode;
                    if (!nodeMap.ContainsKey(trKey))
                    {
                        var trId = (idCounter++).ToString();
                        tree.Add(new
                        {
                            id = trId,
                            parent = parentId,
                            text = doc.TRCode,
                            partialDocNo = trPartialDocNo
                        });
                        nodeMap[trKey] = trId;
                    }

                    parentId = nodeMap[trKey];

                    // Leaf node (file name, includes fullPath + partialDocNo)
                    var fullPathString = $"{doc.OriginalDocNo}\\{doc.DocVer}\\{doc.Purpose}";
                    var leafPartialDocNo = trPartialDocNo; // Inherit the full partialDocNo of its parent TRCode

                    //it seems we can grow another parent layer (Name) and file Purposes under it... can you help please?
                    var leafId = (idCounter++).ToString();
                    tree.Add(new
                    {
                        id = leafId,
                        parent = parentId,
                        text = doc.Name, //$"【{doc.Name}】{doc.Purpose}",
                        icon = "fas fa-file text-secondary",
                        fullPath = fullPathString,
                        partialDocNo = fullPathString
                    });
                }
            }


            return tree;
        }
        */

        /*
        /// <summary>
        /// 2025-07-11 改成從表單發行建樹 版次階層加上表單發行日期
        /// </summary>
        /// <returns></returns>
        private List<object> GetTreeByIssueTableVer(string? dateString = null, string? name = null)
        {


            var query = context.IssueTables
                .Where(i => i.OriginalDocNo != null && i.OriginalDocNo.StartsWith("BMP"));

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(i => i.Name.Contains(name) || i.OriginalDocNo.Contains(name));
            }

            if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out var date))
            {
                // 找出發行日期小於等於領用日期的資料
                query = query.Where(i => i.IssueDatetime <= date);
            }


            var docTree = query
           .AsEnumerable()
           .Select(items =>
           {
               var parts = items.OriginalDocNo.ToUpper().Split('-');
               return new
               {
                   items.OriginalDocNo,
                   Level1 = parts.ElementAtOrDefault(0), //always BMP
                   Level2 = parts.ElementAtOrDefault(1),
                   Level3 = parts.ElementAtOrDefault(2),
                   Level4 = parts.ElementAtOrDefault(3),
                   TRCode = parts.LastOrDefault(),
                   DocVerNumber = double.TryParse(items.DocVer, out var ver) ? ver : 0,
                   items.DocVer,
                   //items.Purpose,// 領用目的
                   items.Name, // 文件名稱
                   items.IssueDatetime // 表單發行日期
               };
           })
           .OrderBy(x => x.Level1) // group folders (BGI, BCG, etc.) alphabetically
           .ThenBy(x => x.Level2) // group folders (BGI, BCG, etc.) alphabetically
           .ThenBy(x => x.Level3)
           .ThenByDescending(x => x.DocVerNumber)// version folders go newest-first
           .ThenBy(s => s.TRCode)

           .ThenBy(s => s.Level4)
           .ThenBy(s => s.Name)

           .ToList();

            var tree = new List<object>();
            var idCounter = 1;
            var nodeMap = new Dictionary<string, string>();

            // Root node
            var rootId = (idCounter++).ToString();
            tree.Add(new { id = "-1", parent = "#", text = "雙擊可將階層資訊推送到底層畫面" });
            tree.Add(new { id = rootId, parent = "#", text = "文管系統" + $"{(!docTree.Any() ? " (查無結果!) " : "")}", partialDocNo = "BMP" });
            nodeMap["BMP"] = rootId;

            foreach (var doc in docTree)
            {
                var parentKey = doc.Level1;
                var parentId = nodeMap[parentKey];

                var segments = new List<string>();
                if (!string.IsNullOrEmpty(doc.Level1) && doc.Level1 != doc.TRCode)
                    segments.Add(doc.Level1);
                if (!string.IsNullOrEmpty(doc.Level2) && doc.Level2 != doc.TRCode)
                    segments.Add(doc.Level2);
                if (!string.IsNullOrEmpty(doc.Level3) && doc.Level3 != doc.TRCode)
                    segments.Add(doc.Level3);
                if (!string.IsNullOrEmpty(doc.Level4) && doc.Level4 != doc.TRCode)
                    segments.Add(doc.Level4);

                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    parentKey += "\\" + segment;
                    var partialDocNo = string.Join("-", segments.Take(i + 1));

                    if (!nodeMap.ContainsKey(parentKey))
                    {
                        var newId = (idCounter++).ToString();
                        tree.Add(new
                        {
                            id = newId,
                            parent = parentId,
                            text = segment,
                            partialDocNo
                        });
                        nodeMap[parentKey] = newId;
                    }

                    parentId = nodeMap[parentKey];
                }

                // DocVer node
                var verKey = parentKey + "\\" + doc.DocVer;
                if (!nodeMap.ContainsKey(verKey))
                {
                    var verId = (idCounter++).ToString();
                    tree.Add(new
                    {
                        id = verId,
                        parent = parentId,
                        text = doc.DocVer + "&ensp;&ensp;(" + doc.IssueDatetime?.ToString("yyyy/MM/dd") + "發行)"// 補上表單發行日期
                    });
                    nodeMap[verKey] = verId;
                }

                parentId = nodeMap[verKey];

                // TRCode node (now includes Name directly in label)
                var trKey = parentKey + "\\" + doc.DocVer + "\\" + doc.TRCode;
                var trPartialDocNo = string.Join("-", segments) + "-" + doc.TRCode;
                if (!nodeMap.ContainsKey(trKey))
                {
                    var trId = (idCounter++).ToString();
                    tree.Add(new
                    {
                        id = trId,
                        parent = parentId,
                        text = doc.TRCode, //$"{doc.TRCode} 【{doc.Name}】", // merged name into label
                        partialDocNo = trPartialDocNo
                    });
                    nodeMap[trKey] = trId;
                }

                parentId = nodeMap[trKey];

                // 📄 Leaf node (Purpose under TRCode)
                var fullPathString = $"{doc.OriginalDocNo}\\{doc.DocVer}\\{doc.Name}";
                var leafId = (idCounter++).ToString();
                tree.Add(new
                {
                    id = leafId,
                    parent = parentId,
                    text = doc.Name,
                    icon = "fas fa-file text-secondary",
                    fullPath = fullPathString,
                    partialDocNo = fullPathString,
                    doc.IssueDatetime
                });
            }




            return tree;
        }

        /// <summary>
        /// 2025-08-07 改成從表單發行建樹 版次階層加上表單發行日期 只抓最新一筆version
        /// </summary>
        /// <returns></returns>
        private List<object> GetTreeByIssueTableVerLatest(string? dateString = null, string? name = null)
        {


            var query = context.IssueTables
                .Where(i => i.OriginalDocNo != null && i.OriginalDocNo.StartsWith("BMP"));

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(i => i.Name.Contains(name) || i.OriginalDocNo.Contains(name));
            }

            if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out var date))
            {
                // 找出發行日期小於等於領用日期的資料
                query = query.Where(i => i.IssueDatetime <= date);
            }


            // Step 1: 原始資料整理，拆出各層級與版本
            var allDocs = query
                .AsEnumerable()
                .Select(items =>
                {
                    var parts = items.OriginalDocNo.ToUpper().Split('-');
                    return new
                    {
                        items.OriginalDocNo,
                        Level1 = parts.ElementAtOrDefault(0),
                        Level2 = parts.ElementAtOrDefault(1),
                        Level3 = parts.ElementAtOrDefault(2),
                        Level4 = parts.ElementAtOrDefault(3),
                        TRCode = parts.LastOrDefault(),
                        DocVerNumber = double.TryParse(items.DocVer, out var ver) ? ver : 0,
                        items.DocVer,
                        items.Name,
                        items.IssueDatetime
                    };
                })
                .ToList();

            // Step 2: 找出每組 Level1~Level3 的最大版本號
            var latestVersions = allDocs
                .GroupBy(x => new { x.Level1, x.Level2, x.Level3 })
                .Select(g => new
                {
                    g.Key.Level1,
                    g.Key.Level2,
                    g.Key.Level3,
                    MaxVer = g.Max(x => x.DocVerNumber)
                })
                .ToList();

            // Step 3: 從原始資料中，挑出符合最新版本的資料（TRCode 可多筆）
            var docTree = allDocs
                .Where(x => latestVersions.Any(v =>
                    v.Level1 == x.Level1 &&
                    v.Level2 == x.Level2 &&
                    v.Level3 == x.Level3 &&
                    v.MaxVer == x.DocVerNumber
                ))
                .OrderBy(x => x.Level1)
                .ThenBy(x => x.Level2)
                .ThenBy(x => x.Level3)
                .ThenByDescending(x => x.DocVerNumber)
                .ThenBy(x => x.TRCode)
                .ToList();


            var tree = new List<object>();
            var idCounter = 1;
            var nodeMap = new Dictionary<string, string>();

            // Root node
            var rootId = (idCounter++).ToString();
            tree.Add(new { id = "-1", parent = "#", text = "雙擊可將階層資訊推送到底層畫面" });
            tree.Add(new { id = rootId, parent = "#", text = "文管系統" + $"{(!docTree.Any() ? " (查無結果!) " : "")}", partialDocNo = "BMP" });
            nodeMap["BMP"] = rootId;

            foreach (var doc in docTree)
            {
                var parentKey = doc.Level1;
                var parentId = nodeMap[parentKey];

                var segments = new List<string>();
                if (!string.IsNullOrEmpty(doc.Level1) && doc.Level1 != doc.TRCode)
                    segments.Add(doc.Level1);
                if (!string.IsNullOrEmpty(doc.Level2) && doc.Level2 != doc.TRCode)
                    segments.Add(doc.Level2);
                if (!string.IsNullOrEmpty(doc.Level3) && doc.Level3 != doc.TRCode)
                    segments.Add(doc.Level3);
                if (!string.IsNullOrEmpty(doc.Level4) && doc.Level4 != doc.TRCode)
                    segments.Add(doc.Level4);

                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    parentKey += "\\" + segment;
                    var partialDocNo = string.Join("-", segments.Take(i + 1));

                    if (!nodeMap.ContainsKey(parentKey))
                    {
                        var newId = (idCounter++).ToString();
                        tree.Add(new
                        {
                            id = newId,
                            parent = parentId,
                            text = segment,
                            partialDocNo
                        });
                        nodeMap[parentKey] = newId;
                    }

                    parentId = nodeMap[parentKey];
                }

                // DocVer node
                var verKey = parentKey + "\\" + doc.DocVer;
                if (!nodeMap.ContainsKey(verKey))
                {
                    var verId = (idCounter++).ToString();
                    tree.Add(new
                    {
                        id = verId,
                        parent = parentId,
                        text = doc.DocVer + "&ensp;&ensp;(" + doc.IssueDatetime?.ToString("yyyy/MM/dd") + "發行)"// 補上表單發行日期
                    });
                    nodeMap[verKey] = verId;
                }

                parentId = nodeMap[verKey];

                // TRCode node (now includes Name directly in label)
                var trKey = parentKey + "\\" + doc.DocVer + "\\" + doc.TRCode;
                var trPartialDocNo = string.Join("-", segments) + "-" + doc.TRCode;
                if (!nodeMap.ContainsKey(trKey))
                {
                    var trId = (idCounter++).ToString();
                    tree.Add(new
                    {
                        id = trId,
                        parent = parentId,
                        text = doc.TRCode, //$"{doc.TRCode} 【{doc.Name}】", // merged name into label
                        partialDocNo = trPartialDocNo
                    });
                    nodeMap[trKey] = trId;
                }

                parentId = nodeMap[trKey];

                // 📄 Leaf node (Purpose under TRCode)
                var fullPathString = $"{doc.OriginalDocNo}\\{doc.DocVer}\\{doc.Name}";
                var leafId = (idCounter++).ToString();
                tree.Add(new
                {
                    id = leafId,
                    parent = parentId,
                    text = doc.Name,
                    icon = "fas fa-file text-secondary",
                    fullPath = fullPathString,
                    partialDocNo = fullPathString,
                    doc.IssueDatetime
                });
            }




            return tree;
        }
        */





        /*
        // Load tree from JSON file
        [HttpGet("/[controller]/GetOldTree")]
        public JsonResult GetOldTree(string search = null)
        {
            //從DB建立初版 JSON TREE
            List<object> tree = GetTreeByDocMainTable(purpose: search);
            return Json(tree);
        }
        */
        /*
        // Save entire tree to file
        [HttpPost]
        private ActionResult SaveTree(List<TreeNode> tree)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "tree.json");
            var json = JsonConvert.SerializeObject(tree, Formatting.Indented);
            System.IO.File.WriteAllText(path, json);

            return Ok();
        }
        */
        /*
        /// <summary>
        /// ✅ checkbox tree
        /// </summary>
        /// <returns></returns>
        public IActionResult CheckBoxTree()
        {
            return View();
        }
        */
        /*
        /// <summary>
        /// 模擬現行文館的文件樹狀 (BMP-*)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Obsolete]
        public JsonResult GetTreeData_DB()
        {
            List<object> tree = GetTreeByDocMainTable();

            return Json(tree);
        }
        */


        /*
        /// <summary>
        /// 2025-05-14 只有 完整路徑有 doc_no
        /// </summary>
        /// <remarks></remarks>
        /// <returns></returns>
        private List<object> InitialDocTree_v0()
        {
            var docTree = context.DocControlMaintables
                .Where(items => items.OriginalDocNo != null && items.OriginalDocNo.StartsWith("BMP"))
                .AsEnumerable() // Bring to memory to safely use Split
                .OrderBy(items => items.OriginalDocNo)
                .ThenByDescending(s => s.DocVer)
                .Select(items =>
                {
                    var parts = items.OriginalDocNo.ToUpper().Split('-');
                    return new
                    {
                        items.OriginalDocNo,
                        Level1 = parts.ElementAtOrDefault(0),
                        Level2 = parts.ElementAtOrDefault(1),
                        Level3 = parts.ElementAtOrDefault(2),
                        Level4 = parts.ElementAtOrDefault(3), // TR### (optional)
                        TRCode = parts.LastOrDefault(),
                        items.DocVer,
                        items.Purpose
                    };
                })
                .ToList();

            var tree = new List<object>();
            var idCounter = 1;
            var nodeMap = new Dictionary<string, string>();

            // Root node
            var rootId = (idCounter++).ToString();
            tree.Add(new { id = rootId, parent = "#", text = "文管系統" });
            nodeMap["BMP"] = rootId;

            foreach (var doc in docTree)
            {
                var parentKey = doc.Level1;
                var parentId = nodeMap[parentKey];

                //DocNo = BMP-AA-BB-TR001 切階層 (自動適應)
                var segments = new List<string>();
                if (!string.IsNullOrEmpty(doc.Level1) && doc.Level1 != doc.TRCode)
                    segments.Add(doc.Level1);
                if (!string.IsNullOrEmpty(doc.Level2) && doc.Level2 != doc.TRCode)
                    segments.Add(doc.Level2);
                if (!string.IsNullOrEmpty(doc.Level3) && doc.Level3 != doc.TRCode)
                    segments.Add(doc.Level3);
                if (!string.IsNullOrEmpty(doc.Level4) && doc.Level4 != doc.TRCode)
                    segments.Add(doc.Level4);

                foreach (var segment in segments)
                {
                    parentKey += "\\" + segment;
                    if (!nodeMap.ContainsKey(parentKey))
                    {
                        var newId = (idCounter++).ToString();
                        tree.Add(new
                        {
                            id = newId,
                            parent = parentId,
                            text = segment
                        });
                        nodeMap[parentKey] = newId;
                    }
                    parentId = nodeMap[parentKey];
                }

                // 版次要作為每個 TRCode 的父節點
                var verKey = parentKey + "\\" + doc.DocVer;
                if (!nodeMap.ContainsKey(verKey))
                {
                    var verId = (idCounter++).ToString();
                    tree.Add(new
                    {
                        id = verId,
                        parent = parentId,
                        text = doc.DocVer
                    });
                    nodeMap[verKey] = verId;
                }
                parentId = nodeMap[verKey];

                // Add TRCode as the final container under DocVer
                var trKey = parentKey + "\\" + doc.DocVer + "\\" + doc.TRCode;
                if (!nodeMap.ContainsKey(trKey))
                {
                    var trId = (idCounter++).ToString();
                    tree.Add(new
                    {
                        id = trId,
                        parent = parentId,
                        text = doc.TRCode
                    });
                    nodeMap[trKey] = trId;
                }
                parentId = nodeMap[trKey];

                // Build full path info
                var fullPathString = $"{doc.OriginalDocNo}\\{doc.DocVer}\\{doc.Purpose}";
                var fullPathJson = new
                {
                    DocNo = doc.OriginalDocNo,
                    DocVer = doc.DocVer,
                    DocName = doc.Purpose
                };

                // Add the file leaf under TRCode
                var leafId = (idCounter++).ToString();
                tree.Add(new
                {
                    id = leafId,
                    parent = parentId,
                    text = doc.Purpose,
                    icon = "fas fa-file text-secondary",
                    fullPath = fullPathString,
                });
            }

            return tree;
        }
        */
        /*
        /// <summary>
        /// 文管文件樹狀階層 (無階層資訊)
        /// </summary>
        /// <remarks></remarks>
        /// <returns></returns>
        private List<object> InitialDocTree_original()
        {
            var docTree = context.DocControlMaintables
                .Where(items => items.OriginalDocNo != null && items.OriginalDocNo.StartsWith("BMP"))
                .AsEnumerable() // Bring to memory to safely use Split
                .OrderBy(items => items.OriginalDocNo)
                .ThenByDescending(s => s.DocVer)
                .Select(items =>
                {
                    var parts = items.OriginalDocNo.ToUpper().Split('-');
                    return new
                    {
                        items.OriginalDocNo,
                        Level1 = parts.ElementAtOrDefault(0),
                        Level2 = parts.ElementAtOrDefault(1),
                        Level3 = parts.ElementAtOrDefault(2),
                        Level4 = parts.ElementAtOrDefault(3), // TR### (optional)
                        TRCode = parts.LastOrDefault(),
                        items.DocVer,
                        items.Purpose
                    };
                })
                .ToList();

            var tree = new List<object>();
            var idCounter = 1;
            var nodeMap = new Dictionary<string, string>();

            // Root node
            var rootId = (idCounter++).ToString();
            tree.Add(new { id = rootId, parent = "#", text = "文管系統" });
            nodeMap["BMP"] = rootId;

            foreach (var doc in docTree)
            {
                var parentKey = doc.Level1;
                var parentId = nodeMap[parentKey];

                // Build up intermediate nodes that are NOT the same as TRCode
                var segments = new List<string>();
                if (!string.IsNullOrEmpty(doc.Level1) && doc.Level1 != doc.TRCode)
                    segments.Add(doc.Level1);
                if (!string.IsNullOrEmpty(doc.Level2) && doc.Level2 != doc.TRCode)
                    segments.Add(doc.Level2);
                if (!string.IsNullOrEmpty(doc.Level3) && doc.Level3 != doc.TRCode)
                    segments.Add(doc.Level3);
                if (!string.IsNullOrEmpty(doc.Level4) && doc.Level4 != doc.TRCode)
                    segments.Add(doc.Level4);

                foreach (var segment in segments)
                {
                    parentKey += "\\" + segment;
                    if (!nodeMap.ContainsKey(parentKey))
                    {
                        var newId = (idCounter++).ToString();
                        tree.Add(new { id = newId, parent = parentId, text = segment });
                        nodeMap[parentKey] = newId;
                    }
                    parentId = nodeMap[parentKey];
                }

                // Add DocVer as a folder under the last segment
                var verKey = parentKey + "\\" + doc.DocVer;
                if (!nodeMap.ContainsKey(verKey))
                {
                    var verId = (idCounter++).ToString();
                    tree.Add(new { id = verId, parent = parentId, text = doc.DocVer });
                    nodeMap[verKey] = verId;
                }
                parentId = nodeMap[verKey];

                // Add TRCode as the final container under DocVer
                var trKey = parentKey + "\\" + doc.DocVer + "\\" + doc.TRCode;
                if (!nodeMap.ContainsKey(trKey))
                {
                    var trId = (idCounter++).ToString();
                    tree.Add(new { id = trId, parent = parentId, text = doc.TRCode });
                    nodeMap[trKey] = trId;
                }
                parentId = nodeMap[trKey];

                // Add the file leaf under TRCode
                var leafId = (idCounter++).ToString();
                tree.Add(new
                {
                    id = leafId,
                    parent = parentId,
                    text = doc.Purpose,
                    icon = "fas fa-file fas fa-file text-secondary"
                });
            }

            return tree;
        }
        */



        /*
        public new IActionResult NotFound()
        {
            return View("~/Views/Shared/_NotFound.cshtml");
        }
        */
        /*
        /// <summary>
        /// 示範 dapper 將 model 轉成 insert/update/delete 的語法
        /// </summary>
        void DapperCrud()
        {

            var ppm = new PeoplePurchaseTable();

            //測試 泛型的 SQL CRUD 產生器
            var add = SqlHelper.GenerateInsertQuery(ppm);
            var update = SqlHelper.GenerateUpdateQuery(ppm, ppm);
            var delete = SqlHelper.GenerateDeleteQuery(ppm);

            var debug = string.Join(Environment.NewLine, add, update, delete);


        }
        */

        /// <summary>
        /// 示範使用 transaction scope, 確保所有DB變更都可以中途還原
        /// </summary>
        /// <returns></returns>
        /*
            private async Task<IActionResult> EditData()
            {
                using var connection = context.Database.GetDbConnection();

                try
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        // Add your entities here using Dapper's `Execute` or `ExecuteAsync`
                        var query1 = "INSERT INTO Table1 (Column1, Column2) VALUES (@Value1, @Value2)";
                        await connection.ExecuteAsync(query1, new { Value1 = "Test1", Value2 = "Test2" }, transaction);

                        var query2 = "INSERT INTO Table2 (Column1, Column2) VALUES (@Value1, @Value2)";
                        await connection.ExecuteAsync(query2, new { Value1 = "Test3", Value2 = "Test4" }, transaction);

                        context.PeopleControlTables.Add(new PeopleControlTable());
                        await _context.SaveChangesAsync();

                        // If everything went well, commit the transaction
                        transaction.Commit();
                    }
                    return Ok(";)"); // Success response
                }
                catch (Exception ex)
                {
                    // If an exception occurs, rollback the transaction
                    // The transaction will be rolled back automatically when disposed if Commit is not called
                    return BadRequest($"An error occurred: {ex.Message}");
                }
            }

            public IActionResult Privacy()
            {
                return View();
            }
        */

        /*
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        */



    }
}
