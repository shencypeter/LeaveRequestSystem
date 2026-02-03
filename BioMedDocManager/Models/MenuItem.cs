using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models;

/// <summary>
/// 選單項目
/// </summary>
public class MenuItem : AuditableEntity
{
    [Key]
    [Display(Name = "MenuItem.MenuItemId")]
    public long MenuItemId { get; set; }

    [Display(Name = "MenuItem.MenuItemParentId")]
    public long? MenuItemParentId { get; set; }

    /// <summary>
    /// 選單標題（顯示用）：改用關聯 Resource 的 ResourceDisplayName
    /// </summary>
    [NotMapped]
    [Display(Name = "MenuItem.MenuItemTitle")]
    public string MenuItemTitle => Resource?.ResourceDisplayName ?? Loc?.T("Menu.UnnamedItem");
    
    [NotMapped]
    public string MenuItemTitleDisplay { get; set; } = "";

    [Display(Name = "MenuItem.MenuItemIcon")]
    [StringLength(100, ErrorMessage = "Validation.StringLength")]
    public string? MenuItemIcon { get; set; }

    [NotMapped]
    [Display(Name = "MenuItem.ResourceKey")]
    public string? ResourceKey => Resource?.ResourceKey;

    [Display(Name = "MenuItem.MenuItemDisplayOrder")]
    public int MenuItemDisplayOrder { get; set; } = 0;

    [Display(Name = "MenuItem.MenuItemIsActive")]
    public bool MenuItemIsActive { get; set; } = true;

    [NotMapped]
    [Display(Name = "MenuItem.MenuItemIsActive")]
    public string MenuItemIsActiveText =>
        MenuItemIsActive
            ? (Loc?.T("Common.Enabled") ?? "Enabled")
            : (Loc?.T("Common.Disabled") ?? "Disabled");

    [Display(Name = "MenuItem.ResourceId")]
    public long? ResourceId { get; set; }

    [ForeignKey(nameof(MenuItemParentId))]
    public MenuItem? Parent { get; set; }

    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();

    public Resource? Resource { get; set; }
}
