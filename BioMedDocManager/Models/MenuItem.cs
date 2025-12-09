using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 選單項目
/// </summary>
public class MenuItem : ISoftDelete
{
    /// <summary>
    /// 選單編號
    /// </summary>
    [Key]
    [Display(Name = "選單編號")]
    public int MenuItemId { get; set; }

    /// <summary>
    /// 上層選單編號
    /// </summary>
    [Display(Name = "上層選單")]
    public int? MenuItemParentId { get; set; }

    /// <summary>
    /// 選單標題
    /// </summary>
    [Display(Name = "選單標題")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string MenuItemTitle { get; set; } = null!;

    /// <summary>
    /// 圖示
    /// </summary>
    [Display(Name = "圖示")]
    [StringLength(100, ErrorMessage = "{0}最多{1}字元")]
    public string? MenuItemIcon { get; set; }

    /*  停用 => 改成用關連Resource.ResourceKey
    /// <summary>
    /// 連結
    /// </summary>
    [Display(Name = "連結")]
    [StringLength(300, ErrorMessage = "{0}最多{1}字元")]
    public string? MenuItemUrl { get; set; }
    */

    /// <summary>
    /// 資源代號（顯示用）
    /// </summary>
    [NotMapped]
    [Display(Name = "連結")]
    public string? ResourceKey
    {
        get
        {
            return Resource?.ResourceKey;
        }
    }

    /// <summary>
    /// 顯示順序
    /// </summary>
    [Display(Name = "顯示順序")]
    public int MenuItemDisplayOrder { get; set; } = 0;

    /// <summary>
    /// 是否啟用
    /// </summary>
    [Display(Name = "是否啟用")]
    public bool MenuItemIsActive { get; set; } = true;

    /// <summary>
    /// 是否啟用 文字
    /// </summary>
    [NotMapped]
    [Display(Name = "是否啟用")]
    public string MenuItemIsActiveText => MenuItemIsActive ? "啟用" : "停用";

    /// <summary>
    /// 資源編號(連結)
    /// </summary>
    [Display(Name = "連結")]
    public int? ResourceId { get; set; }

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

    /// <summary>
    /// 上層選單
    /// </summary>
    [ForeignKey(nameof(MenuItemParentId))]
    public MenuItem? Parent { get; set; }

    /// <summary>
    /// 子選單
    /// </summary>
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();

    /// <summary>
    /// 資源
    /// </summary>
    public Resource? Resource { get; set; }
}
