using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BioMedDocManager.Models;

/// <summary>
/// 文件領用紀錄表
/// </summary>
public partial class DocControlMaintable
{
    /// <summary>
    /// 文件類別：廠內文件(B)、外來文件(E)
    /// </summary>    
    [Column("type")]
    [Display(Name = "文件類別")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? Type { get; set; }

    /// <summary>
    /// 領用日期
    /// </summary>    
    [Column("date_time")]
    [Display(Name = "領用日期")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "無")]
    public DateTime? DateTime { get; set; }

    /// <summary>
    /// 工號
    /// </summary>    
    [Column("id")]
    [Display(Name = "工號")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? Id { get; set; }

    /// <summary>
    /// 領用人(理論上用不到，要用關聯user方式找出姓名)
    /// </summary>
    [Column("person_name")]
    [Display(Name = "領用人")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? PersonName { get; set; }

    /// <summary>
    /// 領用人-關聯User
    /// </summary>
    [Display(Name = "領用人")]
    public virtual User? Person { get; set; }

    /// <summary>
    /// 文件編號(年月流水號3碼，BYYYYMM???，例如:B202504001)
    /// </summary>    
    [Column("id_no")]
    [Display(Name = "文件編號")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string IdNo { get; set; } = null!;

    /// <summary>
    /// 表單名稱
    /// </summary>    
    [Column("name")]
    [Display(Name = "表單名稱")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(4000, ErrorMessage = "{0}最多{1}字元")]
    public string? Name { get; set; }

    /// <summary>
    /// 領用目的
    /// </summary>    
    [Column("purpose")]
    [Display(Name = "領用目的")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(4000, ErrorMessage = "{0}最多{1}字元")]
    [DataType(DataType.MultilineText)]
    public string? Purpose { get; set; }

    /// <summary>
    /// BMP表單編號
    /// </summary>
    [Column("original_doc_no")]
    [Display(Name = "表單編號")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(4000, ErrorMessage = "{0}最多{1}字元")]
    public string? OriginalDocNo { get; set; }

    /// <summary>
    /// 表單版次
    /// </summary>
    [Column("doc_ver")]
    [Display(Name = "表單版次")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(10, ErrorMessage = "{0}最多{1}字元")]
    public string? DocVer { get; set; }

    /// <summary>
    /// 入庫日期
    /// </summary>
    [Column("in_time")]
    [Display(Name = "入庫日期")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "無")]
    public DateTime? InTime { get; set; }

    /// <summary>
    /// 文件領用後註銷之註銷日期
    /// </summary>
    [Column("unuse_time")]
    [Display(Name = "註銷日期")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "無")]
    public DateTime? UnuseTime { get; set; }

    /// <summary>
    /// 文件領用後註銷之註銷原因(若管理者協助註銷，則需額外註記管理者資訊)
    /// </summary>
    [Column("reject_reason")]
    [Display(Name = "註銷原因")]
    [DataType(DataType.MultilineText)]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(4000, ErrorMessage = "{0}最多{1}字元")]
    public string? RejectReason { get; set; }

    /// <summary>
    /// 文件所屬之專案代碼
    /// </summary>
    [Column("project_name")]
    [Display(Name = "專案代碼")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// 檔案類型：docx、xlsx等
    /// </summary>
    [Column("file_extension")]
    [Display(Name = "檔案類型")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(10, ErrorMessage = "{0}最多{1}字元")]
    public string? FileExtension { get; set; }

    /// <summary>
    /// 是否機密
    /// </summary>
    [Column("is_confidential")]
    [Display(Name = "是否機密")]
    [DisplayFormat(NullDisplayText = "無")]
    public bool? IsConfidential { get; set; }

    /// <summary>
    /// 是否機敏
    /// </summary>
    [Column("is_sensitive")]
    [Display(Name = "是否機敏")]
    [DisplayFormat(NullDisplayText = "無")]
    public bool? IsSensitive { get; set; }

    /// <summary>
    /// 檔案的 MIME 類型（Content-Type）
    /// </summary>
    [NotMapped]
    public virtual string ContentType
    {
        get
        {
            var extension = FileExtension?.ToLowerInvariant();
            return extension switch
            {
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xls" => "application/vnd.ms-excel",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _ => "application/octet-stream"
            };
        }
    }

    /// <summary>
    /// 文件用檔名，例如：B202407002_XX報告(v2.0).docx
    /// </summary>
    [NotMapped]
    public virtual string RealFileName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(IdNo) ||
            string.IsNullOrWhiteSpace(Name) ||
            string.IsNullOrWhiteSpace(FileExtension))
            {
                return "Invalid filename";
            }

            // Type 為 B 時，DocVer 為必要欄位
            if (Type == "B" && string.IsNullOrWhiteSpace(DocVer))
            {
                return "Invalid filename";
            }

            // 確認版本
            var versionPart = Type == "B" ? $"(V{DocVer})" : string.Empty;

            // 組檔名
            return $"{IdNo}_{Name}{versionPart}.{FileExtension}";

        }
    }

    /// <summary>
    /// 合成用表單檔名，例如：XX報告(v2.0).docx
    /// </summary>
    [NotMapped]
    public virtual string RealFormFileName
    {
        get
        {
            if (
            string.IsNullOrWhiteSpace(OriginalDocNo) ||
            string.IsNullOrWhiteSpace(FileExtension))
            {
                return "Invalid filename";
            }

            // Type 為 B 時，DocVer 為必要欄位
            if (Type == "B" && string.IsNullOrWhiteSpace(DocVer))
            {
                return "Invalid filename";
            }

            // 確認版本
            var versionPart = Type == "B" ? $"(V{DocVer})" : string.Empty;

            // 組檔名
            return $"{OriginalDocNo}{versionPart}.{FileExtension}";

        }
    }

    /// <summary>
    /// 入庫處理人員(外鍵)
    /// </summary>
    [Column("in_time_modify_by")]
    [Display(Name = "入庫處理人員")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? InTimeModifyBy { get; set; }

    /// <summary>
    /// 入庫處理人員 - 關聯 User
    /// </summary>
    [ForeignKey(nameof(InTimeModifyBy))]
    [Display(Name = "入庫處理人員")]
    [DisplayFormat(NullDisplayText = "無")]
    public virtual User? InTimeModifyUser { get; set; }

    /// <summary>
    /// 入庫處理時間
    /// </summary>
    [Column("in_time_modify_at")]
    [Display(Name = "入庫處理時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true, NullDisplayText = "無")]
    public DateTime? InTimeModifyAt { get; set; }

    /// <summary>
    /// 註銷處理人員
    /// </summary>
    [Column("unuse_time_modify_by")]
    [Display(Name = "註銷處理人員")]    
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? UnuseTimeModifyBy { get; set; }

    /// <summary>
    /// 註銷處理人員 - 關聯 User
    /// </summary>
    [ForeignKey(nameof(UnuseTimeModifyBy))]
    [Display(Name = "註銷處理人員")]
    [DisplayFormat(NullDisplayText = "無")]
    public virtual User? UnuseTimeModifyUser { get; set; }

    /// <summary>
    /// 註銷處理時間
    /// </summary>
    [Column("unuse_time_modify_at")]
    [Display(Name = "註銷處理時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true, NullDisplayText = "無")]
    public DateTime? UnuseTimeModifyAt { get; set; }



}
