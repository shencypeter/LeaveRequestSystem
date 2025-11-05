using BioMedDocManager.Models;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace BioMedDocManager.Helpers
{
    /// <summary>
    /// 工具
    /// </summary>
    public static class Utilities
    {

        // 由 Program.cs 在啟動時設定
        private static string? _contentRootPath;

        /// <summary>由啟動程式設定 ContentRootPath（非 wwwroot）。</summary>
        public static void ConfigurePaths(IWebHostEnvironment env)
            => _contentRootPath = env.ContentRootPath;

        /// <summary>
        ///  取得目前登入者的UserId(int)
        /// </summary>
        /// <param name="ctx">http內容</param>
        /// <returns>登入者的UserId</returns>
        public static int? GetCurrentUserId(HttpContext? ctx)
        {
            var u = ctx?.User;
            if (u?.Identity?.IsAuthenticated != true) return null;

            var id = u.FindFirst("UserId")?.Value
                   ?? u.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(id, out var n) ? n : null;
        }

        /// <summary>
        /// 建立：設定 CreatedAt/CreatedBy
        /// </summary>
        /// <param name="entity">物件</param>
        /// <param name="ctx">http內容</param>
        /// <param name="now"></param>
        public static void StampCreate(object entity, HttpContext? ctx)
        {
            var ts = DateTime.Now;
            var uid = GetCurrentUserId(ctx);

            SetProp(entity, "CreatedAt", ts);
            SetProp(entity, "CreatedBy", uid);
        }

        /// 更新：設定 UpdatedAt/UpdatedBy
        public static void StampUpdate(object entity, HttpContext? ctx)
        {
            var ts = DateTime.Now;
            var uid = GetCurrentUserId(ctx);

            SetProp(entity, "UpdatedAt", ts);
            SetProp(entity, "UpdatedBy", uid);
        }

        /// 刪除：設定 DeletedAt/DeletedBy（軟刪除）
        public static void StampDelete(object entity, HttpContext? ctx)
        {
            var ts = DateTime.Now;
            var uid = GetCurrentUserId(ctx);

            SetProp(entity, "DeletedAt", ts);
            SetProp(entity, "DeletedBy", uid);
        }

        /// <summary>
        /// 設定物件屬性對應（忽略大小寫；若屬性不存在或型別不相容則略過）=====
        /// </summary>
        /// <param name="target">目標物件</param>
        /// <param name="propName">要設定的屬性名稱</param>
        /// <param name="value">要設定的值</param>
        public static void SetProp(object target, string propName, object? value)
        {
            var pi = target.GetType().GetProperty(
                propName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (pi == null || !pi.CanWrite) return;

            var t = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;

            try
            {
                if (value == null)
                {
                    pi.SetValue(target, null);
                }
                else if (t.IsAssignableFrom(value.GetType()))
                {
                    pi.SetValue(target, value);
                }
                else
                {
                    // 嘗試轉型（例如 int? 屬性但 value 是 string）
                    var converted = Convert.ChangeType(value, t);
                    pi.SetValue(target, converted);
                }
            }
            catch
            {
                // 型別不相容就略過，不拋例外以免影響主要流程
            }
        }

        /// <summary>
        /// 讀取屬性值（忽略大小寫）。找不到屬性回傳 null
        /// </summary>
        /// <param name="target">要取得的目標物件</param>
        /// <param name="propName">要取得的屬性名稱</param>
        /// <returns></returns>
        public static object? GetProp(object target, string propName)
        {
            var pi = target.GetType().GetProperty(
                propName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return pi?.GetValue(target);
        }

        /// <summary>
        /// 將 Exception 寫入 Log 檔案。
        /// </summary>
        /// <param name="exception"></param>
        public static void WriteExceptionIntoLogFile(string customErrorString, Exception exception, HttpContext? httpContext = null)
        {
            try
            {
                if (exception is ThreadAbortException)
                {
                    return;
                }

                var exceptionLogPath = AppSettings.ExceptionLogPath;

                if (string.IsNullOrWhiteSpace(exceptionLogPath))
                {
                    return;
                }

                var errorString = new StringBuilder();

                // 自訂錯誤訊息
                errorString.AppendLine($"customErrorString: {customErrorString}");

                // 取得 Request URL（若無 HttpContext 則略過）
                var requestUrl = httpContext?.Request?.GetDisplayUrl();
                if (!string.IsNullOrEmpty(requestUrl))
                {
                    errorString.AppendLine($"RequestUrl: {requestUrl}");
                }
                // 取得 Client IP
                var clientIp = httpContext?.Request is { } req ? GetClientIpAddress(req) : string.Empty;
                if (!string.IsNullOrEmpty(clientIp))
                {
                    errorString.AppendLine($"ClientIp: {clientIp}");
                }

                // 例外鏈
                var ex = exception;
                while (ex != null)
                {
                    errorString.AppendLine($"Message: {ex.Message}");
                    errorString.AppendLine($"Source: {ex.Source}");
                    errorString.AppendLine("StackTrace:");
                    errorString.AppendLine(ex.StackTrace);

                    if (ex.Data?.Count > 0)
                    {
                        errorString.AppendLine("Data:");
                        foreach (var key in ex.Data.Keys)
                        {
                            if (key is string sk)
                            {
                                errorString.AppendLine(sk + ":");
                                errorString.AppendLine(ex.Data[sk]?.ToString());
                            }
                        }
                    }

                    ex = ex.InnerException;
                }

                // 寫入檔案 /yyyyMMdd/Exception_yyyyMMddHH.txt
                var configured = AppSettings.ExceptionLogPath; // 可能是相對或絕對
                if (string.IsNullOrWhiteSpace(configured))
                {
                    return;
                }


                // 1) 解析最終根路徑：相對路徑 → 以 ContentRoot 為基準
                var baseDir = _contentRootPath ?? AppContext.BaseDirectory;
                var baseRoot = Path.GetFullPath(AppSettings.ExceptionLogPath, baseDir);

                // 2) 組檔名：/yyyyMMdd/Exception_yyyyMMddHH.txt
                var now = DateTime.Now;
                var logFolder = Path.Combine(baseRoot, now.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                Directory.CreateDirectory(logFolder);

                var logFileName = Path.Combine(logFolder, $"Exception_{now:yyyyMMddHH}.txt");
                
                var logFileName0 = Path.Combine(
                    exceptionLogPath,
                    DateTime.Now.ToString("yyyyMMdd"),
                    $"Exception_{DateTime.Now:yyyyMMddHH}.txt");

                var logDir = Path.GetDirectoryName(logFileName)!;
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var appendMessage = new StringBuilder()
                    .AppendLine("### 紀錄開始 ###")
                    .AppendLine($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}]")
                    .AppendLine(errorString.ToString())
                    .AppendLine("### 紀錄結束 ###")
                    .AppendLine();

                using var writer = new StreamWriter(logFileName, append: true, Encoding.UTF8);
                writer.WriteLine(appendMessage.ToString());
            }
            catch (Exception ex)
            {
                // 失敗就不記錄錯誤
            }
        }

        /// <summary>
        /// 取得用戶端的IP。
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static string GetClientIpAddress(HttpRequest request)
        {
            // 1) X-Real-IP
            if (request.Headers.TryGetValue("X-Real-IP", out StringValues xri) &&
                IPAddress.TryParse(xri.ToString(), out var ipFromXri))
            {
                return NormalizeIp(ipFromXri);
            }

            // 2) X-Forwarded-For（可能多個，取第一個）
            if (request.Headers.TryGetValue("X-Forwarded-For", out StringValues xff))
            {
                var first = xff.ToString().Split(',')[0].Trim();
                if (IPAddress.TryParse(first, out var ipFromXff))
                {
                    return NormalizeIp(ipFromXff);
                }
            }

            // 3) 連線位址
            var remote = request.HttpContext.Connection.RemoteIpAddress;
            return NormalizeIp(remote);
        }

        /// <summary>
        /// 正規化IP
        /// </summary>
        /// <param name="ip">IP物件</param>
        /// <returns>IP文字</returns>
        private static string NormalizeIp(IPAddress? ip)
        {
            if (ip is null) return string.Empty;

            // ::1 → 127.0.0.1
            if (IPAddress.IsLoopback(ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                return "127.0.0.1";

            // 僅在 IPv4-mapped IPv6（::ffff:127.0.0.1）才 MapToIPv4
            if (ip.IsIPv4MappedToIPv6)
                return ip.MapToIPv4().ToString();

            return ip.ToString(); // 其他情況維持原樣（可能是 IPv4 或原生 IPv6）
        }

        /// <summary>
        /// Object轉換成Dictionary格式
        /// </summary>
        /// <typeparam name="T">型態</typeparam>
        /// <param name="obj">Object物件</param>
        /// <returns>Dictionary格式資料</returns>
        public static Dictionary<string, object?> ToDictionary<T>(T obj)
        {
            var result = new Dictionary<string, object?>();

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead);

            foreach (var prop in props)
            {
                var value = prop.GetValue(obj);

                if (value is DateTime dt)
                {
                    result[prop.Name] = dt.ToString("yyyy-MM-dd");
                }
                else if (value is DateTime?)
                {
                    var nullable = (DateTime?)value;
                    result[prop.Name] = nullable.HasValue ? nullable.Value.ToString("yyyy-MM-dd") : null;
                }
                else
                {
                    result[prop.Name] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// 檢查檔名副檔名是否合法(大小寫不敏感)
        /// </summary>
        public static bool IsValidFileExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AppSettings.AllowedExtensions.Contains(extension);
        }

        /// <summary>
        /// 將資料轉換成表格
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="headers">資料表頭</param>
        /// <returns></returns>
        public static System.Data.DataTable ToDataTable(IEnumerable<dynamic> data, Dictionary<string, string> headers = null)
        {
            var table = new System.Data.DataTable();
            if (!data.Any()) return table;

            if (headers == null)
            {
                // Use property names from the first item in the data collection
                foreach (string property in ((IDictionary<string, object>)data.First()).Keys)
                {
                    table.Columns.Add(property);
                }
            }
            else
            {
                // Use the headers dictionary
                foreach (var header in headers)
                {
                    // Add the column using the custom header name (value) and map it to the original key (key)
                    table.Columns.Add(header.Value);
                }
            }

            int index = 1; // 用來產生序號的變數

            // Populate the rows
            foreach (var row in data)
            {
                var dataRow = table.NewRow();
                var dictRow = (IDictionary<string, object>)row;

                if (headers == null)
                {
                    // Map values directly when headers are not provided
                    foreach (var kvp in dictRow)
                    {
                        dataRow[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                }
                else
                {
                    // Map values according to the headers dictionary
                    foreach (var header in headers)
                    {
                        if (header.Key == "RowNum")
                        {
                            dataRow[header.Value] = index++;
                        }

                        else if (dictRow.TryGetValue(header.Key, out object? value))
                        {
                            if (value is DateTime dt)
                            {
                                dataRow[header.Value] = dt.ToString("yyyy/M/d");
                            }
                            else
                            {
                                dataRow[header.Value] = value ?? DBNull.Value;
                            }
                        }
                        else
                        {
                            dataRow[header.Value] = DBNull.Value; // Handle missing keys
                        }
                    }
                }

                table.Rows.Add(dataRow);
            }

            return table;
        }

        /// <summary>
        /// 交換日期；若date1>date2則交換
        /// </summary>
        /// <param name="date1">日期1</param>
        /// <param name="date2">日期2</param>
        public static (DateTime? Start, DateTime? End) GetOrderedDates(DateTime? date1, DateTime? date2)
        {
            if (date1 != null && date2 != null)
            {
                if (date1 > date2)
                {
                    return (date2, date1);
                }
                return (date1, date2);
            }

            return (date1, date2);
        }

        /// <summary>
        /// 交換數字；若 num1 > num2 則交換
        /// </summary>
        /// <typeparam name="T">可比較的數值型別（如 int、decimal、double）</typeparam>
        /// <param name="num1">數字1</param>
        /// <param name="num2">數字2</param>
        /// <returns>回傳 (較小, 較大)</returns>
        public static (T? Min, T? Max) GetOrderedNumbers<T>(T? num1, T? num2) where T : struct, IComparable<T>
        {
            if (num1.HasValue && num2.HasValue)
            {
                if (num1.Value.CompareTo(num2.Value) > 0)
                {
                    return (num2, num1);
                }
                return (num1, num2);
            }

            return (num1, num2);
        }

        /// <summary>
        /// 判斷日期A是否大於日期B（支援nullable）
        /// </summary>
        /// <param name="a">日期A</param>
        /// <param name="b">日期B</param>
        /// <returns>true：A大於B，false：A小於B</returns>
        public static bool IsDateAGreaterOrEqualThanB(DateTime? a, DateTime? b)
        {
            if (!a.HasValue || !b.HasValue)
                return false;

            return a.Value >= b.Value;
        }

        /// <summary>
        /// 取得小寫文字
        /// </summary>
        /// <param name="s">文字</param>
        /// <returns>小寫文字</returns>
        public static string Norm(string? s)
        {
            return (s ?? string.Empty).Trim().ToLowerInvariant();
        }

        /// <summary>
        ///  將像素轉 EMU（Open XML 使用的單位）；1px(96dpi)=9525 EMU
        /// </summary>
        /// <param name="px">像素值</param>
        /// <returns>EMU值</returns>
        public static long PxToEmu(int px) => px * 9525L;




    }
}
