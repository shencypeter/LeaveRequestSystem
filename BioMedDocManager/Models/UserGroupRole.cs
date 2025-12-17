using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 使用者群組角色
/// </summary>
public class UserGroupRole
{
    /// <summary>
    /// 群組編號
    /// </summary>
    [Display(Name = "群組編號")]
    public int UserGroupId { get; set; }

    /// <summary>
    /// 角色編號
    /// </summary>
    [Display(Name = "角色編號")]
    public int RoleId { get; set; }

    /// <summary>
    /// 使用者群組-關聯
    /// </summary>
    public UserGroup? UserGroup { get; set; }

    /// <summary>
    /// 角色-關聯
    /// </summary>
    public Role? Role { get; set; }
}
