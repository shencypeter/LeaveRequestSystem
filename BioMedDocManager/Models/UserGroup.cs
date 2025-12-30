using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 使用者群組
/// </summary>
public class UserGroup : AuditableEntity
{
    /// <summary>
    /// 群組編號
    /// </summary>
    [Key]
    [Display(Name = "UserGroup.UserGroupId")]
    public int UserGroupId { get; set; }

    /// <summary>
    /// 群組名稱
    /// </summary>
    [Display(Name = "UserGroup.UserGroupCode")]
    [StringLength(
        100,
        ErrorMessage = "Validation.StringLength"
    )]
    public string UserGroupCode { get; set; } = null!;

    /// <summary>
    /// 群組說明
    /// </summary>
    [Display(Name = "UserGroup.UserGroupDescription")]
    [StringLength(
        255,
        ErrorMessage = "Validation.StringLength"
    )]
    public string? UserGroupDescription { get; set; }

    /* ========= 導覽屬性 ========= */

    /// <summary>
    /// 群組成員關聯
    /// </summary>
    public ICollection<UserGroupMember> UserGroupMembers { get; set; } = new List<UserGroupMember>();

    /// <summary>
    /// 群組角色關聯
    /// </summary>
    public ICollection<UserGroupRole> UserGroupRoles { get; set; } = new List<UserGroupRole>();
}
