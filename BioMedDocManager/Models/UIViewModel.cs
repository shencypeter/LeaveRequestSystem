using BioMedDocManager.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    /// 領用人下拉式選單
    /// </summary>
    public class SelectOption
    {
        /// <summary>
        /// 選項值(工號)
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
    public class CreateUser
    {
        /// <summary>
        /// 流水號
        /// </summary>
        [Key]
        [Column("id")]
        [Display(Name = "使用者編號")]
        public int Id { get; set; }

        /// <summary>
        /// 使用者帳號(工號)
        /// </summary>
        [Column("username")]
        [Display(Name = "帳號(工號)")]
        public string Username { get; set; } = null!;

        /// <summary>
        /// 姓名
        /// </summary>
        [Column("full_name")]
        [Display(Name = "姓名")]
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
        /// Email
        /// </summary>
        [Column("email")]
        [Display(Name = "Email")]
        [Required(ErrorMessage = "請輸入「Email」")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        [StringLength(255, ErrorMessage = "{0}最多{1}字元")]
        public string? Email { get; set; } = null!;

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
        /// 密碼
        /// </summary>
        [Required(ErrorMessage = "請輸入「密碼」")]
        [MinLength(8, ErrorMessage = "密碼長度至少需8個字元")]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string Password { get; set; }

        /// <summary>
        /// 確認密碼
        /// </summary>
        [Required(ErrorMessage = "請輸入「確認密碼」")]
        [Display(Name = "確認密碼")]
        [Compare(nameof(Password), ErrorMessage = "「密碼」與「確認密碼」不一致")]
        public string ConfirmPassword { get; set; } = null!;

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
        /// 狀態（例如 Active / Suspended）
        /// </summary>
        [Column("status")]
        [Display(Name = "狀態")]
        [StringLength(20, ErrorMessage = "{0}最多{1}字元")]
        public string? Status { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Column("remarks")]
        [Display(Name = "備註")]
        [StringLength(255, ErrorMessage = "{0}最多{1}字元")]
        public string? Remarks { get; set; }

        /// <summary>
        /// 角色群組(List)
        /// </summary>
        [Display(Name = "系統角色")]
        public List<string> RoleName { get; set; }

        /// <summary>
        /// 角色清單文字
        /// </summary>
        [Display(Name = "系統角色")]
        public string RoleNameList { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Column("created_at")]
        [Display(Name = "建立時間")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "無")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 角色關聯
        /// </summary>
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    /// <summary>
    /// 變更密碼
    /// </summary>
    public class ChangePasswordModel
    {
        /// <summary>
        /// 使用者帳號(工號)
        /// </summary>
        [Display(Name = "帳號(工號)")]
        public string UserName { get; set; }

        /// <summary>
        /// 使用者名稱(中文姓名)
        /// </summary>
        [Display(Name = "姓名")]
        public string FullName { get; set; }

        /// <summary>
        /// 原密碼
        /// </summary>
        [Required(ErrorMessage = "請輸入「原密碼」")]
        [DataType(DataType.Password)]
        [Display(Name = "原密碼")]
        public string CurrentPassword { get; set; }

        /// <summary>
        /// 新密碼
        /// </summary>
        [Required(ErrorMessage = "請輸入「新密碼」")]
        [MinLength(8, ErrorMessage = "新密碼長度至少需8個字元")]
        [DataType(DataType.Password)]
        [Display(Name = "新密碼")]
        public string NewPassword { get; set; }

        /// <summary>
        /// 確認新密碼
        /// </summary>
        [Required(ErrorMessage = "請輸入「確認新密碼」")]
        [Display(Name = "確認新密碼")]
        [Compare(nameof(NewPassword), ErrorMessage = "「新密碼」與「確認新密碼」不一致")]
        public string ConfirmPassword { get; set; } = null!;
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
    public class AccountModel : Pagination
    {
        /// <summary>
        /// 工號
        /// </summary>
        [Display(Name = "帳號(工號)")]
        public string UserName { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Display(Name = "姓名")]
        [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
        public string FullName { get; set; }

        /// <summary>
        /// 職稱
        /// </summary>
        [Display(Name = "職稱")]
        [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
        public string? JobTitle { get; set; }

        /// <summary>
        /// 部門名稱
        /// </summary>
        [Display(Name = "部門名稱")]
        [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
        public string? DepartmentName { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        [StringLength(255, ErrorMessage = "{0}最多{1}字元")]
        public string Email { get; set; }

        /// <summary>
        /// 聯絡電話
        /// </summary>
        [Display(Name = "聯絡電話")]
        [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
        public string? Phone { get; set; }

        /// <summary>
        /// 手機
        /// </summary>
        [Display(Name = "手機")]
        [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
        public string? Mobile { get; set; }

        /// <summary>
        /// 角色群組(List)
        /// </summary>
        [Display(Name = "系統角色")]
        public List<string> RoleName { get; set; }

        /// <summary>
        /// 角色清單文字
        /// </summary>
        [Display(Name = "系統角色")]
        public string RoleNameList { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "建立時間")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "無")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        [Column("is_active")]
        [Display(Name = "是否啟用")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// 是否鎖定
        /// </summary>
        [Column("is_locked")]
        [Display(Name = "是否鎖定")]
        public bool? IsLocked { get; set; }

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
    }

}
