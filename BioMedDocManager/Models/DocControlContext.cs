using ClosedXML.Excel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
namespace BioMedDocManager.Models;


public partial class DocControlContext : DbContext
{

    IConfiguration _config;

    public DocControlContext(DbContextOptions<DocControlContext> options)
        : base(options)
    {
    }

    public DocControlContext()
    {
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _config.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }


    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<DocControlMaintable> DocControlMaintables { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<User>()
        .ToTable("user")
        .HasKey(u => u.Id);

        modelBuilder.Entity<Role>()
            .ToTable("role")
            .HasKey(r => r.Id);

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_role");

            // 複合主鍵
            entity.HasKey(e => new { e.UserId, e.RoleId });

            // 關聯 User
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId);

            // 關聯 Role
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId);
        });

        // 範例資料表：文件領用紀錄表
        modelBuilder.Entity<DocControlMaintable>(entity =>
        {
            entity.HasKey(e => e.IdNo);

            entity.ToTable("doc_control_maintable");

            entity.Property(e => e.IdNo)
                .HasMaxLength(50)
                .HasComment("文件編號")
                .HasColumnName("id_no");
            entity.Property(e => e.DateTime).HasColumnName("date_time");

            entity.Property(e => e.DocVer)
                .HasMaxLength(50)
                .HasComment("版本")
                .HasColumnName("doc_ver");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasComment("工號")
                .HasColumnName("id");

            entity.Property(e => e.InTime).HasColumnName("in_time");

            entity.Property(e => e.Name)
                .HasComment("紀錄名稱")
                .HasColumnName("name");

            entity.Property(e => e.OriginalDocNo)
                .HasComment("表單編號")
                .HasColumnName("original_doc_no");

            entity.HasOne(e => e.Person)
                .WithMany()
                .HasForeignKey(e => e.Id)
                .HasPrincipalKey(u => u.UserName);

            entity.Property(e => e.ProjectName)
                .HasMaxLength(50)
                .HasComment("專案代碼")
                .HasColumnName("project_name");

            entity.Property(e => e.Purpose)
                .HasComment("目的")
                .HasColumnName("purpose");

            entity.Property(e => e.RejectReason)
                .HasComment("註銷原因")
                .HasColumnName("reject_reason");

            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasComment("文件類別")
                .HasColumnName("type");

            entity.Property(e => e.UnuseTime).HasColumnName("unuse_time");

            entity.Property(e => e.IsConfidential)
               .HasColumnName("is_confidential")
               .HasComment("是否機密");

            entity.Property(e => e.IsSensitive)
                .HasColumnName("is_sensitive")
                .HasComment("是否機敏");

            entity.Property(e => e.InTimeModifyAt)
                .HasColumnType("datetime")
                .HasColumnName("in_time_modify_at")
                .HasComment("入庫時間異動時間");

            entity.Property(e => e.UnuseTimeModifyAt)
                .HasColumnType("datetime")
                .HasColumnName("unuse_time_modify_at")
                .HasComment("註銷時間異動時間");

            entity.Property(e => e.InTimeModifyBy)
                .HasMaxLength(50)
                .IsUnicode(true)
                .HasComment("入庫時間異動人員")
                .HasColumnName("in_time_modify_by");

            entity.Property(e => e.UnuseTimeModifyBy)
                .HasMaxLength(50)
                .IsUnicode(true)
                .HasComment("註銷時間異動人員")
                .HasColumnName("unuse_time_modify_by");

            entity.HasOne(e => e.InTimeModifyUser)
                .WithMany()
                .HasForeignKey(e => e.InTimeModifyBy)
                .HasPrincipalKey(u => u.UserName);

            entity.HasOne(e => e.UnuseTimeModifyUser)
                .WithMany()
                .HasForeignKey(e => e.UnuseTimeModifyBy)
                .HasPrincipalKey(u => u.UserName);

        });
    }


    public async Task<(List<T> Items, int TotalCount)> BySqlGetPagedWithCountAsync<T>(
        string selectPart, string orderByPart, int pageNumber = 0, int pageSize = 0, object parameters = null)
    {
        if (string.IsNullOrWhiteSpace(orderByPart))
        {
            orderByPart = "ORDER BY (SELECT NULL)";
        }

        var hasPaging = pageNumber > 0 && pageSize > 0;
        var rowOffset = (pageNumber - 1) * pageSize;

        var baseCte = $"WITH BASE AS ({selectPart})";

        var pagedSql = $@"
            {baseCte}
            , RowNumbers AS (
                SELECT ROW_NUMBER() OVER ({orderByPart}) AS RowNum, * FROM BASE
            )
            SELECT * FROM RowNumbers
            ORDER BY RowNum
            {(hasPaging ? $"OFFSET {rowOffset} ROWS FETCH NEXT {pageSize} ROWS ONLY" : string.Empty)};";

        var countSql = $@"
        {baseCte}
        SELECT COUNT(1) FROM BASE;";

        var sql = $"{pagedSql}\n{countSql}";

        Console.WriteLine($"execute query sql: {Regex.Replace(sql.Replace(Environment.NewLine, " "), @"\s+", " ").Trim()}");

        var conn = this.Database.GetDbConnection();
        using var multi = await conn.QueryMultipleAsync(sql, parameters);

        var items = (await multi.ReadAsync<T>()).ToList();
        var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

        return (items, totalCount);
    }



    public async Task<MemoryStream> ExportToExcelAsync(string sql, Dictionary<string, string> headers, object parameters = null, string sheetName = "Sheet1", bool autoFilters = true)
    {
        using var connection = this.Database.GetDbConnection();
        await connection.OpenAsync();

        var data = await connection.QueryAsync(sql, parameters);


        if (data.Any())
        {
            //有查詢結果才可以匯出
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            if (data is not null)
            {
                var dataTable = ToDataTable(data, headers);
                worksheet.Cell(1, 1).InsertTable(dataTable);

                // Auto-adjust column widths
                worksheet.Columns().AdjustToContents();
            }

            if (!autoFilters)
            {
                //不要產生預設的 Excel 篩選 (Default 有)
                worksheet.AutoFilter.Clear();
            }

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        throw new FileNotFoundException("No data to export");
    }


    private System.Data.DataTable ToDataTable(IEnumerable<dynamic> data, Dictionary<string, string> headers = null)
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
}
