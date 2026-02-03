using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BioMedDocManager.Models;

/// <summary>
/// 舊表：使用者角色 (已改用 UserGroupRole 管理，不建議使用)
/// </summary>
public class UserRole
{
    /// <summary>
    /// 使用者編號
    /// </summary>
    [Display(Name = "使用者編號")]
    public long UserId { get; set; }

    /// <summary>
    /// 角色編號
    /// </summary>
    [Display(Name = "角色編號")]
    public long RoleId { get; set; }
    
    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;
}
