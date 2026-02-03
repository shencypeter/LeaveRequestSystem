using System.ComponentModel.DataAnnotations;

namespace BioMedDocManager.Models;

/// <summary>
/// 角色 × 資源 × 動作 權限關聯
/// </summary>
public class RolePermission
{
    /// <summary>
    /// 角色編號
    /// </summary>
    [Display(Name = "RolePermission.RoleId")]
    public long RoleId { get; set; }

    /// <summary>
    /// 資源編號
    /// </summary>
    [Display(Name = "RolePermission.ResourceId")]
    public long ResourceId { get; set; }

    /// <summary>
    /// 動作編號
    /// </summary>
    [Display(Name = "RolePermission.AppActionId")]
    public long AppActionId { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    public Role? Role { get; set; }

    /// <summary>
    /// 資源
    /// </summary>
    public Resource? Resource { get; set; }

    /// <summary>
    /// 動作
    /// </summary>
    public AppAction? AppAction { get; set; }
}
