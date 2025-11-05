using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Concurrent;
using System.Reflection;

namespace BioMedDocManager.Extensions
{
    public static class OrderByWhitelistExtensions
    {
        // 快取：每個實體表對應一份 { DB欄位名 -> CLR屬性名 }
        private static readonly ConcurrentDictionary<(Type, string?), Dictionary<string, string>> _colMapCache = new();

        /// <summary>
        /// 查詢字串欄位名白名單、排序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">查詢條件EF</param>
        /// <param name="orderByProperty">排序欄位</param>
        /// <param name="sortDir">排序方向</param>
        /// <param name="whitelist">白名單</param>
        /// <param name="tiebreakerProperty">第2排序欄位</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IQueryable<T> OrderByWhitelist<T>(
        this IQueryable<T> query,
        string orderByProperty,                 // e.g. "IdNo" / "DateTime" / "Type"
        string sortDir,                         // "asc" / "desc"
        IReadOnlyDictionary<string, string> whitelist,
        string? tiebreakerProperty = null)      // e.g. "IdNo"
        where T : class
        {
            if (string.IsNullOrWhiteSpace(orderByProperty) || !whitelist.ContainsKey(orderByProperty))
                throw new InvalidOperationException($"排序欄位 '{orderByProperty}' 不在白名單中。");

            // 確認屬性存在（忽略大小寫）
            var pi = typeof(T).GetProperty(orderByProperty,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                     ?? throw new InvalidOperationException(
                         $"找不到 {typeof(T).Name}.{orderByProperty} 屬性。");

            var asc = !string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            var ordered = asc
                ? query.OrderBy(e => EF.Property<object>(e, pi.Name))
                : query.OrderByDescending(e => EF.Property<object>(e, pi.Name));

            if (!string.IsNullOrWhiteSpace(tiebreakerProperty))
            {
                var tiePi = typeof(T).GetProperty(tiebreakerProperty,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                           ?? throw new InvalidOperationException(
                               $"找不到 {typeof(T).Name}.{tiebreakerProperty} 屬性。");

                ordered = asc
                    ? ordered.ThenBy(e => EF.Property<object>(e, tiePi.Name))
                    : ordered.ThenByDescending(e => EF.Property<object>(e, tiePi.Name));
            }

            return ordered;
        }

    }
}
