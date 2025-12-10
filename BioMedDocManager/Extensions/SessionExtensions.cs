using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BioMedDocManager.Extensions
{
    public static class SessionExtensions
    {
        private static readonly JsonSerializerOptions _sessionJsonOptions = new()
        {
            // 忽略循環參考
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false,
        };

        public static void SetObject<T>(this ISession session, string key, T value)
        {
            if (value == null)
            {
                session.Remove(key);
                return;
            }

            var json = JsonSerializer.Serialize(value, _sessionJsonOptions);
            session.SetString(key, json);
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, _sessionJsonOptions);
        }
    }
}
