using System.Text;
using System.Text.Json;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dapper;

namespace UpdateSupplierTaxId
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {

            Console.WriteLine("🔄 Starting Tax ID Sync...");
            var builder = WebApplication.CreateBuilder(args);

            // 💥 Suppress noisy HttpClient logs
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

            // Minimal service setup
            builder.Services.AddDbContext<DocControlContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHttpClient();

            var app = builder.Build();

            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            var db = services.GetRequiredService<DocControlContext>();
            var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

            await RunTaxIdSync(db, httpClientFactory.CreateClient());

            Console.WriteLine("✅ Tax ID Sync complete.");
            return;

        }

        private static async Task RunTaxIdSync(DocControlContext db, HttpClient http)
        {
            //  var cleanSupplierSql = @"
            //    UPDATE qualified_suppliers
            //    SET supplier_name = REPLACE(REPLACE(supplier_name, ' ', ''), '　', '')
            //    WHERE supplier_name LIKE '% %' OR supplier_name LIKE '%　%';
            //";

            //            using var connection = db.Database.GetDbConnection();
            //            await connection.ExecuteAsync(cleanSupplierSql);


            var companiesToUpdate = db.QualifiedSuppliers
    .Where(c => string.IsNullOrEmpty(c.SupplierNo))
    .ToList();

            var throttler = new SemaphoreSlim(5); // Limit to 5 concurrent requests
            var counterLock = new object(); // To safely increment shared count
            int count = 0;

            //如果查不到公司代表服務不正常
            bool result = await RunLookup(http, new QualifiedSupplier() { SupplierName = "三趨" });

            if (!result)
            {
                Console.WriteLine("❌ API service is currently unavailable. Please try again later.");
                return;
            }


            var tasks = companiesToUpdate.Select(async supplier =>
            {
                await throttler.WaitAsync();
                try
                {

                    Console.WriteLine($"🔍 Querying: {supplier.SupplierName}");

                    try
                    {
                        var result = await RunLookup(http, supplier); // change to not rely on count directly
                        if (result)
                        {
                            lock (counterLock)
                            {
                                count++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"💥 Error syncing {supplier.SupplierName}: {ex.Message}");
                    }
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);

            Console.WriteLine($"🎉 Parallel sync complete. Total successful updates: {count}");

            //commit to DB
            await db.SaveChangesAsync();

            StringBuilder sqlFile = new();
            var suppliers = db.QualifiedSuppliers
                .Where(c => !string.IsNullOrEmpty(c.SupplierNo))
                .GroupBy(c => c.SupplierName)
                .Select(g => new
                {
                    supplier_name = g.Key,
                    supplier_no = g.First().SupplierNo // assumes all supplier_no are consistent
                })
                .OrderBy(x => x.supplier_no)
                .ToList();

            foreach (var supplier in suppliers)
            {
                var sql = $"UPDATE qualified_suppliers SET supplier_no = '{supplier.supplier_no}' WHERE supplier_name = '{supplier.supplier_name}';";
                sqlFile.AppendLine(sql);
            }

            // Get the base directory of the currently running app
            var baseDir = AppContext.BaseDirectory;

            // Traverse up to reach the project root (assuming you're in /bin/Debug/netX.Y/)
            var projectRoot = (Directory.GetParent(baseDir) // netX.Y
                                    ?.Parent                // Debug
                                    ?.Parent                // bin
                                    ?.Parent?.FullName) ?? throw new Exception("Could not resolve project root directory.");     // Project root (BioMedDocManager)

            // Construct full path to DBScript folder
            var dbScriptFolder = Path.Combine(projectRoot, "DBScript");
            Directory.CreateDirectory(dbScriptFolder);

            // Timestamp for filename (e.g., 20250625_2134)
            var timestamp = DateTime.Now.ToString("yyyyMMdd");

            // Final SQL file path with timestamp
            var sqlFilePath = Path.Combine(dbScriptFolder, $"{timestamp}_UpdateSupplierTaxId.sql");

            // Write to file
            await File.WriteAllTextAsync(sqlFilePath, sqlFile.ToString(), Encoding.UTF8);

            Console.WriteLine($"🎉 Done. {count} suppliers updated.");
        }

        private static async Task<bool> RunLookup(HttpClient http, QualifiedSupplier supplier)
        {
            var cleanQueryString = supplier.SupplierName;

            var encoded = Uri.EscapeDataString(cleanQueryString.Replace(" ", "").Replace("　", ""));


            // var url = $"https://opendata.vip/api/CompanyData/{encoded}";

            var url = $"https://opendata.vip/data/company?keyword={encoded}";

            var response = await http.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
                {
                    Console.WriteLine($"⚠️ Output array not found or malformed for {supplier.SupplierName}");
                }
                else
                {
                    var first = output.EnumerateArray().FirstOrDefault();

                    if (first.ValueKind == JsonValueKind.Undefined)
                    {
                        Console.WriteLine($"⚠️ No match found in output for {supplier.SupplierName}");
                    }
                    else
                    {
                        supplier.SupplierNo = first.GetProperty("Business_Accounting_NO").GetString();
                        Console.WriteLine($"✅ Updated: {supplier.SupplierName} -> {supplier.SupplierNo}");
                        return true;
                    }
                }
            }
            else
            {
                Console.WriteLine($"❌ Failed API call for {supplier.SupplierName}: {response.StatusCode}");
            }

            await Task.Delay(5000); // Be kind to the API
            return false;
        }


    }
}
