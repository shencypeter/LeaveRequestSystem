using System;
using System.ComponentModel.DataAnnotations;

namespace BioMedDocManager.Models;

/// <summary>
/// 多語系文字
/// </summary>
public class LocalizationString : AuditableEntity
{
    [Key]
    [Display(Name = "流水號")]
    public long Id { get; set; }

    /// <summary>
    /// Key（例如：Parameter.SEC_PASSWORD_MIN_LENGTH.Label）
    /// </summary>
    [Required]
    [StringLength(200)]
    [Display(Name = "字串鍵值")]
    public string Key { get; set; } = default!;

    /// <summary>
    /// Culture（例如：zh-TW / en-US）
    /// </summary>
    [Required]
    [StringLength(20)]
    [Display(Name = "語系")]
    public string Culture { get; set; } = default!;

    /// <summary>
    /// 顯示文字
    /// </summary>
    [Required]
    [Display(Name = "顯示文字")]
    public string Value { get; set; } = default!;

    /// <summary>
    /// 分類（例如：Security / Common / Menu）
    /// </summary>
    [StringLength(100)]
    [Display(Name = "分類")]
    public string? Category { get; set; }

    /// <summary>
    /// 備註/說明（給管理者看的）
    /// </summary>
    [StringLength(500)]
    [Display(Name = "說明")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "是否啟用")]
    public bool IsActive { get; set; } = true;

}
