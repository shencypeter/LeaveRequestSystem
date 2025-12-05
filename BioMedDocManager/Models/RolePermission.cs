using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 角色資源動作權限
/// </summary>
public class RolePermission/* : ISoftDelete*/
{
    /// <summary>
    /// 角色編號
    /// </summary>
    [Display(Name = "角色編號")]
    public int RoleId { get; set; }

    /// <summary>
    /// 資源編號
    /// </summary>
    [Display(Name = "資源編號")]
    public int ResourceId { get; set; }

    /// <summary>
    /// 動作編號
    /// </summary>
    [Display(Name = "動作編號")]
    public int AppActionId { get; set; }

    /*
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
    */
    /// <summary>
    /// 角色
    /// </summary>
    public Role? Role { get; set; }

    /// <summary>
    /// 資源
    /// </summary>
    public Resource? Resource { get; set; }

    /// <summary>
    /// 動作
    /// </summary>
    public AppAction? AppAction { get; set; }
}
