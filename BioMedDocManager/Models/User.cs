using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BioMedDocManager.Models;

/// <summary>
/// 使用者
/// </summary>
public class User
{
    /// <summary>
    /// 使用者編號
    /// </summary>
    [Key]
    [Column("id")]
    [Display(Name = "使用者編號")]
    [DisplayFormat(NullDisplayText = "無")]
    public int Id { get; set; }

    /// <summary>
    /// 帳號
    /// </summary>    
    [Column("username")]
    [Display(Name = "帳號(工號)")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string UserName { get; set; } = null!;

    /// <summary>
    /// 密碼
    /// </summary>    
    [Column("password")]
    [Display(Name = "密碼")]
    [StringLength(255, ErrorMessage = "{0}最多{1}字元")]
    public string Password { get; set; } = null!;

    /// <summary>
    /// 姓名
    /// </summary>    
    [Column("full_name")]
    [Display(Name = "姓名")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string FullName { get; set; } = null!;

    /// <summary>
    /// 是否啟用
    /// </summary>    
    [Column("is_active")]
    [Display(Name = "是否啟用")]
    [DisplayFormat(NullDisplayText = "無")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 建立時間
    /// </summary>
    [Column("created_at")]
    [Display(Name = "建立時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "無")]
    public DateTime? CreatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
