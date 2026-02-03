using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BioMedDocManager.Models;

/// <summary>
/// 資源
/// </summary>
public class Resource : AuditableEntity
{
    /// <summary>
    /// 資源編號
    /// </summary>
    [Key]
    [Display(Name = "Resource.ResourceId")]
    public long ResourceId { get; set; }

    /// <summary>
    /// 資源類型
    /// </summary>
    [Display(Name = "Resource.ResourceType")]
    [StringLength(
        50,
        ErrorMessage = "Validation.StringLength"
    )]
    public string ResourceType { get; set; } = null!; // PAGE / API / ...

    /// <summary>
    /// 資源代碼
    /// </summary>
    [Display(Name = "Resource.ResourceKey")]
    [StringLength(
        200,
        ErrorMessage = "Validation.StringLength"
    )]
    public string ResourceKey { get; set; } = null!;

    /// <summary>
    /// 資源代碼
    /// </summary>
    [NotMapped]
    [Display(Name = "Resource.ResourceDisplayName")]
    public string ResourceDisplayName => Loc?.T($"{ResourceKey}.Index.Title") ?? ResourceKey;

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "Resource.ResourceIsActive")]
    public bool ResourceIsActive { get; set; } = true;

    /// <summary>
    /// 是否啟用 文字
    /// </summary>
    [NotMapped]
    [Display(Name = "Resource.ResourceIsActive")]
    public string ResourceIsActiveText =>
        ResourceIsActive
            ? (Loc?.T("Common.Enabled") ?? "Enabled")
            : (Loc?.T("Common.Disabled") ?? "Disabled");

    /// <summary>
    /// 選單清單
    /// </summary>
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

    /// <summary>
    /// 角色權限清單
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
