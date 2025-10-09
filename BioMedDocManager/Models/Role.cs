using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BioMedDocManager.Models;

/// <summary>
/// 角色
/// </summary>
public class Role
{
    /// <summary>
    /// 角色編號
    /// </summary>
    [Key]
    [Column("id")]
    [Display(Name = "角色編號")]
    public int Id { get; set; }

    /// <summary>
    /// 角色名稱
    /// </summary>
    [Column("role_name")]
    [Display(Name = "角色名稱")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string RoleName { get; set; } = null!;

    /// <summary>
    /// 角色群組
    /// </summary>
    [Column("role_group")]
    [Display(Name = "角色群組")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string RoleGroup { get; set; } = null!;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
