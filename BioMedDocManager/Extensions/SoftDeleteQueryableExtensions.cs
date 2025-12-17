using BioMedDocManager.Interface;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Extensions
{
    public static class SoftDeleteQueryableExtensions
    {

        /// <summary>
        /// 查詢時包含已軟刪除的資料
        /// </summary>
        /// <typeparam name="T">Model類型</typeparam>
        /// <param name="q">查詢EF</param>
        /// <returns></returns>
        public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> q) where T : class, ISoftDelete
            => q.IgnoreQueryFilters();

        /// <summary>
        /// 僅查詢已軟刪除的資料
        /// </summary>
        /// <typeparam name="T">Model類型</typeparam>
        /// <param name="q">查詢EF</param>
        /// <returns></returns>
        public static IQueryable<T> OnlyDeleted<T>(this IQueryable<T> q) where T : class, ISoftDelete
            => q.IgnoreQueryFilters().Where(e => e.DeletedAt != null);

        // 使用方式
        //var a = await _context.DocControlMaintables.IncludeDeleted().ToListAsync();
        //var b = await _context.DocControlMaintables.OnlyDeleted().ToListAsync();

    }

}
