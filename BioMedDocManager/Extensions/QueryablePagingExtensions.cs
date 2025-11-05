using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Extensions
{
    public static class QueryablePagingExtensions
    {
        /// <summary>
        /// 對 IQueryable 資料來源執行分頁查詢，並同時回傳符合條件的總筆數。
        /// </summary>
        /// <typeparam name="T">
        /// IQueryable 中的元素型別（通常是 EF 的實體或投影的 DTO）。
        /// </typeparam>
        /// <param name="source">
        /// 要進行查詢的 IQueryable 資料來源。
        /// </param>
        /// <param name="pageNumber">
        /// 頁碼（從 1 開始）。若小於等於 0，則不進行分頁，只回傳所有資料。
        /// </param>
        /// <param name="pageSize">
        /// 每頁筆數。若小於等於 0，則不進行分頁，只回傳所有資料。
        /// </param>
        /// <param name="ct">
        /// CancellationToken，可由 ASP.NET Core 注入 (HttpContext.RequestAborted)，允許在用戶中斷或逾時時提前取消資料庫查詢，避免浪費資源。
        /// </param>
        /// <returns>
        /// 一個包含兩個元素的 tuple：
        /// <list type="bullet">
        ///   <item><description>Items：依照分頁與條件查詢後的資料清單。</description></item>
        ///   <item><description>TotalCount：符合查詢條件的總筆數（不受分頁影響）。</description></item>
        /// </list>
        /// </returns>
        public static async Task<(List<T> Items, int TotalCount)>
            PaginateWithCountAsync<T>(this IQueryable<T> source, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var hasPaging = pageNumber > 0 && pageSize > 0;
            var total = await source.CountAsync(ct);

            if (hasPaging)
                source = source.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            var list = await source.ToListAsync(ct);
            return (list, total);
        }
    }
}
