using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 參數
/// </summary>
public class Parameter : AuditableEntity
{

    [Key]
    public int ParameterId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "參數代碼")]
    public string ParameterCode { get; set; } = default!;

    [Required]
    [StringLength(200)]
    [Display(Name = "參數名稱")]
    public string ParameterName { get; set; } = default!;

    [Display(Name = "參數值")]
    public string? ParameterValue { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "參數格式")]
    public string ParameterFormat { get; set; } = default!;

    [Display(Name = "是否啟用")]
    public bool ParameterIsActive { get; set; } = true;

    /// <summary>
    /// 是否啟用 文字
    /// </summary>
    [NotMapped]
    [Display(Name = "是否啟用")]
    public string ParameterIsActiveText => ParameterIsActive ? "啟用" : "停用";

}
