using ClosedXML.Excel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
namespace BioMedDocManager.Models;

/// <summary>
/// 這一半用來寫 Dapper/ SQL 查詢邏輯
/// </summary>
public partial class DocControlContext : DbContext
{
    /*
    private List<dynamic> ExecuteSqlQueryDynamic2(string sqlQuery, CommandType type, int pageNumber = 0, int pageSize = 10, List<SqlParameter> parameters = null)
    {
        var result = new List<dynamic>();
        using (var conn = this.Database.GetDbConnection())
        {
            conn.Open();

            try
            {
                // Convert SqlParameter to DynamicParameters for Dapper
                var dynamicParameters = new DynamicParameters();
                parameters?.ForEach(param => dynamicParameters.Add(param.ParameterName, param.Value));

                // Determine if a transaction exists and execute accordingly
                var transaction = this.Database.CurrentTransaction?.GetDbTransaction();
                var queryResult = conn.Query<dynamic>(sqlQuery, dynamicParameters, transaction).Skip(pageNumber * pageSize).Take(pageSize).ToList();



                // Add a manual rownum property to the results
                for (int i = 0; i < queryResult.Count; i++)
                {
                    // Add a 'rownum' field to each item in the result
                    ((IDictionary<string, object>)queryResult[i])["rownum"] = (pageNumber * pageSize) + i + 1;
                }

                result = queryResult;
            }
            catch (Exception ex)
            {
                // Handle error (e.g., logging)
                // Logger.Error(ex.ToString());
            }
            // Connection is automatically closed at the end of the using block
        }

        return result;
    }
    */
    /*
    public async Task<(List<dynamic> Items, int TotalCount)> BySqlPageByMemory(string selectPart, string orderByPart, int pageNumber = 0, int pageSize = 0, object parameters = null)
    {
        if (orderByPart == null)
        {
            orderByPart = "ORDER BY (SELECT NULL) -- Dummy order to satisfy the requirement";
        }

        var sql = $@"{selectPart} {orderByPart}";

        var conn = this.Database.GetDbConnection();
        using var multi = await conn.QueryMultipleAsync(sql, parameters);

        var items = (await multi.ReadAsync<dynamic>()).ToList();
        var totalCount = items.Count;

        // Apply paging in-memory
        items = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return (items, totalCount);
    }
    */
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
    
    /*
    private string RewriteOrderByWithNullsLast(string orderByPart)
    {
        if (string.IsNullOrWhiteSpace(orderByPart))
            return "ORDER BY (SELECT NULL)";

        // Extract everything after "ORDER BY"
        var orderClause = orderByPart.Trim().Substring("ORDER BY".Length).Trim();

        var columns = orderClause.Split(',')
            .Select(col =>
            {
                var parts = col.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var column = parts[0];
                var direction = parts.Length > 1 ? parts[1].ToUpper() : "ASC";

                // ASC: use high-value fallback like 'ZZZ', DESC: use low like '000'
                var fallback = direction == "DESC" ? "'ZZZ'" : "'000'";
                var casted = $"ISNULL(CAST({column} AS VARCHAR), {fallback})";

                return $"{casted} {direction}";
            });

        return "ORDER BY " + string.Join(", ", columns);
    }
    */
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
    /*
    [Obsolete]
    public async Task<MemoryStream> ExportToExcelAsync(string sql, object parameters = null)
    {
        using var connection = this.Database.GetDbConnection();
        await connection.OpenAsync();

        var data = await connection.QueryAsync(sql, parameters);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        if (data is not null)
        {
            var dataTable = ToDataTable(data);
            worksheet.Cell(1, 1).InsertTable(dataTable);

            // Auto-adjust column widths
            worksheet.Columns().AdjustToContents();
        }


        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
    */

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




/*
public static class StringExtensions
{
    /// <summary>
    /// ClassName => class_name
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToSnakeCase(this string str)
    {
        // If the string is already in snake_case, return as-is
        if (Regex.IsMatch(str, @"^[a-z]+(_[a-z]+)*$"))
            return str;

        // Convert PascalCase or camelCase to snake_case
        return Regex.Replace(str, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }
}
*/
/*
public static class TableNameHelper
{
    public static string GetTableName<T>()
    {
        var tableName = typeof(T)
                        .GetCustomAttribute<TableAttribute>()?
                        .Name ?? typeof(T).Name.ToSnakeCase(); // Fallback to class name if TableAttribute is not set

        return tableName.ToSnakeCase();
    }
}
*/
/*
public static class SqlHelper
{
    // Get the column names and primary key information from the model
    public static (List<string> columns, string primaryKeyColumn, List<string> allColumns) GetColumnInfo<T>() where T : class
    {
        var columns = new List<string>();
        var primaryKeyColumn = string.Empty;
        var allColumns = new List<string>();

        // Get all properties of T
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            // Check if property has the [Column] attribute and get the column name
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            var columnName = columnAttr != null ? columnAttr.Name : property.Name;
            allColumns.Add(columnName);

            // Check if property has the [Key] attribute and assign it as the primary key column
            var keyAttr = property.GetCustomAttribute<KeyAttribute>();
            if (keyAttr != null)
            {
                primaryKeyColumn = columnName;
            }

            columns.Add(columnName);
        }

        return (columns, primaryKeyColumn, allColumns);
    }
    /*
    // Generate Update Query and Parameters
    public static (string sqlcmd, object parameters) GenerateUpdateQuery<T>(T model) where T : class
    {
        var (columns, primaryKeyColumn, allColumns) = SqlHelper.GetColumnInfo<T>();

        if (string.IsNullOrEmpty(primaryKeyColumn))
        {
            //意思是 這種 table 只能用 現況資料查詢 設定為新的值
            throw new InvalidOperationException("未偵測到主鍵 PK。您必須依賴所有欄位作為 WHERE 子句的條件。");
        }

        var setClauses = columns.Select(c => $"{c} = @{c}").ToList();
        var whereClauses = allColumns.Select(c => $"{c} = @{c}").ToList();

        var sqlcmd = $"UPDATE {TableNameHelper.GetTableName<T>} SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", whereClauses)}";

        // Prepare parameters dictionary
        var parameters = columns.ToDictionary(c => c, c => model.GetType().GetProperty(c)?.GetValue(model));

        return (sqlcmd, parameters);
    }

    // Generate Update Query and Parameters (with where and set model)
    public static (string sqlcmd, object parameters) GenerateUpdateQuery<T>(T whereModel, T setModel, DbConnection connection = null) where T : class
    {
        var (columns, primaryKeyColumn, allColumns) = SqlHelper.GetColumnInfo<T>();

        var setClauses = columns
            .Where(c => setModel.GetType().GetProperty(c) != null)
            .Select(c => $"{c} = @{c}")
            .ToList();

        var whereClauses = allColumns
            .Where(c => whereModel.GetType().GetProperty(c) != null)
            .Select(c => $"{c} = @{c}")
            .ToList();

        var sqlcmd = $"UPDATE {TableNameHelper.GetTableName<T>} SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", whereClauses)}";

        var parameters = new Dictionary<string, object>();

        foreach (var column in columns)
        {
            var propertyValue = setModel.GetType().GetProperty(column)?.GetValue(setModel);
            if (propertyValue != null)
                parameters.Add(column, propertyValue);

            propertyValue = whereModel.GetType().GetProperty(column)?.GetValue(whereModel);
            if (propertyValue != null)
                parameters.Add(column, propertyValue);
        }

        if (connection != null)
        {
            //用當前的 DB Connection 確認是否能更新唯一筆資料
            var countSql = $"SELECT COUNT(*) FROM {TableNameHelper.GetTableName<T>} WHERE {string.Join(" AND ", whereClauses)}";
            var rowCount = connection.QuerySingle<int>(countSql, whereModel);

            if (rowCount == 0)
            {
                //沒找到
                throw new InvalidOperationException("No rows found for the given condition.");
            }
            else if (rowCount > 1)
            {
                //不應該影響超過一筆資料
                throw new InvalidOperationException("Multiple rows found for the given condition.");
            }

        }

        return (sqlcmd, parameters);
    }

    // Generate Insert Query and Parameters
    public static (string sqlcmd, object parameters) GenerateInsertQuery<T>(T model) where T : class
    {
        var (columns, _, _) = SqlHelper.GetColumnInfo<T>();

        var columnNames = string.Join(", ", columns);
        var values = string.Join(", ", columns.Select(c => $"@{c}"));

        // Get the table name from the TableAttribute
        var tableName = TableNameHelper.GetTableName<T>;



        var sqlcmd = $"INSERT INTO {tableName} ({columnNames}) VALUES ({values})";

        // Prepare parameters dictionary
        var parameters = columns.ToDictionary(c => c, c => model.GetType().GetProperty(c)?.GetValue(model));

        return (sqlcmd, parameters);
    }

    // Generate Delete Query and Parameters
    public static (string sqlcmd, object parameters) GenerateDeleteQuery<T>(T model) where T : class
    {
        var (columns, primaryKeyColumn, allColumns) = SqlHelper.GetColumnInfo<T>();

        if (string.IsNullOrEmpty(primaryKeyColumn))
        {
            //throw new InvalidOperationException("No primary key detected. You must rely on all columns for WHERE clause.");
        }

        var whereClauses = allColumns.Select(c => $"{c} = @{c}").ToList();

        var sqlcmd = $"DELETE FROM {TableNameHelper.GetTableName<T>} WHERE {string.Join(" AND ", whereClauses)}";

        // Prepare parameters dictionary
        var parameters = allColumns.ToDictionary(c => c, c => model.GetType().GetProperty(c)?.GetValue(model));

        return (sqlcmd, parameters);
    }

    // Generate Select Query and Parameters
    public static (string sqlcmd, object parameters) GenerateSelectQuery<T>(object parameters = null) where T : class
    {
        var (columns, _, _) = SqlHelper.GetColumnInfo<T>();

        var sqlcmd = $"SELECT {string.Join(", ", columns)} FROM {TableNameHelper.GetTableName<T>}";

        var parameterDict = new Dictionary<string, object>();

        if (parameters != null)
        {
            var whereClauses = columns.Select(c => $"{c} = @{c}");
            sqlcmd += " WHERE " + string.Join(" AND ", whereClauses);

            // Prepare parameters dictionary
            var properties = parameters.GetType().GetProperties();
            foreach (var property in properties)
            {
                parameterDict.Add(property.Name, property.GetValue(parameters));
            }
        }

        return (sqlcmd, parameterDict);
    }
    
}
*/
