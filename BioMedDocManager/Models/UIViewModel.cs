using BioMedDocManager.Enums;
using BioMedDocManager.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BioMedDocManager.Models
{
    public class UIViewModel
    {
    }

    /// <summary>
    /// 分頁 
    /// </summary>
    /// <remarks>實作 model 繼承 pagination 就不用自己寫分頁的屬性</remarks>
    public class Pagination
    {
        /// <summary>
        /// 頁面編號
        /// </summary>
        [NotMapped]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 頁面大小
        /// </summary>
        [NotMapped]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 排序欄位
        /// </summary>
        [NotMapped]
        public string OrderBy
        {
            get; set;
        }

        /// <summary>
        /// 排序方向 asc / desc
        /// </summary>
        [NotMapped]
        public string SortDir { get; set; }
    }

    /// <summary>
    /// 下拉式選單
    /// </summary>
    public class SelectOption
    {
        /// <summary>
        /// 選項值
        /// </summary>
        public string OptionValue { get; set; }

        /// <summary>
        /// 顯示文字
        /// </summary>
        public string OptionText { get; set; } = null!;
    }

    /// <summary>
    /// 頁面連結物件
    /// </summary>
    public class PageLink
    {
        /// <summary>
        /// 控制器
        /// </summary>
        public string Controller { get; set; }

        /// <summary>
        /// 動作
        /// </summary>
        public string Action { get; set; } = "Index";

        /// <summary>
        /// 標籤
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 允許的角色
        /// </summary>
        public string[] Roles { get; set; } = Array.Empty<string>();

    }

    /// <summary>
    /// 新增使用者
    /// </summary>
    public class CreateUserViewModel
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
        [Required(ErrorMessage = "Validation.Required")]
        [StringLength(100, ErrorMessage = "Validation.StringLength")]
        public string UserAccount { get; set; } = null!;

        /// <summary>
        /// 姓名
        /// </summary>
        [Display(Name = "User.UserFullName")]
        [Required(ErrorMessage = "Validation.Required")]
        [StringLength(100, ErrorMessage = "Validation.StringLength")]
        public string UserFullName { get; set; } = null!;

        /// <summary>
        /// 職稱
        /// </summary>
        [Display(Name = "User.UserJobTitle")]
        [StringLength(100, ErrorMessage = "Validation.StringLength")]
        public string? UserJobTitle { get; set; }

        /// <summary>
        /// 部門
        /// </summary>
        [Display(Name = "User.DepartmentId")]
        public long? DepartmentId { get; set; }

        /// <summary>
        /// 電子郵件
        /// </summary>
        [Display(Name = "User.UserEmail")]
        [Required(ErrorMessage = "Validation.Required")]
        [EmailAddress(ErrorMessage = "Validation.EmailAddress")]
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
        /// 密碼
        /// </summary>
        [Display(Name = "User.UserPasswordHash")]
        [Required(ErrorMessage = "Validation.Required")]
        [DataType(DataType.Password)]
        [StringLength(255, ErrorMessage = "Validation.StringLength")]
        public string UserPasswordHash { get; set; } = null!;

        /// <summary>
        /// 確認密碼
        /// </summary>
        [Display(Name = "User.UserConfirmPasswordHash")]
        [Required(ErrorMessage = "Validation.Required")]
        [DataType(DataType.Password)]
        [Compare(nameof(UserPasswordHash), ErrorMessage = "Validation.Compare")]
        public string UserConfirmPasswordHash { get; set; } = null!;

        /// <summary>
        /// 是否啟用
        /// </summary>
        [Display(Name = "User.UserIsActive")]
        public bool UserIsActive { get; set; } = true;

        /// <summary>
        /// 是否鎖定
        /// </summary>
        [Display(Name = "User.UserIsLocked")]
        public bool UserIsLocked { get; set; } = false;

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
        /// 部門-關聯
        /// </summary>
        public Department Department { get; set; } = new Department();
    }

    /// <summary>
    /// 變更密碼
    /// </summary>
    public class ChangePasswordViewModel
    {
        /// <summary>
        /// 使用者Id
        /// </summary>
        [Display(Name = "User.UserId")]
        public long UserId { get; set; }

        /// <summary>
        /// 使用者帳號(工號)
        /// </summary>
        [Display(Name = "User.UserAccount")]
        public string UserAccount { get; set; } = null!;

        /// <summary>
        /// 使用者姓名
        /// </summary>
        [Display(Name = "User.UserFullName")]
        public string UserFullName { get; set; } = null!;

        /// <summary>
        /// 原密碼
        /// </summary>
        [Display(Name = "User.UserCurrentPassword")]
        [Required(ErrorMessage = "Validation.Required")]
        [DataType(DataType.Password)]
        public string UserCurrentPassword { get; set; } = null!;

        /// <summary>
        /// 新密碼
        /// </summary>
        [Display(Name = "User.UserNewPasswordHash")]
        [Required(ErrorMessage = "Validation.Required")]
        [DataType(DataType.Password)]
        [StringLength(255, ErrorMessage = "Validation.StringLength")]
        public string UserNewPasswordHash { get; set; } = null!;

        /// <summary>
        /// 確認新密碼
        /// </summary>
        [Display(Name = "User.UserConfirmPasswordHash")]
        [Required(ErrorMessage = "Validation.Required")]
        [DataType(DataType.Password)]
        [Compare(nameof(UserNewPasswordHash), ErrorMessage = "Validation.Compare")]
        public string UserConfirmPasswordHash { get; set; } = null!;
    }

    /// <summary>
    /// 使用者群組編輯畫面
    /// </summary>
    public class UserGroupsEditViewModel
    {
        /// <summary>
        /// 使用者Id
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// 使用者帳號
        /// </summary>
        [Display(Name = "User.UserAccount")]
        public string UserAccount { get; set; } = string.Empty;

        /// <summary>
        /// 使用者姓名
        /// </summary>
        [Display(Name = "User.UserFullName")]
        public string UserFullName { get; set; } = string.Empty;

        /// <summary>
        /// 使用者群組
        /// </summary>
        [Display(Name = "User.UserGroupRoleList")]
        public List<long> SelectedUserGroupIds { get; set; } = new();

        /// <summary>
        /// 所有使用者群組List
        /// </summary>
        public List<SelectListItem> AllUserGroups { get; set; } = new();

        /// <summary>
        /// 預覽區：有效角色
        /// </summary>
        public List<EffectiveRoleViewModel> EffectiveRoles { get; set; } = new();

        /// <summary>
        /// 預覽區：有效權限（平面，View 端用 Resource 分組）
        /// </summary>
        public List<EffectivePermissionViewModel> EffectivePermissions { get; set; } = new();
    }

    /// <summary>
    /// 使用者群組編輯儲存模型
    /// </summary>
    public class UserGroupsEditPostViewModel
    {
        /// <summary>
        /// 使用者Id
        /// </summary>
        [Required]
        public long UserId { get; set; }

        /// <summary>
        /// 被選擇到的使用者群組
        /// </summary>
        public List<long>? SelectedUserGroupIds { get; set; }

    }

    /// <summary>
    /// 使用者明細畫面用 ViewModel
    /// </summary>
    /// <summary>
    /// 使用者明細頁用 ViewModel
    ///   上半部：User 基本資料
    ///   下半部：群組 / 角色 / 權限 (沿用 ComputePreviewAsync 結果)
    /// </summary>
    public class UserDetailsViewModel
    {
        public User User { get; set; } = null!;

        /// <summary>
        /// 使用者已屬於的群組名稱清單
        /// </summary>
        public List<string> UserGroups { get; set; } = new();

        /// <summary>
        /// 有效角色 (從 ComputePreviewAsync 來)
        /// </summary>
        public List<EffectiveRoleViewModel> EffectiveRoles { get; set; } = new();

        /// <summary>
        /// 有效權限 (從 ComputePreviewAsync 來)
        /// </summary>
        public List<EffectivePermissionViewModel> EffectivePermissions { get; set; } = new();
    }

    /// <summary>
    /// 有效角色
    /// </summary>
    public class EffectiveRoleViewModel
    {
        public long RoleId { get; set; }
        public string RoleCode { get; set; } = "";
        public string RoleGroup { get; set; } = "";
        public List<long> FromUserGroupIds { get; set; } = new();
    }

    /// <summary>
    /// 有效權限
    /// </summary>
    public class EffectivePermissionViewModel
    {
        public long ResourceId { get; set; }
        public string ResourceKey { get; set; } = "";
        public string ResourceDisplayName { get; set; } = "";
        public long AppActionId { get; set; }
        public string AppActionCode { get; set; } = "";
        public string AppActionDisplayName { get; set; } = "";
        public List<long> FromRoleIds { get; set; } = new();
    }

    /// <summary>
    /// 群組角色設定用 ViewModel
    /// </summary>
    public class UserGroupRoleEditViewModel
    {
        /// <summary>
        /// 群組 Id
        /// </summary>
        public long UserGroupId { get; set; }

        /// <summary>
        /// 群組名稱（顯示用）
        /// </summary>
        public string? UserGroupCode { get; set; }

        /// <summary>
        /// 使用者選取的角色 Id 清單
        /// </summary>
        public List<long> SelectedRoleIds { get; set; } = new();

        /// <summary>
        /// 所有可選角色清單（之後 View 可做 checkbox / multi-select）
        /// </summary>
        public List<Role> AllRoles { get; set; } = new();


        /// <summary>
        /// 預覽區：有效權限（平面，View 端用 Resource 分組）
        /// </summary>
        public List<PreviewPermissionViewModel> EffectivePermissions { get; set; } = new();
    }

    /// <summary>
    /// AppAction 在 RolePermission 中的使用情況：Resource + Role
    /// </summary>
    public class AppActionRoleUsageViewModel
    {
        public long ResourceId { get; set; }
        public string? ResourceKey { get; set; }
        public string? ResourceDisplayName { get; set; }

        public long RoleId { get; set; }
        public string? RoleGroup { get; set; }
        public string? RoleCode { get; set; }
    }

    /// <summary>
    /// 錯誤畫面
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// 預設狀態碼
        /// </summary>
        public int StatusCode { get; set; } = 500;

        /// <summary>
        /// 預設標題
        /// </summary>
        public string Title { get; set; } = "錯誤";

        /// <summary>
        /// 預設訊息
        /// </summary>
        public string Message { get; set; } = "頁面發生錯誤。";

        /// <summary>
        /// 預設icon class
        /// </summary>
        public string IconClass { get; set; } = "fas fa-exclamation-triangle";

        /// <summary>
        /// 預設轉跳網址路徑
        /// </summary>
        public string RedirectUrl { get; set; } = "/";

        /// <summary>
        /// 預設倒數秒數
        /// </summary>
        public int Seconds { get; set; } = 5;


        public static ErrorViewModel FromStatusCode(int code) => code switch
        {
            401 => new ErrorViewModel
            {
                StatusCode = 401,
                Title = "401未經授權",
                Message = "抱歉，您尚未獲得存取此頁面的授權。",
                IconClass = "fas fa-user-lock"
            },
            403 => new ErrorViewModel
            {
                StatusCode = 403,
                Title = "403禁止存取",
                Message = "您沒有權限檢視這個頁面。",
                IconClass = "fas fa-ban"
            },
            404 => new ErrorViewModel
            {
                StatusCode = 404,
                Title = "404找不到頁面",
                Message = "您要檢視的頁面不存在或已被移除。",
                IconClass = "fas fa-search"
            },
            500 => new ErrorViewModel
            {
                StatusCode = 500,
                Title = "500伺服器錯誤",
                Message = "發生錯誤，我們正在努力修復中。",
                IconClass = "fas fa-server"
            },
            _ => new ErrorViewModel
            {
                StatusCode = code,
                Title = "錯誤",
                Message = "頁面發生錯誤。",
                IconClass = "fas fa-exclamation-triangle"
            }
        };
    }

    /// <summary>
    /// 帳號管理
    /// </summary>
    public class AccountViewModel : Pagination
    {
        /// <summary>
        /// 使用者Id
        /// </summary>
        [Display(Name = "User.UserId")]
        public long UserId { get; set; }

        /// <summary>
        /// 工號
        /// </summary>
        [Display(Name = "User.UserAccount")]
        public string UserAccount { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Display(Name = "User.UserFullName")]
        [StringLength(100, ErrorMessage = "Validation.StringLength")]
        public string UserFullName { get; set; }

        /// <summary>
        /// 職稱
        /// </summary>
        [Display(Name = "User.UserJobTitle")]
        [StringLength(100, ErrorMessage = "Validation.StringLength")]
        public string? UserJobTitle { get; set; }

        /// <summary>
        /// 部門
        /// </summary>
        [Display(Name = "User.DepartmentId")]
        public long? DepartmentId { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [Display(Name = "User.UserEmail")]
        [EmailAddress(ErrorMessage = "Validation.EmailAddress")]
        [StringLength(255, ErrorMessage = "Validation.StringLength")]
        public string UserEmail { get; set; }

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
        /// 角色群組(List)
        /// </summary>
        [Display(Name = "User.RoleCodeList")]
        public List<long>? RoleId { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "AuditableEntity.CreatedAt")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        [Display(Name = "User.UserIsActive")]
        public bool? UserIsActive { get; set; }

        /// <summary>
        /// 是否鎖定
        /// </summary>
        [Display(Name = "User.UserIsLocked")]
        public bool? UserIsLocked { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        [Display(Name = "User.UserStatus")]
        public AccountStatus UserStatus { get; set; } = AccountStatus.Active;

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "User.UserRemarks")]
        [StringLength(255, ErrorMessage = "Validation.StringLength")]
        public string? UserRemarks { get; set; }
    }


    /// <summary>
    /// 使用者權限預覽請求
    /// </summary>
    public class PreviewPermissionsRequestViewModel
    {
        public long UserId { get; set; }
        public List<long>? SelectedUserGroupIds { get; set; }
    }

    /// <summary>
    /// 角色預覽時的來源群組
    /// </summary>
    public class PreviewRoleSourceGroupViewModel
    {
        public long UserGroupId { get; set; }
        public string UserGroupCode { get; set; } = "";
    }
    /// <summary>
    /// 角色預覽DTO
    /// </summary>
    public class PreviewRoleViewModel
    {
        public long RoleId { get; set; }
        public string RoleCode { get; set; } = "";
        public string RoleCodeName { get; set; } = "";
        public string RoleGroup { get; set; } = "";
        public string RoleGroupName { get; set; } = "";
        public bool IsNew { get; set; }

        public List<PreviewRoleSourceGroupViewModel> FromGroups { get; set; } = new();
    }

    /// <summary>
    /// 角色預覽權限DTO
    /// </summary>
    public class PreviewPermissionsViewModel
    {
        public long UserGroupId { get; set; }

        public List<long>? SelectedRoleIds { get; set; }
    }

    /// <summary>
    /// 權限預覽DTO
    /// </summary>
    public class PreviewPermissionViewModel
    {
        public long ResourceId { get; set; }
        public string ResourceKey { get; set; } = "";
        public string ResourceDisplayName { get; set; } = "";
        public long AppActionId { get; set; }        
        public string AppActionCode { get; set; } = "";
        public string AppActionDisplayName { get; set; } = "";
        public bool IsNew { get; set; }
        public int AppActionOrder { get; set; }
    }

    public class RoleUsageUserViewModel
    {
        public long UserId { get; set; }
        public string UserAccount { get; set; } = "";
        public string UserFullName { get; set; } = "";
    }

    public class RoleUsageGroupViewModel
    {
        public long UserGroupId { get; set; }
        public string UserGroupCode { get; set; } = "";
        public string? UserGroupDescription { get; set; }
    }

    /// <summary>
    /// 用來描述一個 Role 的「權限編輯」畫面
    /// </summary>
    public class RolePermissionEditViewModel
    {
        /// <summary>
        /// 角色編號
        /// </summary>
        [Display(Name = "Role.RoleId")] 
        public long RoleId { get; set; }

        /// <summary>
        /// 角色名稱（純顯示用）
        /// </summary>
        [Display(Name = "Role.RoleCode")]
        public string RoleCode { get; set; } = string.Empty;

        /// <summary>
        /// 可選擇的資源清單（只放啟用中的 Resource）
        /// </summary>
        public List<Resource> Resources { get; set; } = new();

        /// <summary>
        /// 可選擇的動作清單（AppAction）
        /// </summary>
        public List<AppAction> AppActions { get; set; } = new();

        /// <summary>
        /// 當前這個角色已存在的 RolePermission key 清單（"resourceId:appActionId"）
        /// 主要用來在 View 端預設打勾 & 在 POST 回傳使用。
        /// </summary>
        [Display(Name = "Role.EditPermission.PermissionSetting")] 
        public List<string> SelectedPermissionKeys { get; set; } = new();
    }

    /// <summary>
    /// 刪除 Resource 時，顯示「被哪些 UserGroup / Role 使用到」
    /// </summary>
    public class ResourceGroupUsageViewModel
    {
        public long UserGroupId { get; set; }
        public string UserGroupCode { get; set; } = string.Empty;
        public string? UserGroupDescription { get; set; }

        public long RoleId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleGroup { get; set; } = string.Empty;

        /// <summary>
        /// 是否有指定群組 (true=有群組; false=未指定群組)
        /// </summary>
        public bool HasGroup { get; set; }
    }

    public class MenuItemGroupViewModel
    {
        /// <summary>
        /// DB Localizer（供 NotMapped 計算屬性使用）
        /// Controller/Service 在取資料後可指派：entity.Loc = _loc;
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public IDbLocalizer? Loc { get; set; }

        /// <summary>
        /// 父層選單編號
        /// </summary>
        public long? MenuItemParentId { get; set; }

        /// <summary>
        /// 父層選單本身
        /// </summary>
        public MenuItem? Parent { get; set; }

        /// <summary>
        /// 底下的子選單
        /// </summary>
        public List<MenuItem> Children { get; set; } = new();
    }

    /// <summary>
    /// 系統參數查詢 ViewModel
    /// </summary>
    public class ParameterQueryViewModel : Pagination
    {
        /// <summary>
        /// 參數代碼（程式使用）
        /// </summary>
        [Display(Name = "參數代碼")]
        [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
        public string? ParameterCode { get; set; }

        /// <summary>
        /// 參數名稱（顯示用）
        /// </summary>
        [Display(Name = "參數名稱")]
        [StringLength(200, ErrorMessage = "{0}最多{1}字元")]
        public string? ParameterName { get; set; }

        /// <summary>
        /// 參數格式（text / int / html / json）
        /// </summary>
        [Display(Name = "參數格式")]
        [StringLength(20, ErrorMessage = "{0}最多{1}字元")]
        public string? ParameterFormat { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        [Display(Name = "是否啟用")]
        public bool? ParameterIsActive { get; set; }
    }

    /// <summary>
    /// 密碼設定選項
    /// </summary>
    public class PasswordPolicy
    {
        public bool PolicyEnabled { get; set; }
        public int MinLength { get; set; }
        public bool RequireUpper { get; set; }
        public bool RequireLower { get; set; }
        public bool RequireDigit { get; set; }
        public bool RequireSpecial { get; set; }
        public string SpecialCharSets { get; set; } = "!@#$%^&*()-_=+[]{}|;:'\",.<>/?`~\\";
        public int HistoryCount { get; set; }
        public int MinAgeDays { get; set; }

        // 登入失敗鎖定
        public int FailedLimit { get; set; }          // int.MaxValue = 不鎖定
        public int LockMinutes { get; set; }          // 0 = 不鎖定

        // 密碼過期 / 首次登入強制改密碼
        public int? PasswordExpireDays { get; set; }  // >0 代表啟用過期機制
        public bool ForceChangeFirstLoginFlag { get; set; }

        // 2FA
        public bool Sec2faEnabled { get; set; }
        public bool Sec2faEmailEnabled { get; set; }
        public bool Sec2faTotpEnabled { get; set; }

    }

    /// <summary>
    /// 二階段驗證暫存狀態（存在 Session）
    /// </summary>
    public class TwoFactorState
    {
        public long UserId { get; set; }
        public string UserAccount { get; set; } = string.Empty;

        public bool CanUseEmail { get; set; }
        public bool CanUseTotp { get; set; }

        // Email OTP 相關
        public string? DefaultProvider { get; set; } //預設提供者
        public string? EmailOtpHash { get; set; }        // OTP 的雜湊（避免明碼放 Session）
        public DateTime? EmailOtpExpiresAt { get; set; } // 有效期限
        public int EmailOtpSendCount { get; set; }       // 寄送次數（防濫用）
        public int EmailOtpVerifyFailCount { get; set; } // 驗證失敗次數（防暴力破解）

        // 之後用來 Redirect 回原頁
        public string? ReturnUrl { get; set; }
        

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool MustChangePassword { get; set; }
        public bool RequireFirstLoginChange { get; set; }
        public bool RequireExpireChange { get; set; }
    }

    /// <summary>
    /// TOTP 註冊流程用的 Session 暫存狀態
    /// （避免還沒確認前就寫入 DB）
    /// </summary>
    public class TotpSetupState
    {
        public long UserId { get; set; }

        /// <summary>
        /// Base32 編碼的 TOTP Secret（給 Google Authenticator 用）
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;

        public string AccountLabel { get; set; } = string.Empty;

        /// <summary>
        /// otpauth://totp/... URI，用來產 QR Code
        /// </summary>
        public string OtpauthUri { get; set; } = string.Empty;
    }

    /// <summary>
    /// TOTP 註冊頁的 ViewModel
    /// </summary>
    public class TotpSetupViewModel
    {
        [Display(Name = "User.UserAccount")]
        public string UserAccount { get; set; } = string.Empty;

        [Display(Name = "Totp.Issuer")]
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// 顯示給使用者的 Base32 Secret（手動輸入用）
        /// </summary>
        [Display(Name = "Totp.Secret")]
        public string Secret { get; set; } = string.Empty;

        [Display(Name = "Totp.OtpauthUri")]
        public string OtpauthUri { get; set; } = string.Empty;

        /// <summary>
        /// 使用者用 Authenticator App 產生的 6 碼驗證碼
        /// </summary>
        [Required(ErrorMessage = "Validation.Required")]
        [Display(Name = "Totp.Code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Validation.StringLength")]
        public string Code { get; set; } = string.Empty;
    }


    /// <summary>
    /// 通用型：是否選單<select>
    /// </summary>
    public class BoolNullableSelectVm
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool? Value { get; set; }

        // 額外 class（可選）
        public string? CssClass { get; set; }

        // 額外屬性（可選）：例如 disabled、data-xxx、aria-xxx
        // 直接傳入完整片段：@"data-foo=""bar"" disabled"
        public string? Attributes { get; set; }

        // 讓 Attributes 不會輸出 "System.String"
        public Microsoft.AspNetCore.Html.IHtmlContent AttributesHtml =>
            string.IsNullOrWhiteSpace(Attributes)
                ? Microsoft.AspNetCore.Html.HtmlString.Empty
                : new Microsoft.AspNetCore.Html.HtmlString(Attributes);
    }

    public class PermissionRequirement : IAuthorizationRequirement
    {
        // 不需要任何屬性，空殼就可以
    }

    /// <summary>
    /// 多語系文字查詢 ViewModel
    /// </summary>
    public class LocalizationStringQueryViewModel : Pagination
    {
        /// <summary>
        /// Key（例如：Parameter.SEC_PASSWORD_MIN_LENGTH.Label）
        /// </summary>
        [Display(Name = "字串鍵值")]
        [StringLength(200, ErrorMessage = "{0}最多{1}字元")]
        public string? LocalizationStringKey { get; set; }

        /// <summary>
        /// Culture（例如：zh-TW / en-US）
        /// </summary>
        [Display(Name = "語系")]
        [StringLength(20, ErrorMessage = "{0}最多{1}字元")]
        public string? LocalizationStringCulture { get; set; }

        /// <summary>
        /// 顯示文字
        /// </summary>
        [Display(Name = "顯示文字")]
        [StringLength(500, ErrorMessage = "{0}最多{1}字元")]
        public string? LocalizationStringValue { get; set; }

        /// <summary>
        /// 分類（例如：Security / Common / Menu）
        /// </summary>
        [Display(Name = "分類")]
        [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
        public string? LocalizationStringCategory { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        [Display(Name = "是否啟用")]
        public bool? LocalizationStringIsActive { get; set; }
    }







































    // ====== 範例 View Model ======

    /// <summary>
    /// 文件列
    /// </summary>
    public class DocRowViewModel
    {
        /// <summary>
        /// 表單編號
        /// </summary>
        public string OriginalDocNo { get; set; } = "";

        /// <summary>
        /// 第1階層：MBP
        /// </summary>
        public string? Level1 { get; set; }

        /// <summary>
        /// 第2階層：QR01、CS等
        /// </summary>
        public string? Level2 { get; set; }

        /// <summary>
        /// 第3階層：AP02、MP05、TRO01(因為是3階)
        /// </summary>
        public string? Level3 { get; set; }

        /// <summary>
        /// 第4階層：null 或 TRO01(因為是4階)
        /// </summary>
        public string? Level4 { get; set; }

        /// <summary>
        /// TR階層：TRO01、TRO02等
        /// </summary>
        public string? TRCode { get; set; }

        /// <summary>
        /// 版次(文字型態)
        /// </summary>
        public string DocVer { get; set; } = "0";

        /// <summary>
        /// 版次(數字型態)
        /// </summary>
        public double DocVerNumber { get; set; }

        /// <summary>
        /// 表單名稱
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 表單發行日期
        /// </summary>
        public DateTime? IssueDatetime { get; set; }
    }

    public sealed class IssueTableListIssueTableViewModel
    {
        public string? Name { get; set; }
        public DateTime? IssueDatetime { get; set; }
        public string? OriginalDocNo { get; set; }
        public string? DocVer { get; set; }
        public string? FileExtension { get; set; }

        public int IsLatest { get; set; } // 1=最新版, 0=非最新版
    }


}
