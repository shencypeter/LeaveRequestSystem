using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 部門
/// </summary>
public class Department : ISoftDelete
{
    /// <summary>
    /// 部門編號
    /// </summary>
    [Key]
    [Display(Name = "部門編號")]
    public int DepartmentId { get; set; }

    /// <summary>
    /// 部門代碼
    /// </summary>
    [Display(Name = "部門代碼")]
    [StringLength(50, ErrorMessage = "{0}最多{1}字元")]
    public string DepartmentCode { get; set; } = null!;

    /// <summary>
    /// 部門名稱
    /// </summary>
    [Display(Name = "部門名稱")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string DepartmentName { get; set; } = null!;

    /// <summary>
    /// 上層部門 Id（自我參照）
    /// </summary>
    [Display(Name = "上層部門")]
    public int? DepartmentParentId { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "是否啟用")]
    public bool DepartmentIsActive { get; set; } = true;

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
