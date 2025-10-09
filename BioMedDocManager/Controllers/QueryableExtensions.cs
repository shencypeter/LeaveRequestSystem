using BioMedDocManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace BioMedDocManager.Controllers
{
    public static class QueryableExtensions
    {
        #region 變數
        /// <summary>
        /// XSS防護：允許的可見空白：空白/Tab/CR/LF
        /// </summary>
        private static readonly Regex ControlCharsExceptWhitespace = new(
            @"[^\P{C}\t\r\n]", RegexOptions.Compiled);
        /// <summary>
        /// XSS防護：移除零寬字元（ZWSP/ZWNJ/ZWJ 等）
        /// </summary>
        private static readonly Regex ZeroWidthChars = new(
            "[\u200B-\u200F\u202A-\u202E\u2060-\u206F\uFEFF]", RegexOptions.Compiled);

        /// <summary>
        /// XSS防護：移除所有 HTML 標籤（最保守，轉成純文字）
        /// </summary>
        private static readonly Regex HtmlTags = new(
            @"</?[^>]+?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// XSS防護：常見「偽裝」的危險片段（解碼後再掃）
        /// </summary>
        private static readonly Regex DangerousFragments = new(
            @"(?ix)
            (?:\bon\w+\s*=)       # onload=, onclick= 等事件
            |(?:javascript\s*:)    # javascript: 協定
            |(?:data\s*:[^,]*,?)   # data: 負載
            |(?:vbscript\s*:)
            |(?:<\s*script\b)
            |(?:<\s*iframe\b)
            |(?:<\s*img\b)
            |(?:<\s*svg\b)
        ", RegexOptions.Compiled);
        #endregion

        #region 方法
        /// <summary>
        /// 排序過濾參數
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="TableHeaders"></param>
        /// <param name="queryModel"></param>
        /// <param name="InitSort"></param>
        public static void OrderByFilter<T>(Dictionary<string, string> TableHeaders, T queryModel, string InitSort) where T : Pagination
        {
            var sortClickColumn = queryModel.OrderBy;

            if (!string.IsNullOrEmpty(sortClickColumn) && sortClickColumn != "RowNum" && TableHeaders.ContainsKey(sortClickColumn))
            {
                queryModel.OrderBy = sortClickColumn;
            }
            else
            {
                queryModel.OrderBy = InitSort; // default fallback
            }

        }

        /// <summary>
        /// 設定查詢模型Session資料
        /// </summary>
        /// <typeparam name="T">class類型</typeparam>
        /// <param name="context">http內容</param>
        /// <param name="model">模型</param>
        /// <param name="sessionKey">session名稱</param>
        public static void SetSessionQueryModel<T>(HttpContext context, T model, string? sessionKey)
        {
            var key = !string.IsNullOrWhiteSpace(sessionKey)
            ? sessionKey
            : (context.Items.TryGetValue("SessionKey", out var v) ? v as string : null) ?? "GlobalQueryModel";

            // 顯示頁面數與第幾頁檢查，若model有PageNumber/PageSize屬性，進行驗證與修正
            var type = typeof(T);
            var pageNumberProp = type.GetProperty("PageNumber");
            var pageSizeProp = type.GetProperty("PageSize");

            if (pageNumberProp != null && pageNumberProp.PropertyType == typeof(int))
            {
                int pageNumber = (int)(pageNumberProp.GetValue(model) ?? 0);
                if (pageNumber <= 0)
                    pageNumberProp.SetValue(model, 1); // 預設為第 1 頁
            }

            if (pageSizeProp != null && pageSizeProp.PropertyType == typeof(int))
            {
                int pageSize = (int)(pageSizeProp.GetValue(model) ?? 0);
                if (pageSize <= 0)
                    pageSizeProp.SetValue(model, 10); // 預設每頁 10 筆
            }

            context.Session.SetString(key, JsonSerializer.Serialize(model));
        }

        /// <summary>
        /// 設定查詢模型Session資料
        /// </summary>
        /// <typeparam name="T">class類型</typeparam>
        /// <param name="context">http內容</param>
        /// <param name="model">模型</param>
        public static void SetSessionQueryModel<T>(HttpContext context, T model)
            => SetSessionQueryModel(context, model, null);

        /// <summary>
        /// 取得查詢模型Session資料
        /// </summary>
        /// <typeparam name="T">class類型</typeparam>
        /// <param name="context">http內容</param>
        /// <param name="sessionKey">session名稱</param>
        public static T GetSessionQueryModel<T>(HttpContext context, string? sessionKey) where T : new()
        {
            var key = !string.IsNullOrWhiteSpace(sessionKey)
            ? sessionKey
            : (context.Items.TryGetValue("SessionKey", out var v) ? v as string : null) ?? "GlobalQueryModel";

            var sessionData = context.Session.GetString(key);
            if (!string.IsNullOrEmpty(sessionData))
            {
                return JsonSerializer.Deserialize<T>(sessionData);
            }

            // If session data is empty, create a new instance of T and set default pagination values if applicable
            var result = new T();

            // Check if T has the properties 'PageNumber' and 'PageSize'
            var pageNumberProperty = typeof(T).GetProperty("PageNumber");
            var pageSizeProperty = typeof(T).GetProperty("PageSize");

            if (pageNumberProperty != null && pageSizeProperty != null)
            {
                // Set default pagination values
                pageNumberProperty.SetValue(result, 1);
                pageSizeProperty.SetValue(result, 10);
            }

            return result;
        }

        /// <summary>
        /// 取得查詢模型Session資料
        /// </summary>
        /// <typeparam name="T">class類型</typeparam>
        /// <param name="context">http內容</param>
        public static T GetSessionQueryModel<T>(HttpContext context) where T : new()
            => GetSessionQueryModel<T>(context, null);

        /// <summary>
        ///  過濾文字：Trim + Unicode 正規化 + 移除控制/零寬 + HTML 解碼 + 去標籤 + 二次清潔。
        /// </summary>
        /// <param name="obj">文字物件</param>
        public static void TrimStringProperties(object? obj)
        {
            if (obj is null) return;

            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.PropertyType == typeof(string));

            foreach (var prop in props)
            {
                var raw = (string)prop.GetValue(obj);
                if (string.IsNullOrEmpty(raw))
                    continue;

                // 1) Trim + Unicode 正規化（Form C）
                var s = raw.Trim().Normalize(NormalizationForm.FormC);

                // 2) 去除零寬與不必要控制字元
                s = ZeroWidthChars.Replace(s, "");
                s = ControlCharsExceptWhitespace.Replace(s, "");

                // 3) 先 HTML 解碼（把 &lt; 之類解回來，避免雙重編碼躲避檢查）
                s = WebUtility.HtmlDecode(s);

                // 4) 策略：純文字
                s = HtmlTags.Replace(s, "");

                // 5) 再做一次危險片段清潔（保守刪除）
                s = DangerousFragments.Replace(s, "");

                // 6) 收尾：再次 Trim（避免清理後殘留多餘空白/換行）
                s = s.Trim();

                prop.SetValue(obj, s);
            }
        }

        /// <summary>
        /// 分頁查詢 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static async Task<List<T>> GetPagedAsync<T>(this IQueryable<T> query, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// 分頁查詢並返回總數 (queryable 先設好排序再傳來處理)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static async Task<(List<T> Items, int TotalCount)> GetPagedWithCountAsync<T>(this IQueryable<T> query, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            //entity 產出的 sql
            var sql = query.ToQueryString();

            return (items, totalCount);
        }


        #endregion

    }


    public static class TableHelper
    {
        public static string FormatValue(object value)
        {
            return value switch
            {
                null or "" => "(無)",
                DateTime dt => dt.ToString("yyyy-MM-dd"),
                int i => i.ToString("#,###,##0"),
                decimal d => d.ToString("#,###,##0"),
                double dbl => dbl.ToString("#,###,##0"),
                _ => value.ToString()
            };
        }
    }


}
