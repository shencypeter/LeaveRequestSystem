using ClosedXML.Excel;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Models
{
    public partial class DocControlContext : DbContext
    {
        /*
        /// <summary>
        /// 供應商清冊List
        /// </summary>    
        /// <param name="pageNumber">如果 0, 不分頁</param>
        /// <param name="pageSize">如果0, 不分頁</param>    
        /// <returns></returns>
        [Obsolete]
        public async Task<(List<T> Items, int TotalCount)> GetQualifiedSuppliersList<T>(Dictionary<string, string?> searchParams, string orderByPart, int pageNumber = 0, int pageSize = 0)
        {
            if (string.IsNullOrWhiteSpace(orderByPart))
            {
                orderByPart = "ORDER BY (SELECT NULL)"; // Ensures ROW_NUMBER() always has an ORDER BY
            }

            string selectPart = @"select * from qualified_suppliers where 1=1";

            List<string> conditions = new();
            DynamicParameters parameters = new();

            if (searchParams.TryGetValue("ddlQualifiedStatus", out var qualifiedStatus) && !string.IsNullOrEmpty(qualifiedStatus))
            {
                conditions.Add("and reassess_result = @qualifiedStatus");
                parameters.Add("qualifiedStatus", qualifiedStatus);
            }

            if (searchParams.TryGetValue("txtReassessDate", out var reassessDateStart) && !string.IsNullOrEmpty(reassessDateStart))
            {
                conditions.Add("and reassess_date >= @ReassessDateStart");
                parameters.Add("reassessDateStart", reassessDateStart);
            }

            if (searchParams.TryGetValue("txtReassessDateEnd", out var reassessDateEnd) && !string.IsNullOrEmpty(reassessDateEnd))
            {
                conditions.Add("and reassess_date <= @ReassessDateEnd");
                parameters.Add("ReassessDateEnd", reassessDateEnd);
            }

            if (searchParams.TryGetValue("txtSupplierNo", out var supplierNo) && !string.IsNullOrEmpty(supplierNo))
            {
                conditions.Add("and supplier_no LIKE '%' + @supplierno + '%'");
                parameters.Add("supplierno", supplierNo);
            }

            if (searchParams.TryGetValue("txtSupplierName", out var supplierName) && !string.IsNullOrEmpty(supplierName))
            {
                conditions.Add("and supplier_name LIKE '%' + @suppliername + '%'");
                parameters.Add("suppliername", supplierName);
            }

            string whereClause = string.Join(" ", conditions);
            selectPart += $" {whereClause}";
            //string orderByPart = @"ORDER BY supplier_name";

            //如果轉成寫好的下面就不用自己抄寫了
            return await BySqlGetPagedWithCountAsync<T>(selectPart, orderByPart, pageNumber, pageSize, parameters);
        }

        /*
        /// <summary>
        /// 取號表單編號List
        /// </summary>    
        /// <param name="pageNumber">如果 0, 不分頁</param>
        /// <param name="pageSize">如果0, 不分頁</param>    
        /// <returns></returns>
        [Obsolete]
        public async Task<(List<T> Items, int TotalCount)> GetMaxVerIssueTableList<T>(Dictionary<string, string?> searchParams, string orderByPart, int pageNumber = 0, int pageSize = 0)
        {
            if (string.IsNullOrWhiteSpace(orderByPart))
            {
                orderByPart = "ORDER BY (SELECT NULL)"; // Ensures ROW_NUMBER() always has an ORDER BY
            }

            string selectPart = @" select [name] Name,original_doc_no OriginalDocNo,max(doc_ver) DocVer from issue_table 
                    where 1=1 ";

            List<string> conditions = new();
            DynamicParameters parameters = new();

            if (searchParams.TryGetValue("txtStartDate", out var sdate) && !string.IsNullOrEmpty(sdate))
            {
                conditions.Add("and issue_datetime <= @startDate");
                parameters.Add("startDate", sdate);
            }

            if (searchParams.TryGetValue("searchInput", out var keyword) && !string.IsNullOrEmpty(keyword))
            {
                conditions.Add("and original_doc_no like '%" + keyword + "%'");
                parameters.Add("searchInput", keyword);
            }
            else
            {
                conditions.Add("and original_doc_no like 'BMP-%' ");
            }

            string whereClause = string.Join(" ", conditions);
            selectPart += $" {whereClause}";

            selectPart += @" group by [name],original_doc_no ";


            //如果轉成寫好的下面就不用自己抄寫了
            return await BySqlGetPagedWithCountAsync<T>(selectPart, orderByPart, pageNumber, pageSize, parameters);
        }
        /*
        /// <summary>
        /// 文件查詢List
        /// </summary>    
        /// <param name="pageNumber">如果 0, 不分頁</param>
        /// <param name="pageSize">如果0, 不分頁</param>    
        /// <returns></returns>
        [Obsolete]
        public async Task<(List<T> Items, int TotalCount)> GetDocControlMaintableList<T>(Dictionary<string, string?> searchParams, string orderByPart, int pageNumber = 0, int pageSize = 0)
        {
            if (string.IsNullOrWhiteSpace(orderByPart))
            {
                orderByPart = "ORDER BY (SELECT NULL)"; // Ensures ROW_NUMBER() always has an ORDER BY
            }

            string selectPart = @"select * from doc_control_maintable where 1=1";

            List<string> conditions = new();
            DynamicParameters parameters = new();

            if (searchParams.TryGetValue("ddlQualifiedStatus", out var qualifiedStatus) && !string.IsNullOrEmpty(qualifiedStatus))
            {
                conditions.Add("and reassess_result = @qualifiedStatus");
                parameters.Add("qualifiedStatus", qualifiedStatus);
            }

            if (searchParams.TryGetValue("txtReassessDate", out var reassessDateStart) && !string.IsNullOrEmpty(reassessDateStart))
            {
                conditions.Add("and reassess_date >= @ReassessDateStart");
                parameters.Add("reassessDateStart", reassessDateStart);
            }

            if (searchParams.TryGetValue("txtReassessDateEnd", out var reassessDateEnd) && !string.IsNullOrEmpty(reassessDateEnd))
            {
                conditions.Add("and reassess_date <= @ReassessDateEnd");
                parameters.Add("ReassessDateEnd", reassessDateEnd);
            }

            if (searchParams.TryGetValue("txtSupplierNo", out var supplierNo) && !string.IsNullOrEmpty(supplierNo))
            {
                conditions.Add("and supplier_no LIKE '%' + @supplierno + '%'");
                parameters.Add("supplierno", supplierNo);
            }

            if (searchParams.TryGetValue("txtSupplierName", out var supplierName) && !string.IsNullOrEmpty(supplierName))
            {
                conditions.Add("and supplier_name LIKE '%' + @suppliername + '%'");
                parameters.Add("suppliername", supplierName);
            }

            string whereClause = string.Join(" ", conditions);
            selectPart += $" {whereClause}";

            //string orderByPart = @"ORDER BY supplier_name";

            //如果轉成寫好的下面就不用自己抄寫了
            return await BySqlGetPagedWithCountAsync<T>(selectPart, orderByPart, pageNumber, pageSize, parameters);
        }

        /*
        /// <summary>
        /// 驗收清冊List
        /// </summary>    
        /// <param name="pageNumber">如果 0, 不分頁</param>
        /// <param name="pageSize">如果0, 不分頁</param>    
        /// <returns></returns>
        [Obsolete]
        public async Task<(List<T> Items, int TotalCount)> GetPurchaseRecordsList<T>(Dictionary<string, string?> searchParams, string orderByPart, int pageNumber = 0, int pageSize = 0)
        {
            if (string.IsNullOrWhiteSpace(orderByPart))
            {
                orderByPart = "ORDER BY (SELECT NULL)"; // Ensures ROW_NUMBER() always has an ORDER BY
            }

            string selectPart = @"select * from purchase_records where 1=1";

            List<string> conditions = new();
            DynamicParameters parameters = new();

            if (searchParams.TryGetValue("txtRequester", out var requester) && !string.IsNullOrEmpty(requester))
            {
                conditions.Add("and requester LIKE '%' + @requester + '%'");
                parameters.Add("requester", requester);
            }

            if (searchParams.TryGetValue("txtRequestNo", out var request_no) && !string.IsNullOrEmpty(request_no))
            {
                conditions.Add("and request_no LIKE '%' + @request_no + '%'");
                parameters.Add("request_no", request_no);
            }

            if (searchParams.TryGetValue("txtPurchaser", out var purchaser) && !string.IsNullOrEmpty(purchaser))
            {
                conditions.Add("and purchaser LIKE '%' + @purchaser + '%'");
                parameters.Add("purchaser", purchaser);
            }

            if (searchParams.TryGetValue("txtReceiveNumber", out var receive_number) && !string.IsNullOrEmpty(receive_number))
            {
                conditions.Add("and receive_number LIKE '%' + @receive_number + '%'");
                parameters.Add("receive_number", receive_number);
            }

            if (searchParams.TryGetValue("txtReceivePerson", out var receive_person) && !string.IsNullOrEmpty(receive_person))
            {
                conditions.Add("and receive_person LIKE '%' + @receive_person + '%'");
                parameters.Add("receive_person", receive_person);
            }

            if (searchParams.TryGetValue("txtDeliveryDate_start", out var delivery_dateStar) && !string.IsNullOrEmpty(delivery_dateStar))
            {
                conditions.Add("and delivery_date >= @delivery_dateStar");
                parameters.Add("delivery_dateStar", delivery_dateStar);
            }

            if (searchParams.TryGetValue("txtDeliveryDate_end", out var delivery_dateEnd) && !string.IsNullOrEmpty(delivery_dateEnd))
            {
                conditions.Add("and delivery_date <= @delivery_dateEnd");
                parameters.Add("delivery_dateEnd", delivery_dateEnd);
            }

            if (searchParams.TryGetValue("txtVerifyPerson", out var verify_person) && !string.IsNullOrEmpty(verify_person))
            {
                conditions.Add("and verify_person LIKE '%' + @verify_person + '%'");
                parameters.Add("verify_person", verify_person);
            }

            if (searchParams.TryGetValue("txtVerifyDate_start", out var verify_dateStar) && !string.IsNullOrEmpty(verify_dateStar))
            {
                conditions.Add("and verify_date >= @verify_dateStar");
                parameters.Add("verify_dateStar", verify_dateStar);
            }

            if (searchParams.TryGetValue("txtVerifyDate_end", out var verify_dateEnd) && !string.IsNullOrEmpty(verify_dateEnd))
            {
                conditions.Add("and verify_date <= @verify_dateEnd");
                parameters.Add("verify_dateEnd", verify_dateEnd);
            }

            string whereClause = string.Join(" ", conditions);
            selectPart += $" {whereClause}";

            return await BySqlGetPagedWithCountAsync<T>(selectPart, orderByPart, pageNumber, pageSize, parameters);
        }

        /*
        /// <summary>
        /// 驗收商清冊Export Excel
        /// </summary>
        /// <returns></returns>
        /// 
        [Obsolete]
        public async Task<MemoryStream> PurchaseRecordsExportToExcelAsync(Dictionary<string, string?> searchParams, string orderByPart)
        {
            if (string.IsNullOrWhiteSpace(orderByPart))
            {
                orderByPart = "ORDER BY (SELECT NULL)"; // Ensures ROW_NUMBER() always has an ORDER BY
            }

            using var connection = this.Database.GetDbConnection();
            await connection.OpenAsync();

            #region sql
            string selectPart = @"select * from purchase_records where 1=1";

            List<string> conditions = new();
            DynamicParameters parameters = new();


            if (searchParams.TryGetValue("txtRequester", out var requester) && !string.IsNullOrEmpty(requester))
            {
                conditions.Add("and requester LIKE '%' + @requester + '%'");
                parameters.Add("requester", requester);
            }

            if (searchParams.TryGetValue("txtRequestNo", out var request_no) && !string.IsNullOrEmpty(request_no))
            {
                conditions.Add("and request_no LIKE '%' + @request_no + '%'");
                parameters.Add("request_no", request_no);
            }

            if (searchParams.TryGetValue("txtPurchaser", out var purchaser) && !string.IsNullOrEmpty(purchaser))
            {
                conditions.Add("and purchaser LIKE '%' + @purchaser + '%'");
                parameters.Add("purchaser", purchaser);
            }

            if (searchParams.TryGetValue("txtReceiveNumber", out var receive_number) && !string.IsNullOrEmpty(receive_number))
            {
                conditions.Add("and receive_number LIKE '%' + @receive_number + '%'");
                parameters.Add("receive_number", receive_number);
            }

            if (searchParams.TryGetValue("txtReceivePerson", out var receive_person) && !string.IsNullOrEmpty(receive_person))
            {
                conditions.Add("and receive_person LIKE '%' + @receive_person + '%'");
                parameters.Add("receive_person", receive_person);
            }

            if (searchParams.TryGetValue("txtDeliveryDate_start", out var delivery_dateStar) && !string.IsNullOrEmpty(delivery_dateStar))
            {
                conditions.Add("and delivery_date >= @delivery_dateStar");
                parameters.Add("delivery_dateStar", delivery_dateStar);
            }

            if (searchParams.TryGetValue("txtDeliveryDate_end", out var delivery_dateEnd) && !string.IsNullOrEmpty(delivery_dateEnd))
            {
                conditions.Add("and delivery_date <= @delivery_dateEnd");
                parameters.Add("delivery_dateEnd", delivery_dateEnd);
            }

            if (searchParams.TryGetValue("txtVerifyPerson", out var verify_person) && !string.IsNullOrEmpty(verify_person))
            {
                conditions.Add("and verify_person LIKE '%' + @verify_person + '%'");
                parameters.Add("verify_person", verify_person);
            }

            if (searchParams.TryGetValue("txtVerifyDate_start", out var verify_dateStar) && !string.IsNullOrEmpty(verify_dateStar))
            {
                conditions.Add("and verify_date >= @verify_dateStar");
                parameters.Add("verify_dateStar", verify_dateStar);
            }

            if (searchParams.TryGetValue("txtVerifyDate_end", out var verify_dateEnd) && !string.IsNullOrEmpty(verify_dateEnd))
            {
                conditions.Add("and verify_date <= @verify_dateEnd");
                parameters.Add("verify_dateEnd", verify_dateEnd);
            }

            string whereClause = string.Join(" ", conditions);
            selectPart += $" {whereClause}";

            //string orderByPart = @" ORDER BY supplier_name";

            var sql = selectPart + orderByPart;

            #endregion


            Dictionary<string, string> TableHeaders = new()
        {
            { "requester", "請購人" },
            { "receive_person", "驗收人" },
            { "request_no", "請購編號" },
            { "delivery_date", "收貨日期" },
            { "verify_date", "驗收日期" },
            { "product_name", "產品名稱" }
        };

            var data = await connection.QueryAsync(sql, parameters);
            if (data.Any())
            {
                //有查詢結果才可以匯出
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Sheet1");

                if (data is not null)
                {
                    var dataTable = ToDataTable(data, TableHeaders);
                    worksheet.Cell(1, 1).InsertTable(dataTable);

                    // Auto-adjust column widths
                    worksheet.Columns().AdjustToContents();
                }


                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return stream;
            }

            throw new FileNotFoundException("No data to export");
        }
        /*
        /// <summary>
        /// 再評估資料
        /// </summary>
        /// <returns></returns>
        public SupplierReassessment GetSupplierReassessmentDatas(Dictionary<string, string?> searchParams)
        {
            string sql = @"

select 
  a.product_class, 
  a.supplier_name, 
  b.supplier_class, 
  a.assess_date, 
  c.avg_grade grade, 
  a.assess_result 
from 
  (
    select 
      * 
    from 
      supplier_reassessment 
    where 
      1 = 1
  ) a 
  left join qualified_suppliers b on a.supplier_name = b.supplier_name 
  and a.product_class = b.product_class 
  left join (
    select 
      supplier_name, 
      product_class, 
      avg(grade) as avg_grade 
    from 
      (
        select 
          * 
        from 
          purchase_records 
        where 
          receipt_status is not null 
          and grade is not null
      ) as Src 
    group by 
      supplier_name, 
      product_class
  ) c on a.supplier_name = c.supplier_name 
  and a.product_class = c.product_class 
  left join (
    select 
      * 
    from 
      purchase_records 
    where 
      receipt_status is not null 
      and grade is not null
  ) d on a.supplier_name = d.supplier_name 
  and a.product_class = d.product_class 
  and a.assess_date = d.assess_date 
where 
  1 = 1 ";

            List<string> conditions = new();
            DynamicParameters parameters = new();

            if (!string.IsNullOrEmpty(searchParams["supplier_name"]))
            {
                conditions.Add("and a.supplier_name = @supplier_name");
                parameters.Add("supplier_name", searchParams["supplier_name"]);
            }

            if (!string.IsNullOrEmpty(searchParams["product_class"]))
            {
                conditions.Add("and a.product_class = @product_class");
                parameters.Add("product_class", searchParams["product_class"]);
            }

            if (!string.IsNullOrEmpty(searchParams["assess_date"]))
            {
                conditions.Add("and a.assess_date = @assess_date");
                parameters.Add("assess_date", searchParams["assess_date"]);
            }

            string whereClause = string.Join(" ", conditions);
            sql += $" {whereClause}";

            var conn = this.Database.GetDbConnection();
            SupplierReassessment items = conn.QueryFirstOrDefault<SupplierReassessment>(sql, parameters) ?? new SupplierReassessment();

            return items;
        }

        /*
        /// <summary>
        /// 供應商資料
        /// </summary>
        /// <returns></returns>
        public QualifiedSupplier_Data GetQualifiedSuppliersDatas(Dictionary<string, string?> searchParams)
        {
            string sql = @"select * from qualified_suppliers where 1=1";

            List<string> conditions = new();
            DynamicParameters parameters = new();

            if (!string.IsNullOrEmpty(searchParams["supplier_name"]))
            {
                conditions.Add("and supplier_name = @supplier_name");
                parameters.Add("supplier_name", searchParams["supplier_name"]);
            }

            if (!string.IsNullOrEmpty(searchParams["product_class"]))
            {
                conditions.Add("and product_class = @product_class");
                parameters.Add("product_class", searchParams["product_class"]);
            }

            if (!string.IsNullOrEmpty(searchParams["supplier_no"]))
            {
                conditions.Add("and supplier_no = @supplier_no");
                parameters.Add("supplier_no", searchParams["supplier_no"]);
            }

            string whereClause = string.Join(" ", conditions);
            sql += $" {whereClause}";

            var conn = this.Database.GetDbConnection();
            QualifiedSupplier_Data items = conn.QueryFirstOrDefault<QualifiedSupplier_Data>(sql, parameters) ?? new QualifiedSupplier_Data();

            return items;
        }
        /*
        /// <summary>
        /// 供應商清冊Export Excel
        /// </summary>
        /// <returns></returns>
        public async Task<MemoryStream> QualifiedSuppliersExportToExcelAsync(Dictionary<string, string?> searchParams, string orderByPart)
        {
            if (string.IsNullOrWhiteSpace(orderByPart))
            {
                orderByPart = "ORDER BY (SELECT NULL)"; // Ensures ROW_NUMBER() always has an ORDER BY
            }

            using var connection = this.Database.GetDbConnection();
            await connection.OpenAsync();

            #region sql
            string selectPart = @"select * from qualified_suppliers where 1=1";

            List<string> conditions = new();
            DynamicParameters parameters = new();


            if (!string.IsNullOrEmpty(searchParams["ddlQualifiedStatus"]))
            {
                conditions.Add("and reassess_result = @qualifiedStatus");
                parameters.Add("qualifiedStatus", searchParams["ddlQualifiedStatus"]);

            }

            if (!string.IsNullOrEmpty(searchParams["txtReassessDate"]))
            {
                conditions.Add("and reassess_date >= @ReassessDateStart");
                parameters.Add("reassessDateStart", searchParams["txtReassessDate"]);
            }

            if (!string.IsNullOrEmpty(searchParams["txtReassessDateEnd"]))
            {
                conditions.Add("and reassess_date <= @ReassessDateEnd");
                parameters.Add("ReassessDateEnd", searchParams["txtReassessDateEnd"]);
            }

            if (!string.IsNullOrEmpty(searchParams["txtSupplierNo"]))
            {
                conditions.Add("and supplier_no LIKE '%' + @supplierno + '%'");
                parameters.Add("supplierno", searchParams["txtSupplierNo"]);

            }

            if (!string.IsNullOrEmpty(searchParams["txtSupplierName"]))
            {
                conditions.Add("and supplier_name LIKE '%' + @suppliername + '%'");
                parameters.Add("suppliername", searchParams["txtSupplierName"]);

            }

            string whereClause = string.Join(" ", conditions);
            selectPart += $" {whereClause}";

            //string orderByPart = @" ORDER BY supplier_name";

            var sql = selectPart + orderByPart;

            #endregion


            Dictionary<string, string> TableHeaders = new()
        {
            { "product_class", "品項編號" },
            { "supplier_name", "供應商名稱" },
            { "supplier_class", "供應商分類" },
            { "supplier_1st_assess_date", "初評日期" },
            { "reassess_date", "再評日期" },
            { "product_class_title", "品項分類" },
            { "reassess_result", "合格狀態" }
        };

            var data = await connection.QueryAsync(sql, parameters);
            if (data.Any())
            {
                //有查詢結果才可以匯出
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Sheet1");

                if (data is not null)
                {
                    var dataTable = ToDataTable(data, TableHeaders);
                    worksheet.Cell(1, 1).InsertTable(dataTable);

                    // Auto-adjust column widths
                    worksheet.Columns().AdjustToContents();
                }


                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return stream;
            }

            throw new FileNotFoundException("No data to export");
        }


        public async Task<(List<T> Items, int TotalCount)> GetSupplierReassessmentsList<T>(
    Dictionary<string, string?> searchParams, string orderByPart, int pageNumber = 0, int pageSize = 0)
        {
            if (string.IsNullOrWhiteSpace(orderByPart))
            {
                orderByPart = "ORDER BY (SELECT NULL)";
            }

            var reassessConditions = new List<string>();
            var finalConditions = new List<string>();
            var purchaseAggConditions = new List<string>();
            var purchaseJoinConditions = new List<string>();
            var parameters = new DynamicParameters();

            // Utility for clean conditional check
            bool TryGetValid(IDictionary<string, string> dict, string key, out string value)
            {
                value = null;
                return dict.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val) && (value = val) != null;
            }

            // Filters
            if (TryGetValid(searchParams, "txtSupplierName", out var supplierName))
            {
                parameters.Add("suppliername", $"%{supplierName}%");
                reassessConditions.Add("sr.supplier_name LIKE @suppliername");
            }

            if (TryGetValid(searchParams, "txtProductClass", out var productClass))
            {
                parameters.Add("productClass", $"%{productClass}%");
                reassessConditions.Add("sr.product_class LIKE @productClass");
            }

            if (TryGetValid(searchParams, "txtAssessDateStar", out var startDate) && DateTime.TryParse(startDate, out _))
            {
                parameters.Add("ReassessDateStart", startDate);
                reassessConditions.Add("sr.assess_date >= @ReassessDateStart");
                purchaseAggConditions.Add("pr.assess_date >= @ReassessDateStart");
                purchaseJoinConditions.Add("pr.assess_date >= @ReassessDateStart");
            }

            if (TryGetValid(searchParams, "txtAssessDateEnd", out var endDate) && DateTime.TryParse(endDate, out _))
            {
                parameters.Add("ReassessDateEnd", endDate);
                reassessConditions.Add("sr.assess_date <= @ReassessDateEnd");
                purchaseAggConditions.Add("pr.assess_date <= @ReassessDateEnd");
                purchaseJoinConditions.Add("pr.assess_date <= @ReassessDateEnd");
            }

            if (TryGetValid(searchParams, "txtSupplierNo", out var supplierNo))
            {
                parameters.Add("supplierno", $"%{supplierNo}%");
                finalConditions.Add("qs.supplier_no LIKE @supplierno");
            }

            string sql = $@"
                        SELECT 
                            sr.product_class, 
                            sr.supplier_name, 
                            pr.supplier_class, 
                            qs.product_class_title,
                            sr.assess_date, 
                            agg.avg_grade AS grade, 
                            sr.assess_result, 
                            qs.supplier_no
                        FROM supplier_reassessment sr
                        LEFT JOIN qualified_suppliers qs 
                            ON sr.supplier_name = qs.supplier_name 
                            AND sr.product_class = qs.product_class
                        LEFT JOIN (
                            SELECT 
                                pr.supplier_name, 
                                pr.product_class, 
                                AVG(pr.grade) AS avg_grade
                            FROM purchase_records pr
                            WHERE pr.receipt_status IS NOT NULL 
                                  AND pr.grade IS NOT NULL
                                  {(purchaseAggConditions.Count > 0 ? "AND " + string.Join(" AND ", purchaseAggConditions) : "")}
                            GROUP BY pr.supplier_name, pr.product_class
                        ) agg 
                            ON sr.supplier_name = agg.supplier_name 
                            AND sr.product_class = agg.product_class
                        LEFT JOIN purchase_records pr 
                            ON pr.supplier_name = sr.supplier_name 
                            AND pr.product_class = sr.product_class 
                            AND pr.assess_date = sr.assess_date 
                            AND pr.receipt_status IS NOT NULL 
                            AND pr.grade IS NOT NULL
                            {(purchaseJoinConditions.Count > 0 ? "AND " + string.Join(" AND ", purchaseJoinConditions) : "")}
                        WHERE 1=1
                            {(reassessConditions.Count > 0 ? "AND " + string.Join(" AND ", reassessConditions) : "")}
                            {(finalConditions.Count > 0 ? "AND " + string.Join(" AND ", finalConditions) : "")}
                        ";

            // Feed to pager
            return await BySqlGetPagedWithCountAsync<T>(sql, orderByPart, pageNumber, pageSize, parameters);

        }

        public async Task<(List<T> Items, int TotalCount)> GetSupplierReassessmentsList_00<T>(
      Dictionary<string, string?> searchParams, string orderByPart, int pageNumber = 0, int pageSize = 0)
        {
            if (string.IsNullOrWhiteSpace(orderByPart))
            {
                orderByPart = "ORDER BY (SELECT NULL)";
            }

            var baseQuery = @"
SELECT a.product_class, 
       a.supplier_name, 
       d.supplier_class, 
       a.assess_date, 
       c.avg_grade AS grade, 
       a.assess_result, 
       b.supplier_no
FROM (
    SELECT supplier_name, product_class, assess_date, assess_result
    FROM supplier_reassessment
    WHERE 1 = 1
    {MainConditions}
) a
LEFT JOIN qualified_suppliers b 
    ON a.supplier_name = b.supplier_name AND a.product_class = b.product_class
LEFT JOIN (
    SELECT supplier_name, product_class, AVG(grade) AS avg_grade 
    FROM (
        SELECT supplier_name, product_class, grade
        FROM purchase_records
        WHERE receipt_status IS NOT NULL AND grade IS NOT NULL
        {SharedConditions}
    ) AS Src
    GROUP BY supplier_name, product_class
) c ON a.supplier_name = c.supplier_name AND a.product_class = c.product_class
LEFT JOIN (
    SELECT supplier_name, product_class, assess_date, supplier_class
    FROM purchase_records
    WHERE receipt_status IS NOT NULL AND grade IS NOT NULL
    {SharedConditions}
) d ON a.supplier_name = d.supplier_name 
     AND a.product_class = d.product_class 
     AND a.assess_date = d.assess_date
WHERE 1 = 1
{FinalConditions}
";

            // Condition builders
            List<string> mainConditions = new();
            List<string> sharedConditions = new();
            List<string> finalConditions = new();
            var parameters = new DynamicParameters();

            // Helper: single-target condition
            void AddCondition(List<string> targetList, string condition, string paramName, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    targetList.Add(condition);
                    parameters.Add(paramName, value);
                }
            }

            if (searchParams.TryGetValue("txtSupplierName", out var supplierName))
                AddCondition(mainConditions, "AND supplier_name LIKE '%' + @suppliername + '%'", "suppliername", supplierName);

            if (searchParams.TryGetValue("txtProductClass", out var productClass))
                AddCondition(mainConditions, "AND product_class LIKE '%' + @productClass + '%'", "productClass", productClass);

            if (searchParams.TryGetValue("txtAssessDateStar", out var startDate))
            {
                AddCondition(mainConditions, "AND assess_date >= @ReassessDateStart", "ReassessDateStart", startDate);
                AddCondition(sharedConditions, "AND assess_date >= @ReassessDateStart", "ReassessDateStart", startDate);
                AddCondition(finalConditions, "AND assess_date >= @ReassessDateStart", "ReassessDateStart", startDate);
            }

            if (searchParams.TryGetValue("txtAssessDateEnd", out var endDate))
            {
                AddCondition(mainConditions, "AND assess_date <= @ReassessDateEnd", "ReassessDateEnd", endDate);
                AddCondition(sharedConditions, "AND assess_date <= @ReassessDateEnd", "ReassessDateEnd", endDate);
                AddCondition(finalConditions, "AND assess_date <= @ReassessDateEnd", "ReassessDateEnd", endDate);
            }

            if (searchParams.TryGetValue("txtSupplierNo", out var supplierNo))
                AddCondition(finalConditions, "AND supplier_no LIKE '%' + @supplierno + '%'", "supplierno", supplierNo);

            // Plug in conditions
            string fullSql = baseQuery
                .Replace("{MainConditions}", string.Join(" ", mainConditions))
                .Replace("{SharedConditions}", string.Join(" ", sharedConditions))
                .Replace("{FinalConditions}", string.Join(" ", finalConditions));

            return await BySqlGetPagedWithCountAsync<T>(fullSql, orderByPart, pageNumber, pageSize, parameters);
        }

        /// <summary>
        /// 再評估清冊Export Excel
        /// </summary>
        /// <returns></returns>
        public async Task<MemoryStream> SupplierReassessmentsExportToExcelAsync(Dictionary<string, string?> searchParams, string orderByPart)
        {
            if (string.IsNullOrWhiteSpace(orderByPart))
            {
                orderByPart = "ORDER BY (SELECT NULL)";
            }

            using var connection = this.Database.GetDbConnection();
            await connection.OpenAsync();

            #region sql
            string selectPart = @"
select a.product_class,a.supplier_name,d.supplier_class,a.assess_date,c.avg_grade grade,a.assess_result,b.supplier_no 
from (
select * from supplier_reassessment where 1=1 
";

            List<string> conditions = new();
            List<string> conditions2 = new();
            List<string> conditions3 = new();
            DynamicParameters parameters = new();

            if (searchParams.TryGetValue("txtSupplierName", out var supplierName) && !string.IsNullOrEmpty(supplierName))
            {
                conditions.Add("and supplier_name LIKE '%' + @suppliername + '%'");
                parameters.Add("suppliername", supplierName);
            }

            if (searchParams.TryGetValue("txtProductClass", out var productClass) && !string.IsNullOrEmpty(productClass))
            {
                conditions.Add("and product_class LIKE '%' + @productClass + '%'");
                parameters.Add("productClass", productClass);
            }

            if (searchParams.TryGetValue("txtAssessDateStar", out var reassessDateStart) && !string.IsNullOrEmpty(reassessDateStart))
            {
                conditions.Add("and assess_date >= @ReassessDateStart");
                conditions2.Add("and assess_date >= @ReassessDateStart");
                conditions3.Add("and assess_date >= @ReassessDateStart");
                parameters.Add("ReassessDateStart", reassessDateStart);
            }

            if (searchParams.TryGetValue("txtAssessDateEnd", out var reassessDateEnd) && !string.IsNullOrEmpty(reassessDateEnd))
            {
                conditions.Add("and assess_date <= @ReassessDateEnd");
                conditions2.Add("and assess_date <= @ReassessDateEnd");
                conditions3.Add("and assess_date <= @ReassessDateEnd");
                parameters.Add("ReassessDateEnd", reassessDateEnd);
            }

            string whereClause = string.Join(" ", conditions);

            string sql2 = @" 
) a left join qualified_suppliers b 
on a.supplier_name=b.supplier_name and a.product_class=b.product_class 
left join
(
select supplier_name,product_class,avg(grade) as avg_grade from (select * from purchase_records 
where receipt_status is not null and grade is not null ";
            string whereClause2 = string.Join(" ", conditions2);
            sql2 += $" {whereClause2}";

            sql2 += @" 
) as Src group by supplier_name,product_class
) c
on a.supplier_name=c.supplier_name and a.product_class=c.product_class
left join
(
select * from purchase_records where receipt_status is not null and grade is not null ";
            string whereClause3 = string.Join(" ", conditions3);
            sql2 += $" {whereClause3}";

            sql2 += @" 
) d
on a.supplier_name=d.supplier_name and a.product_class=d.product_class and a.assess_date=d.assess_date 
where 1=1 
";
            selectPart += $" {whereClause}";
            selectPart = selectPart + sql2;


            List<string> conditions4 = new();
            if (searchParams.TryGetValue("txtSupplierNo", out var supplierNo) && !string.IsNullOrEmpty(supplierNo))
            {
                conditions4.Add("and supplier_no LIKE '%' + @supplierno + '%'");
                parameters.Add("supplierno", supplierNo);
            }
            string whereClause4 = string.Join(" ", conditions4);
            selectPart += $" {whereClause4}";

            //string orderByPart = @" ORDER BY supplier_name";

            var sql = selectPart + orderByPart;

            #endregion


            Dictionary<string, string> TableHeaders = new()
        {
            { "product_class", "品項編號" },
            { "supplier_name", "供應商名稱" },
            { "supplier_class", "供應商分類" },
            { "assess_date", "評估日期" },
            { "grade", "平均分數" },
            { "assess_result", "評核結果" }
        };

            var data = await connection.QueryAsync(sql, parameters);
            if (data.Any())
            {
                //有查詢結果才可以匯出
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Sheet1");

                if (data is not null)
                {
                    var dataTable = ToDataTable(data, TableHeaders);
                    worksheet.Cell(1, 1).InsertTable(dataTable);

                    // Auto-adjust column widths
                    worksheet.Columns().AdjustToContents();
                }


                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return stream;
            }

            throw new FileNotFoundException("No data to export");
        }

        
        /// <summary>
        /// 再評估需產生新的評核List
        /// </summary>    
        /// <returns></returns>
        public async Task<List<SupplierReassessment_QualifiedSupplier>> GetNewReassessmentsList(Dictionary<string, string?> searchParams, string orderByPart)
        {

            string selectPart = @"
select supplier_name,product_class,grade,reassess_result from (
select a.supplier_name,a.product_class,c.avg_grade grade,a.reassess_result
from (
select * from qualified_suppliers
) a
join
(
select supplier_name,product_class,avg(grade) as avg_grade from (select * from purchase_records 
where receipt_status is not null and grade is not null 
";
            List<string> conditions = new();
            DynamicParameters parameters = new();

            if (searchParams.TryGetValue("txtAssessDateStar", out var reassessDateStart) && !string.IsNullOrEmpty(reassessDateStart))
            {
                conditions.Add("and assess_date >= @ReassessDateStart");
                parameters.Add("ReassessDateStart", reassessDateStart);
            }

            if (searchParams.TryGetValue("txtAssessDateEnd", out var reassessDateEnd) && !string.IsNullOrEmpty(reassessDateEnd))
            {
                conditions.Add("and assess_date <= @ReassessDateEnd");
                parameters.Add("ReassessDateEnd", reassessDateEnd);
            }

            string whereClause = string.Join(" ", conditions);
            selectPart += $" {whereClause}";

            selectPart += @"
) as Src group by supplier_name,product_class
) c
on a.supplier_name=c.supplier_name and a.product_class=c.product_class
join
(
select * from purchase_records where receipt_status is not null and grade is not null 
";

            selectPart += $" {whereClause}";

            selectPart += @"
) d
on a.supplier_name=d.supplier_name and a.product_class=d.product_class 
) aa group by supplier_name,product_class,grade,reassess_result
";

            selectPart += orderByPart;

            using var connection = this.Database.GetDbConnection();
            await connection.OpenAsync();

            var items = await connection.QueryAsync<SupplierReassessment_QualifiedSupplier>(selectPart, parameters);

            return items.ToList();
        }

        public async Task<int> CreateReassessments(List<SupplierReassessment_QualifiedSupplier>? listModel, string NewassessDateStar)
        {
            try
            {
                if (listModel == null || listModel.Count == 0)
                {
                    return 0;
                }
                var sql = @"
            INSERT INTO supplier_reassessment(supplier_name, product_class, assess_date, assess_result) VALUES (@supplier_name, @product_class, @assess_date, @assess_result)
            UPDATE qualified_suppliers set reassess_result='合格',reassess_date=@assess_date where supplier_name=@supplier_name and product_class=@product_class
            ";
                using var connection = this.Database.GetDbConnection();
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();
                foreach (var item in listModel)
                {
                    await connection.ExecuteAsync(sql, new
                    {
                        supplier_name = item.supplier_name,
                        product_class = item.product_class,
                        assess_date = NewassessDateStar,
                        assess_result = item.reassess_result
                    }, transaction);
                }
                transaction.Commit();
                return 1;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }*/
    }
}
