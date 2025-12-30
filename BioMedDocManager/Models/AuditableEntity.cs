using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BioMedDocManager.Models;

/// <summary>
/// 稽核標記基底類別
/// </summary>
public abstract class AuditableEntity : ISoftDelete, IHasDbLocalizer
{
    /// <summary>
    /// DB Localizer（供 NotMapped 計算屬性使用）
    /// Controller/Service 在取資料後可指派：entity.Loc = _loc;
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public IDbLocalizer? Loc { get; set; }


    [Display(Name = "Common.CreatedAt")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
    public DateTime? CreatedAt { get; set; }

    [Display(Name = "Common.CreatedBy")]
    public int? CreatedBy { get; set; }

    [Display(Name = "Common.UpdatedAt")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
    public DateTime? UpdatedAt { get; set; }

    [Display(Name = "Common.UpdatedBy")]
    public int? UpdatedBy { get; set; }

    [Display(Name = "Common.DeletedAt")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
    public DateTime? DeletedAt { get; set; }

    [Display(Name = "Common.DeletedBy")]
    public int? DeletedBy { get; set; }
}
