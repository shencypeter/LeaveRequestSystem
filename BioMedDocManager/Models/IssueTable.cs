using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BioMedDocManager.Models;

/// <summary>
/// 已發行文件
/// </summary>
public partial class IssueTable
{
    /// <summary>
    /// 序號
    /// </summary>
    [NotMapped]
    public int RowNum { get; set; } = 0;

    /// <summary>
    /// 紀錄名稱
    /// </summary>    
    [Column("name")]
    [Display(Name = "紀錄名稱")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(4000, ErrorMessage = "{0}最多{1}字元")]
    public string? Name { get; set; }

    /// <summary>
    /// 發行日期
    /// </summary>    
    [Column("issue_datetime")]
    [Display(Name = "發行日期")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "無")]
    public DateTime? IssueDatetime { get; set; }

    /// <summary>
    /// BMP表單編號
    /// </summary>    
    [Column("original_doc_no")]
    [Display(Name = "表單編號")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string OriginalDocNo { get; set; } = null!;

    /// <summary>
    /// 表單版次
    /// </summary>
    [Column("doc_ver")]
    [Display(Name = "表單版次")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(10, ErrorMessage = "{0}最多{1}字元")]
    public string DocVer { get; set; } = null!;

    /// <summary>
    /// 檔案類型：docx、xlsx等
    /// </summary>    
    [Column("file_extension")]
    [Display(Name = "檔案類型")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(10, ErrorMessage = "{0}最多{1}字元")]
    public string? FileExtension { get; set; }

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
    /// 表單下載用檔名(實體檔名)，例如：B202407002_XX報告(v2.0).docx
    /// </summary>
    [NotMapped]
    public virtual string RealFileName
    {
        get
        {
            if (
                string.IsNullOrWhiteSpace(OriginalDocNo) ||
                string.IsNullOrWhiteSpace(DocVer) ||
                string.IsNullOrWhiteSpace(FileExtension))
            {
                return "Invalid filename";
            }

            return $"{OriginalDocNo}(V{DocVer}).{FileExtension}";
        }
    }

}
