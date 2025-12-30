using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 動作
/// </summary>
public class AppAction : AuditableEntity
{
    /// <summary>
    /// 動作編號
    /// </summary>
    [Key]
    [Display(Name = "AppAction.AppActionId")]
    public int AppActionId { get; set; }

    /// <summary>
    /// 動作名稱
    /// </summary>
    [Display(Name = "AppAction.AppActionCode")]
    [StringLength(
        50,
        ErrorMessage = "Validation.StringLength"
    )]
    public string AppActionCode { get; set; } = null!;  // Index/Create/Edit/Delete/...

    /// <summary>
    /// 顯示名稱（改為多語系，不再存 DB 欄位）
    /// </summary>
    [NotMapped]
    [Display(Name = "AppAction.AppActionDisplayName")]
    public string AppActionDisplayName => Loc?.T($"AppAction.{AppActionCode}") ?? AppActionCode;

    /// <summary>
    /// 顯示順序
    /// </summary>
    [Display(Name = "AppAction.AppActionOrder")]
    public int AppActionOrder { get; set; }

    /// <summary>
    /// 使用者權限-關聯
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
