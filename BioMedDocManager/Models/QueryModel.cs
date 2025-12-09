using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models
{
    public class QueryModel
    {
    }

    /// <summary>
    /// 使用者群組查詢條件 / 分頁 Model
    /// </summary>
    public class UserGroupQueryViewModel : Pagination
    {
        /// <summary>
        /// 群組名稱（模糊查詢）
        /// </summary>
        public string? UserGroupName { get; set; }

        /// <summary>
        /// 群組說明（模糊查詢）
        /// </summary>
        public string? UserGroupDescription { get; set; }
    }

    /// <summary>
    /// 資源查詢條件
    /// </summary>
    public class ResourceQueryViewModel : Pagination
    {
        /// <summary>
        /// 資源類型（PAGE / API / ...）
        /// </summary>
        [Display(Name = "資源類型")]
        public string? ResourceType { get; set; }

        /// <summary>
        /// 資源代號
        /// </summary>
        [Display(Name = "資源代號")]
        public string? ResourceKey { get; set; }

        /// <summary>
        /// 顯示名稱
        /// </summary>
        [Display(Name = "顯示名稱")]
        public string? ResourceDisplayName { get; set; }

        /// <summary>
        /// 是否啟用
        /// null = 全部, true = 啟用, false = 停用
        /// </summary>
        [Display(Name = "是否啟用")]
        public bool? ResourceIsActive { get; set; }

    }

    /// <summary>
    /// 角色查詢條件
    /// </summary>
    public class RoleQueryViewModel : Pagination
    {
        /// <summary>
        /// 角色名稱
        /// </summary>
        [Display(Name = "角色名稱")]
        public string? RoleName { get; set; }

        /// <summary>
        /// 角色群組
        /// </summary>
        [Display(Name = "角色群組")]
        public string? RoleGroup { get; set; }

    }

    /// <summary>
    /// AppAction 清單查詢條件
    /// </summary>
    public class AppActionQueryViewModel : Pagination
    {
        /// <summary>
        /// 動作名稱（模糊查詢）
        /// </summary>
        [Display(Name = "動作名稱")]
        public string? AppActionName { get; set; }

        /// <summary>
        /// 顯示名稱（模糊查詢）
        /// </summary>
        [Display(Name = "顯示名稱")]
        public string? AppActionDisplayName { get; set; }

    }

    /// <summary>
    /// 選單項目查詢條件
    /// </summary>
    public class MenuItemQueryViewModel : Pagination
    {
        /// <summary>
        /// 選單標題（模糊查詢）
        /// </summary>
        [Display(Name = "選單標題")]
        public string? MenuItemTitle { get; set; }

        /// <summary>
        /// 上層選單編號（精確查詢）
        /// </summary>
        [Display(Name = "上層選單編號")]
        public int? MenuItemParentId { get; set; }

        /// <summary>
        /// Resource.ResourceKey 關鍵字查詢
        /// </summary>
        public string? ResourceKey { get; set; }

        /// <summary>
        /// 資源編號（精確查詢）
        /// </summary>
        [Display(Name = "資源編號")]
        public int? ResourceId { get; set; }

        /// <summary>
        /// 是否啟用
        /// null = 全部；true = 啟用；false = 停用
        /// </summary>
        [Display(Name = "是否啟用")]
        public bool? MenuItemIsActive { get; set; }

    }
































    // ===== 以下是範例QueryModel =====

    /// <summary>
    /// 供應商清冊 model
    /// </summary>
    public class QualifiedSupplierQueryModel : Pagination
    {
        /// <summary>合格狀態 (例如: 合格/不合格)</summary>
        public string? QualifiedStatus { get; set; }

        /// <summary>再評開始日期</summary>
        public DateTime? ReassessDateStart { get; set; }

        /// <summary>再評結束日期</summary>
        public DateTime? ReassessDateEnd { get; set; }

        /// <summary>供應商統編</summary>
        public string? SupplierNo { get; set; }

        /// <summary>供應商名稱</summary>
        public string? SupplierName { get; set; }

    }

    public class FormQueryModel : Pagination
    {

        public string? DocNoA { get; set; }
        public string? DocNoB { get; set; }


        /// <summary>
        /// 文件類別 (B: 內部, E: 外部, C: 客戶)
        /// </summary>
        public string? DocType { get; set; }

        /// <summary>
        /// 入庫 起
        /// </summary>
        public DateTime? InTime1 { get; set; }

        /// <summary>
        /// 入庫 迄
        /// </summary>
        public DateTime? InTime2 { get; set; }

        /// <summary>
        /// 領用人
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// 表單編號 (BMP... 空白表單編號)
        /// </summary>
        public string? FormNo { get; set; }

        /// <summary>
        /// B2025.... (領用之日期流水號)
        /// </summary>
        public string? DocNo { get; set; }

        /// <summary>
        /// 入庫 "或" 註銷
        /// </summary>
        public string? FiledOrRevoked { get; set; }

        /// <summary>
        /// 未入庫 "且" 未註銷
        /// </summary>
        public string? UnFiledAndNotRevoked { get; set; }


        public DateTime? IssueDate { get; set; }

        public string? DocName { get; set; }

        public string? DocVer { get; set; }

        public string? Remark { get; set; }

        public string? ProjectName { get; set; }

        public string? OriginalDocNo { get; set; }

        public string? Purpose { get; set; }

        public bool? IsConfidential { get; set; }

        public bool? IsSensitive { get; set; }



    }


    public class OldDocControl2020_QueryModel : Pagination
    {
        public DateTime? IssueDate { get; set; }
        public string? DocNo { get; set; }


        public string? DocVer { get; set; }

        public string? DocName { get; set; }
        public string? Remark { get; set; }

        public string? ProjectName { get; set; }

    }

    public class SupplierAssessQueryModel : Pagination
    {

        public string? ProductClass { get; set; }
        public string? ProductClassTitle { get; set; }

        public string? SupplierName { get; set; }
        public string? ItemNo { get; set; }
        public string? ItemCategory { get; set; }
        public DateTime? AssessDateStart { get; set; }
        public DateTime? AssessDateEnd { get; set; }
        public DateTime? NewAssessDate { get; set; }

        public string SupplierClass { get; set; } = string.Empty;

        /// <summary>
        /// 初評狀態
        /// </summary>
        public string HasReviewDate { get; set; } = string.Empty;

        public string RiskLevel { get; set; } = string.Empty;

        public string? SupplierNo { get; set; }

    }

    public class AssessmentsQueryModel : PurchaseRecordsQueryModel
    {
        /// <summary>
        /// 評核結果
        /// </summary>
        public string AssessResult { get; set; }

    }

    public class PurchaseRecordsQueryModel : PurchaseQueryModel
    {

        public string? SupplierName { get; set; }

        //採購金額, 分數, 都採區間查詢 (input type number)

        public int? PurchaseMin { get; set; }

        public int? PurchaseMax { get; set; }

        public int? GradeMin { get; set; }
        public int? GradeMax { get; set; }

    }


    /// <summary>
    /// 查詢的 model
    /// </summary>
    public class PurchaseQueryModel : Pagination
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? ProductClass { get; set; }
        public string? Purchaser { get; set; }

        public string? Requester { get; set; }
        public string? SupplierName { get; set; }
        public string? RequestNo { get; set; }
        public string? ProductId { get; set; }
        public string? SupplierNo { get; set; }
        public string? ProductName { get; set; }
        public string? ReceiveNumber { get; set; }
        public string? ReceivePerson { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime? DeliveryDateStart { get; set; }
        public DateTime? DeliveryDateEnd { get; set; }
        public string? VerifyPerson { get; set; }
        public DateTime? VerifyDate { get; set; }
        public DateTime? VerifyDateStart { get; set; }
        public DateTime? VerifyDateEnd { get; set; }




    }

    public class ProductClassQueryModel : Pagination
    {
        /// <summary>
        /// 供應商分類
        /// </summary>
        public string? SupplierClass { get; set; }

        /// <summary>
        /// 品項編號
        /// </summary>
        public string? ProductClass { get; set; }

        /// <summary>
        /// 顯示名稱
        /// </summary>
        public string? ProductClassTitle { get; set; }

        /// <summary>
        /// 是否停用(虛擬欄位)
        /// </summary>
        [NotMapped]
        public virtual string? IsEnabled { get; set; }


    }

}
