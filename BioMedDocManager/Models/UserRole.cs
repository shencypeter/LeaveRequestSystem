using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BioMedDocManager.Models;

/// <summary>
/// 使用者角色
/// </summary>
public class UserRole
{
    /// <summary>
    /// 使用者編號
    /// </summary>
    [Column("user_id")]
    [Display(Name = "使用者編號")]
    public int UserId { get; set; }

    /// <summary>
    /// 角色編號
    /// </summary>
    [Column("role_id")]
    [Display(Name = "角色編號")]
    public int RoleId { get; set; }

    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;
}
