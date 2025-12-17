using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    [Display(Name = "資源編號")]
    public int ResourceId { get; set; }

    /// <summary>
    /// 資源類型
    /// </summary>
    [Display(Name = "資源類型")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string ResourceType { get; set; } = null!; // PAGE / API / ...

    /// <summary>
    /// 資源代號
    /// </summary>
    [Display(Name = "資源代號")]
    [StringLength(200, ErrorMessage = "{0}最多{1}字元")]
    public string ResourceKey { get; set; } = null!;

    /// <summary>
    /// 顯示名稱
    /// </summary>
    [Display(Name = "顯示名稱")]
    [StringLength(200, ErrorMessage = "{0}最多{1}字元")]
    public string ResourceDisplayName { get; set; } = null!;

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "是否啟用")]
    public bool ResourceIsActive { get; set; } = true;

    /// <summary>
    /// 是否啟用 文字
    /// </summary>
    [NotMapped]
    [Display(Name = "是否啟用")]
    public string ResourceIsActiveText => ResourceIsActive ? "啟用" : "停用";

    /// <summary>
    /// 選單清單
    /// </summary>
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

    /// <summary>
    /// 角色權限清單
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
