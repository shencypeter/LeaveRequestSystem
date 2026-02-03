using BioMedDocManager.Enums;
using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 使用者
/// </summary>
public class User : AuditableEntity, IAccount
{
    /// <summary>
    /// 使用者編號
    /// </summary>
    [Key]
    [Display(Name = "User.UserId")]
    public long UserId { get; set; }

    /// <summary>
    /// 帳號(工號)
    /// </summary>
    [Display(Name = "User.UserAccount")]
    [StringLength(100, ErrorMessage = "Validation.StringLength")]
    public string UserAccount { get; set; } = null!;

    /// <summary>
    /// 密碼雜湊
    /// </summary>
    [Display(Name = "User.UserPasswordHash")]
    [StringLength(255, ErrorMessage = "Validation.StringLength")]
    public string UserPasswordHash { get; set; } = null!;

    /// <summary>
    /// 姓名
    /// </summary>
    [Display(Name = "User.UserFullName")]
    [StringLength(100, ErrorMessage = "Validation.StringLength")]
    public string UserFullName { get; set; } = null!;

    /// <summary>
    /// 職稱
    /// </summary>
    [Display(Name = "User.UserJobTitle")]
    [StringLength(100, ErrorMessage = "Validation.StringLength")]
    public string? UserJobTitle { get; set; }

    /// <summary>
    /// 電子郵件
    /// </summary>
    [Display(Name = "User.UserEmail")]
    [StringLength(255, ErrorMessage = "Validation.StringLength")]
    public string UserEmail { get; set; } = null!;

    /// <summary>
    /// 聯絡電話
    /// </summary>
    [Display(Name = "User.UserPhone")]
    [StringLength(50, ErrorMessage = "Validation.StringLength")]
    public string? UserPhone { get; set; }

    /// <summary>
    /// 手機
    /// </summary>
    [Display(Name = "User.UserMobile")]
    [StringLength(50, ErrorMessage = "Validation.StringLength")]
    public string? UserMobile { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "User.UserIsActive")]
    public bool UserIsActive { get; set; } = true;

    /// <summary>
    /// 是否啟用 文字
    /// </summary>
    [NotMapped]
    [Display(Name = "User.UserIsActive")]
    public string UserIsActiveText =>
        UserIsActive
            ? (Loc?.T("Common.Enabled") ?? "Enabled")
            : (Loc?.T("Common.Disabled") ?? "Disabled");

    /// <summary>
    /// 是否鎖定
    /// </summary>
    [Display(Name = "User.UserIsLocked")]
    public bool UserIsLocked { get; set; } = false;

    /// <summary>
    /// 是否鎖定 文字
    /// </summary>
    [NotMapped]
    [Display(Name = "User.UserIsLocked")]
    public string UserIsLockedText =>
        UserIsLocked
            ? (Loc?.T("Common.Locked") ?? "Locked")
            : (Loc?.T("Common.Unlocked") ?? "Unlocked");

    /// <summary>
    /// 鎖定解除時間
    /// </summary>
    [Display(Name = "User.UserLockedUntil")]
    public DateTime? UserLockedUntil { get; set; }

    /// <summary>
    /// 登入失敗次數
    /// </summary>
    [Display(Name = "User.UserLoginFailedCount")]
    public int UserLoginFailedCount { get; set; } = 0;

    /// <summary>
    /// 最後登入時間
    /// </summary>
    [Display(Name = "User.UserLastLoginAt")]
    public DateTime? UserLastLoginAt { get; set; }

    /// <summary>
    /// 最後登入 IP
    /// </summary>
    [Display(Name = "User.UserLastLoginIp")]
    [StringLength(50, ErrorMessage = "Validation.StringLength")]
    public string? UserLastLoginIp { get; set; }

    /// <summary>
    /// 密碼最後修改時間
    /// </summary>
    [Display(Name = "User.UserPasswordChangedAt")]
    public DateTime? UserPasswordChangedAt { get; set; }

    /// <summary>
    /// 帳號狀態
    /// </summary>
    [Display(Name = "User.UserStatus")]
    public AccountStatus? UserStatus { get; set; } = AccountStatus.Active;

    /// <summary>
    /// 備註
    /// </summary>
    [Display(Name = "User.UserRemarks")]
    [StringLength(255, ErrorMessage = "Validation.StringLength")]
    public string? UserRemarks { get; set; }

    /// <summary>
    /// TOTP Secret（綁定用）
    /// </summary>
    [Display(Name = "User.UserTotpSecret")]
    [MaxLength(128)]
    public string? UserTotpSecret { get; set; }

    /// <summary>
    /// 部門
    /// </summary>
    [Display(Name = "User.DepartmentId")]
    public long? DepartmentId { get; set; }

    /// <summary>
    /// 是否啟用 文字
    /// </summary>
    [NotMapped]
    [Display(Name = "Department.DepartmentName")]
    public string DepartmentName => Department == null ? Loc?.T("Common.None") : Department.DepartmentName;

    /// <summary>
    /// 直連UserRoles-系統角色List
    /// </summary>
    [NotMapped]
    public List<string> RoleCode =>
        UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.RoleCode)
            .ToList();

    /// <summary>
    /// 直連UserRoles-系統角色文字串
    /// </summary>
    [NotMapped]
    [Display(Name = "User.RoleCodeList")]
    public string RoleCodeList =>
        string.Join("、",
            UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => $"{ur.Role!.RoleGroup}-{ur.Role!.RoleCode}")
                .Distinct()
        );

    /// <summary>
    /// 使用者群組（名稱清單）
    /// </summary>
    [NotMapped]
    [Display(Name = "User.UserGroupList")]
    public List<string> UserGroupList =>
        UserGroupMembers?
            .Where(m => m.UserGroup != null)
            .Select(m => m.UserGroup!.UserGroupCode)
            .Distinct()
            .ToList() ?? new();

    /// <summary>
    /// 群組權限（格式：群組名稱-權限）
    /// </summary>
    [NotMapped]
    [Display(Name = "User.UserGroupRoleList")]
    public List<string> UserGroupRoleList =>
        UserGroupMembers?
            .Where(m => m.UserGroup != null)
            .SelectMany(m => m.UserGroup!.UserGroupRoles)
            .Where(gr => gr.Role != null && gr.UserGroup != null)
            .Select(gr => $"{gr.UserGroup!.UserGroupCode}-{gr.Role!.RoleCode}")
            .Distinct()
            .ToList() ?? new();

    /// <summary>
    /// 使用者角色清單-關聯
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// 使用者群組成員-關聯
    /// </summary>
    public ICollection<UserGroupMember> UserGroupMembers { get; set; } = new List<UserGroupMember>();

    /// <summary>
    /// 部門-關聯
    /// </summary>
    public Department? Department { get; set; }

    /// <summary>
    /// 使用者歷史密碼-關聯
    /// </summary>
    public ICollection<UserPasswordHistory> UserPasswordHistories { get; set; } = new List<UserPasswordHistory>();

    // ===== IAccount 介面實作 =====

    /// <summary>
    /// 取得帳號類型
    /// </summary>
    public AccountType GetAccountType() => AccountType.Admin;

    /// <summary>
    /// 取得使用者ID
    /// </summary>
    public long GetUId() => UserId;

    /// <summary>
    /// 取得加密後的密碼
    /// </summary>
    public string GetEncryptedPassword() => UserPasswordHash;

    /// <summary>
    /// 取得使用者帳號
    /// </summary>
    public string GetAccount() => UserAccount;
}
