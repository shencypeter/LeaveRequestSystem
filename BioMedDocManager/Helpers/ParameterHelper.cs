using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace BioMedDocManager.Helpers
{
    public class ParameterHelper(DocControlContext _context, IMemoryCache _cache) : IParameterService
    {

        private const string CachePrefix = "PARAM_";
        private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(10);

        // ===== 基礎：抓字串 =====
        public string? GetString(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            return _cache.GetOrCreate(CachePrefix + code, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTime;

                return _context.Parameters
                    .AsNoTracking()
                    .Where(p =>
                        p.ParameterCode == code &&
                        p.ParameterIsActive &&
                        p.DeletedAt == null)
                    .Select(p => p.ParameterValue)
                    .FirstOrDefault();
            });
        }

        // ===== int =====
        public int? GetInt(string code)
        {
            var val = GetString(code);
            if (int.TryParse(val, out var i))
            {
                return i;
            }
            return null;
        }

        // ===== bool（0/1 / true/false）=====
        public bool GetBool(string code)
        {
            var val = GetString(code);
            if (string.IsNullOrWhiteSpace(val))
            {
                return false;
            }

            return val == "1" ||
                   val.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // ===== JSON =====
        public T? GetJson<T>(string code)
        {
            var val = GetString(code);
            if (string.IsNullOrWhiteSpace(val))
            {
                return default;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(val);
            }
            catch
            {
                return default;
            }
        }
    }

}
