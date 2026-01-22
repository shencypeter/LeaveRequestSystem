using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 多語系文字
/// </summary>
public class LocalizationString : AuditableEntity
{
    [Key]
    [Display(Name = "LocalizationString.LocalizationStringId")]
    public long LocalizationStringId { get; set; }

    /// <summary>
    /// Key（例如：Parameter.SEC_PASSWORD_MIN_LENGTH.Label）
    /// </summary>
    [Required]
    [StringLength(200)]
    [Display(Name = "LocalizationString.LocalizationStringKey")]
    public string LocalizationStringKey { get; set; } = default!;

    /// <summary>
    /// Culture（例如：zh-TW / en-US）
    /// </summary>
    [Required]
    [StringLength(20)]
    [Display(Name = "LocalizationString.LocalizationStringCulture")]
    public string LocalizationStringCulture { get; set; } = default!;

    /// <summary>
    /// 顯示文字
    /// </summary>
    [Required]
    [Display(Name = "LocalizationString.LocalizationStringValue")]
    public string LocalizationStringValue { get; set; } = default!;

    /// <summary>
    /// 分類（例如：Security / Common / Menu）
    /// </summary>
    [StringLength(100)]
    [Display(Name = "LocalizationString.LocalizationStringCategory")]
    public string? LocalizationStringCategory { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "LocalizationString.LocalizationStringIsActive")]
    public bool LocalizationStringIsActive { get; set; } = true;

    /// <summary>
    /// 是否啟用（文字）
    /// </summary>
    [NotMapped]
    [Display(Name = "LocalizationString.LocalizationStringIsActive")]
    public string LocalizationStringIsActiveText =>
        LocalizationStringIsActive
            ? (Loc?.T("Common.Enabled") ?? "Enabled")
            : (Loc?.T("Common.Disabled") ?? "Disabled");
}
