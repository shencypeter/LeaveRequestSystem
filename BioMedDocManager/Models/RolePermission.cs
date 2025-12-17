using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 角色資源動作權限
/// </summary>
public class RolePermission
{
    /// <summary>
    /// 角色編號
    /// </summary>
    [Display(Name = "角色編號")]
    public int RoleId { get; set; }

    /// <summary>
    /// 資源編號
    /// </summary>
    [Display(Name = "資源編號")]
    public int ResourceId { get; set; }

    /// <summary>
    /// 動作編號
    /// </summary>
    [Display(Name = "動作編號")]
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
