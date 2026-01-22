using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace BioMedDocManager.Helpers
{
    public class DbLocalizer(DocControlContext _db, IMemoryCache _cache) : IDbLocalizer
    {

        // 系統預設語系
        private const string DefaultCulture = "zh-TW";

        // Cache 設定
        private static readonly MemoryCacheEntryOptions CacheOpt = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
        };

        public string T(string key)
        {
            var culture = CultureInfo.CurrentUICulture?.Name ?? DefaultCulture;
            return T(key, culture);
        }

        public string T(string key, string? cultureName)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var culture = string.IsNullOrWhiteSpace(cultureName) ? DefaultCulture : cultureName.Trim();

            // fallback 順序：精準 culture -> parent culture -> DefaultCulture -> en-US -> key
            foreach (var c in BuildFallbackCultures(culture))
            {
                var cacheKey = $"loc:{c}:{key}";
                if (_cache.TryGetValue(cacheKey, out string? cached))
                {
                    if (!string.IsNullOrEmpty(cached))
                    {
                        return cached;
                    }
                    continue;
                }

                var val = _db.LocalizationStrings
                    .AsNoTracking()
                    .Where(x => x.LocalizationStringIsActive && x.DeletedAt == null && x.LocalizationStringCulture == c && x.LocalizationStringKey == key)
                    .Select(x => x.LocalizationStringValue)
                    .FirstOrDefault();

                // 用空字串也快取，避免一直打 DB（但回傳時要再 fallback）
                _cache.Set(cacheKey, val ?? string.Empty, CacheOpt);

                if (!string.IsNullOrEmpty(val))
                {
                    return val;
                }
            }

            // 找不到就回 key（你也可以回 $"[{key}]" 方便抓缺字）
            return key;
        }

        private static IEnumerable<string> BuildFallbackCultures(string culture)
        {
            // culture 可能是 zh-TW, en-US, zh, en
            // 我們給：culture -> parent -> DefaultCulture -> en-US
            var list = new List<string>();

            if (!string.IsNullOrWhiteSpace(culture))
            {
                list.Add(culture);

                try
                {
                    var ci = CultureInfo.GetCultureInfo(culture);
                    var parent = ci.Parent?.Name;
                    if (!string.IsNullOrWhiteSpace(parent) && !string.Equals(parent, culture, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(parent);
                    }
                }
                catch
                {
                    // ignore invalid culture
                }
            }

            if (!list.Contains(DefaultCulture, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(DefaultCulture);
            }

            if (!list.Contains("en-US", StringComparer.OrdinalIgnoreCase))
            {
                list.Add("en-US");
            }

            // 去重複
            return list.Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }
}
