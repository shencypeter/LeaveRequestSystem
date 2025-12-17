using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 稽核標記基底類別
/// </summary>
public abstract class AuditableEntity: ISoftDelete
{
    [Display(Name = "建立日期")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? CreatedAt { get; set; }

    [Display(Name = "建立人")]
    public int? CreatedBy { get; set; }

    [Display(Name = "更新日期")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? UpdatedAt { get; set; }

    [Display(Name = "更新人")]
    public int? UpdatedBy { get; set; }

    [Display(Name = "刪除日期")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? DeletedAt { get; set; }

    [Display(Name = "刪除人")]
    public int? DeletedBy { get; set; }
}

