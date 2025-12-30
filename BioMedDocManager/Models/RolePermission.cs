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
    public int RoleId { get; set; }

    /// <summary>
    /// 資源編號
    /// </summary>
    [Display(Name = "RolePermission.ResourceId")]
    public int ResourceId { get; set; }

    /// <summary>
    /// 動作編號
    /// </summary>
    [Display(Name = "RolePermission.AppActionId")]
    public int AppActionId { get; set; }

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
