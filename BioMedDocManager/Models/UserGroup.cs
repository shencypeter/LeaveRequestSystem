using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 使用者群組
/// </summary>
public class UserGroup : ISoftDelete
{
    /// <summary>
    /// 群組編號
    /// </summary>
    [Key]
    [Display(Name = "群組編號")]
    public int UserGroupId { get; set; }

    /// <summary>
    /// 群組名稱
    /// </summary>
    [Display(Name = "群組名稱")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string UserGroupName { get; set; } = null!;

    /// <summary>
    /// 群組說明
    /// </summary>
    [Display(Name = "群組說明")]
    [StringLength(255, ErrorMessage = "{0}最多{1}字元")]
    public string? UserGroupDescription { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    [Display(Name = "建立時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// 建立人
    /// </summary>
    [Display(Name = "建立人")]
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    [Display(Name = "更新時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    [Display(Name = "更新人")]
    public int? UpdatedBy { get; set; }

    /// <summary>
    /// 刪除時間
    /// </summary>
    [Display(Name = "刪除時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 刪除人
    /// </summary>
    [Display(Name = "刪除人")]
    public int? DeletedBy { get; set; }

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
