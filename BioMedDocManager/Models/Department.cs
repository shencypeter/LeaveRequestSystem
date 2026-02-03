using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 部門
/// </summary>
public class Department : AuditableEntity
{
    /// <summary>
    /// 部門編號
    /// </summary>
    [Key]
    [Display(Name = "Department.DepartmentId")]
    public long DepartmentId { get; set; }

    /// <summary>
    /// 部門代碼
    /// </summary>
    [Display(Name = "Department.DepartmentCode")]
    [StringLength(
        50,
        ErrorMessage = "Validation.StringLength"
    )]
    public string DepartmentCode { get; set; } = null!;

    /// <summary>
    /// 部門名稱（改用多語系，不再存 DB 欄位）
    /// </summary>
    [NotMapped]
    [Display(Name = "Department.DepartmentName")]
    public string DepartmentName => Loc?.T($"Department.{DepartmentCode}") ?? DepartmentCode;

    /// <summary>
    /// 上層部門 Id（自我參照）
    /// </summary>
    [Display(Name = "Department.DepartmentParentId")]
    public long? DepartmentParentId { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "Department.DepartmentIsActive")]
    public bool DepartmentIsActive { get; set; } = true;

    /// <summary>
    /// 是否啟用 文字
    /// </summary>
    [NotMapped]
    [Display(Name = "Department.DepartmentIsActive")]
    public string DepartmentIsActiveText =>
        DepartmentIsActive
            ? (Loc?.T("Common.Enabled") ?? "Enabled")
            : (Loc?.T("Common.Disabled") ?? "Disabled");

    /* ===========================
       導覽屬性
       =========================== */

    /// <summary>
    /// 上層部門
    /// </summary>
    [ForeignKey(nameof(DepartmentParentId))]
    public Department? Parent { get; set; }

    /// <summary>
    /// 子部門集合
    /// </summary>
    public ICollection<Department> Children { get; set; } = new List<Department>();

    /// <summary>
    /// 此部門下的使用者（需有 User.DepartmentId 外鍵）
    /// </summary>
    public ICollection<User> Users { get; set; } = new List<User>();
}
