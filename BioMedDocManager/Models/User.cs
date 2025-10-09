using BioMedDocManager.Enum;
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
    /// 帳號(工號)
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
    /// 職稱
    /// </summary>
    [Column("job_title")]
    [Display(Name = "職稱")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// 部門名稱
    /// </summary>
    [Column("department_name")]
    [Display(Name = "部門名稱")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 電子郵件
    /// </summary>
    [Column("email")]
    [Display(Name = "電子郵件")]
    [StringLength(255, ErrorMessage = "{0}最多{1}字元")]
    public string Email { get; set; } = null!;

    /// <summary>
    /// 聯絡電話
    /// </summary>
    [Column("phone")]
    [Display(Name = "聯絡電話")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? Phone { get; set; }

    /// <summary>
    /// 手機
    /// </summary>
    [Column("mobile")]
    [Display(Name = "手機")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? Mobile { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>    
    [Column("is_active")]
    [Display(Name = "是否啟用")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 是否鎖定
    /// </summary>
    [Column("is_locked")]
    [Display(Name = "是否鎖定")]
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// 登入失敗次數
    /// </summary>
    [Column("login_failed_count")]
    [Display(Name = "登入失敗次數")]
    public int LoginFailedCount { get; set; } = 0;

    /// <summary>
    /// 最後登入時間
    /// </summary>
    [Column("last_login_at")]
    [Display(Name = "最後登入時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 最後登入 IP
    /// </summary>
    [Column("last_login_ip")]
    [Display(Name = "最後登入 IP")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 密碼最後修改時間
    /// </summary>
    [Column("password_changed_at")]
    [Display(Name = "密碼最後修改時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>
    /// 狀態
    /// </summary>
    [Column("status")]
    [Display(Name = "帳號狀態")]
    public AccountStatus Status { get; set; } = AccountStatus.Active;

    /// <summary>
    /// 備註
    /// </summary>
    [Column("remarks")]
    [Display(Name = "備註")]
    [StringLength(255, ErrorMessage = "{0}最多{1}字元")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    [Column("created_at")]
    [Display(Name = "建立時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// 建立人
    /// </summary>
    [Column("created_by")]
    [Display(Name = "建立人")]
    public int? CreatedBy { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    [Column("updated_at")]
    [Display(Name = "更新時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    [Column("updated_by")]
    [Display(Name = "更新人")]
    public int? UpdatedBy { get; set; }

    /// <summary>
    /// 刪除時間
    /// </summary>
    [Column("deleted_at")]
    [Display(Name = "刪除時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 刪除人
    /// </summary>
    [Column("deleted_by")]
    [Display(Name = "刪除人")]
    public int? DeletedBy { get; set; }

    /// <summary>
    /// 使用者角色清單
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
