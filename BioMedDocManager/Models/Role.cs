using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;

namespace BioMedDocManager.Models;

/// <summary>
/// 角色
/// </summary>
public class Role : AuditableEntity
{
    /// <summary>
    /// 角色編號
    /// </summary>
    [Key]
    [Display(Name = "Role.RoleId")]
    public int RoleId { get; set; }

    /// <summary>
    /// 角色名稱
    /// </summary>
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(100, ErrorMessage = "Validation.StringLength")]
    [Display(Name = "Role.RoleCode")]
    public string RoleCode { get; set; } = null!;

    /// <summary>
    /// 角色群組
    /// </summary>
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(100, ErrorMessage = "Validation.StringLength")]
    [Display(Name = "Role.RoleGroup")]
    public string RoleGroup { get; set; } = null!;

    /// <summary>
    /// 使用者角色-關聯
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// 使用者群組角色-關聯
    /// </summary>
    public ICollection<UserGroupRole> UserGroupRoles { get; set; } = new List<UserGroupRole>();

    /// <summary>
    /// 使用者權限-關聯
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
