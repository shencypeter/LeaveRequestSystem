using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BioMedDocManager.Models;

/// <summary>
/// 系統參數
/// </summary>
public class Parameter : AuditableEntity
{
    /// <summary>
    /// 參數編號
    /// </summary>
    [Key]
    [Display(Name = "Parameter.ParameterId")]
    public long ParameterId { get; set; }

    /// <summary>
    /// 參數代碼（程式使用）
    /// </summary>
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(100, ErrorMessage = "Validation.StringLength")]
    [Display(Name = "Parameter.ParameterCode")]
    public string ParameterCode { get; set; } = default!;

    /// <summary>
    /// 參數名稱（顯示用）
    /// </summary>
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(200, ErrorMessage = "Validation.StringLength")]
    [Display(Name = "Parameter.ParameterName")]
    public string ParameterName { get; set; } = default!;

    /// <summary>
    /// 參數值
    /// </summary>
    [Display(Name = "Parameter.ParameterValue")]
    public string? ParameterValue { get; set; }

    /// <summary>
    /// 參數格式（string / int / bool / json…）
    /// </summary>
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(20, ErrorMessage = "Validation.StringLength")]
    [Display(Name = "Parameter.ParameterFormat")]
    public string ParameterFormat { get; set; } = default!;

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "Parameter.ParameterIsActive")]
    public bool ParameterIsActive { get; set; } = true;

    /// <summary>
    /// 是否啟用（顯示文字，多語系）
    /// </summary>
    [NotMapped]
    [Display(Name = "Parameter.ParameterIsActive")]
    public string ParameterIsActiveText =>
        ParameterIsActive
            ? (Loc?.T("Common.Enabled") ?? "Enabled")
            : (Loc?.T("Common.Disabled") ?? "Disabled");
}
