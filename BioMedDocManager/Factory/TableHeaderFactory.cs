namespace BioMedDocManager.Factory
{
    using System.Collections.Concurrent;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Reflection;

    public static class TableHeaderFactory
    {
        private static readonly ConcurrentDictionary<(Type, bool, string), Dictionary<string, string>> _cache = new();

        /// <summary>
        /// 從模型型別建立「屬性名 → 顯示名稱」對照表（支援 [Display(Name=…)] / 資源檔 / [DisplayName]）。
        /// </summary>
        /// <typeparam name="TModel">你的 EF 模型或 DTO</typeparam>
        /// <param name="includeRowNum">是否加入 RowNum → "#"</param>
        /// <param name="onlyProps">
        /// 指定要包含的屬性名（可控制欄位順序與範圍）；null 表示全取（會依 Display(Order) 排序）。
        /// </param>
        public static Dictionary<string, string> Build<TModel>(
            bool includeRowNum = true,
            IReadOnlyList<string>? onlyProps = null)
        {
            // 用 onlyProps 建一個順序鍵，做快取 key（避免大集合直接串接）
            var orderKey = onlyProps is null ? "" : string.Join("|", onlyProps);

            return _cache.GetOrAdd((typeof(TModel), includeRowNum, orderKey), _ =>
            {
                var flags = BindingFlags.Instance | BindingFlags.Public;
                var props = typeof(TModel).GetProperties(flags)
                    // 僅取可讀、可序列化的簡單型別（你也可以拿掉這段，就全取）
                    .Where(p => p.GetMethod != null && IsSimpleType(p.PropertyType))
                    .ToList();

                // 如果指定 onlyProps，就照傳入順序挑出
                if (onlyProps is not null && onlyProps.Count > 0)
                {
                    var lookup = props.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
                    props = onlyProps.Where(n => lookup.ContainsKey(n))
                                     .Select(n => lookup[n])
                                     .ToList();
                }
                else
                {
                    // 沒指定時，若有 Display(Order) 則依 Order 排序；同 Order 再依名稱
                    props = props.OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.GetOrder() ?? 0)
                                 .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                                 .ToList();
                }

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (includeRowNum)
                    dict["RowNum"] = "#";

                foreach (var p in props)
                {
                    var displayName = GetDisplayName(p);
                    dict[p.Name] = displayName; // Key=屬性名, Value=顯示名稱
                }

                return dict;
            });
        }

        private static string GetDisplayName(PropertyInfo pi)
        {
            // 1) [Display(Name="…", ResourceType=…)]（會自動抓資源字串）
            var disp = pi.GetCustomAttribute<DisplayAttribute>();
            if (disp != null)
            {
                var name = disp.GetName();
                if (!string.IsNullOrWhiteSpace(name)) return name!;
            }

            // 2) [DisplayName("…")]
            var dn = pi.GetCustomAttribute<DisplayNameAttribute>();
            if (dn != null && !string.IsNullOrWhiteSpace(dn.DisplayName))
                return dn.DisplayName;

            // 3) 預設回屬性名
            return pi.Name;
        }

        private static bool IsSimpleType(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t.IsPrimitive
                || t.IsEnum
                || t == typeof(string)
                || t == typeof(decimal)
                || t == typeof(DateTime)
                || t == typeof(Guid)
                || t == typeof(DateTimeOffset)
                || t == typeof(TimeSpan);
        }
    }

}
