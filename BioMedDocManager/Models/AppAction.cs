using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 動作
/// </summary>
public class AppAction : AuditableEntity
{
    /// <summary>
    /// 動作編號
    /// </summary>
    [Key]
    [Display(Name = "動作編號")]
    public int AppActionId { get; set; }

    /// <summary>
    /// 動作名稱
    /// </summary>
    [Display(Name = "動作名稱")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string AppActionName { get; set; } = null!;  // index/create/edit/delete/...

    /// <summary>
    /// 顯示名稱
    /// </summary>
    [Display(Name = "顯示名稱")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string AppActionDisplayName { get; set; } = null!;

    /// <summary>
    /// 顯示順序
    /// </summary>
    [Display(Name = "顯示順序")]
    public int AppActionOrder { get; set; }

    /// <summary>
    /// 使用者權限-關聯
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();


}
