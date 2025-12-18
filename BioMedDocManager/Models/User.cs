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
    [Display(Name = "使用者編號")]
    [DisplayFormat(NullDisplayText = "無")]
    public int UserId { get; set; }

    /// <summary>
    /// 帳號(工號)
    /// </summary>
    [Display(Name = "帳號(工號)")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string UserAccount { get; set; } = null!;

    /// <summary>
    /// 密碼
    /// </summary>
    [Display(Name = "密碼")]
    [StringLength(255, ErrorMessage = "{0}最多{1}字元")]
    public string UserPasswordHash { get; set; } = null!;

    /// <summary>
    /// 姓名
    /// </summary>
    [Display(Name = "姓名")]
    [DisplayFormat(NullDisplayText = "無")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string UserFullName { get; set; } = null!;

    /// <summary>
    /// 職稱
    /// </summary>
    [Display(Name = "職稱")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string? UserJobTitle { get; set; }

    /// <summary>
    /// 電子郵件
    /// </summary>
    [Display(Name = "電子郵件")]
    [StringLength(255, ErrorMessage = "{0}最多{1}字元")]
    public string UserEmail { get; set; } = null!;

    /// <summary>
    /// 聯絡電話
    /// </summary>
    [Display(Name = "聯絡電話")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? UserPhone { get; set; }

    /// <summary>
    /// 手機
    /// </summary>
    [Display(Name = "手機")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? UserMobile { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "是否啟用")]
    public bool UserIsActive { get; set; } = true;

    /// <summary>
    /// 是否啟用 文字
    /// </summary>
    [NotMapped]
    [Display(Name = "是否啟用")]
    public string UserIsActiveText => UserIsActive ? "啟用" : "停用";

    /// <summary>
    /// 是否鎖定
    /// </summary>
    [Display(Name = "是否鎖定")]
    public bool UserIsLocked { get; set; } = false;

    /// <summary>
    /// 是否鎖定 文字
    /// </summary>
    [NotMapped]
    [Display(Name = "是否鎖定")]
    public string UserIsLockedText => UserIsLocked ? "已鎖定" : "未鎖定";

    /// <summary>
    /// 鎖定解除時間
    /// </summary>
    [Display(Name = "鎖定解除時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? UserLockedUntil { get; set; }

    /// <summary>
    /// 登入失敗次數
    /// </summary>
    [Display(Name = "登入失敗次數")]
    public int UserLoginFailedCount { get; set; } = 0;

    /// <summary>
    /// 最後登入時間
    /// </summary>
    [Display(Name = "最後登入時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? UserLastLoginAt { get; set; }

    /// <summary>
    /// 最後登入 IP
    /// </summary>
    [Display(Name = "最後登入 IP")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string? UserLastLoginIp { get; set; }

    /// <summary>
    /// 密碼最後修改時間
    /// </summary>
    [Display(Name = "密碼最後修改時間")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", NullDisplayText = "無")]
    public DateTime? UserPasswordChangedAt { get; set; }

    /// <summary>
    /// 狀態
    /// </summary>
    [Display(Name = "帳號狀態")]
    public AccountStatus? UserStatus { get; set; } = AccountStatus.Active;

    /// <summary>
    /// 備註
    /// </summary>
    [Display(Name = "備註")]
    [StringLength(255, ErrorMessage = "{0}最多{1}字元")]
    public string? UserRemarks { get; set; }

    /// <summary>
    /// TOTP驗證碼
    /// </summary>
    [MaxLength(128)]
    public string? UserTotpSecret { get; set; }

    /// <summary>
    /// 部門
    /// </summary>
    [Display(Name = "部門")]
    public int? DepartmentId { get; set; }

    /// <summary>
    /// 直連UserRoles-系統角色List
    /// </summary>
    [NotMapped]
    public List<string> RoleName =>
        UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role.RoleName)
            .ToList();

    /// <summary>
    /// 直連UserRoles-系統角色文字串
    /// </summary>
    [NotMapped]
    [Display(Name = "強制指定系統角色")]
    public string RoleNameList =>
        string.Join("、",
            UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => $"{ur.Role.RoleGroup}-{ur.Role.RoleName}")
                .Distinct()
        );

    /// <summary>
    /// 使用者群組（名稱清單）
    /// </summary>
    [NotMapped]
    [Display(Name = "使用者群組清單")]
    public List<string> UserGroupList =>
        UserGroupMembers?
            .Where(m => m.UserGroup != null)
            .Select(m => m.UserGroup!.UserGroupName)
            .Distinct()
            .ToList() ?? new();

    /// <summary>
    /// 群組權限（格式：群組名稱-權限）
    /// </summary>
    [NotMapped]
    [Display(Name = "使用者群組權限清單")]
    public List<string> UserGroupRoleList =>
        UserGroupMembers?
            .Where(m => m.UserGroup != null)
            .SelectMany(m => m.UserGroup!.UserGroupRoles)
            .Where(gr => gr.Role != null && gr.UserGroup != null)
            .Select(gr => $"{gr.UserGroup!.UserGroupName}-{gr.Role!.RoleName}")
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
    public Department Department { get; set; } = new Department();

/// <summary>
    /// 使用者歷史密碼-關聯
    /// </summary>
    public ICollection<UserPasswordHistory> UserPasswordHistories { get; set; } = new List<UserPasswordHistory>();
    //  ===== IAccount 介面實作 =====
    /// <summary>
    /// IAccount 介面實作-取得帳號類型
    /// </summary>
    /// <returns></returns>
    public AccountType GetAccountType()
    {
        return AccountType.Admin;
    }

    /// <summary>
    /// IAccount 介面實作-取得使用者ID
    /// </summary>
    /// <returns></returns>
    public int GetUId()
    {
        return this.UserId;
    }

    /// <summary>
    /// IAccount 介面實作-取得加密後的密碼
    /// </summary>
    /// <returns></returns>
    public string GetEncryptedPassword()
    {
        return this.UserPasswordHash;
    }

    /// <summary>
    /// IAccount 介面實作-取得使用者帳號
    /// </summary>
    /// <returns></returns>
    public string GetAccount()
    {
        return this.UserAccount;
    }

}
