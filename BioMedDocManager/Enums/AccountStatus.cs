using Humanizer.Localisation;
using System.ComponentModel.DataAnnotations;

namespace BioMedDocManager.Enums
{
    /// <summary>
    /// 帳號狀態列舉
    /// </summary>
    public enum AccountStatus
    {
        // 多語系 Resource 資源檔 (EnumLabels.resx / EnumLabels.zh-TW.resx / EnumLabels.en-US.resx)
        // [Display(Name = "Active", ResourceType = typeof(Resources.EnumLabels))]
        Active = 1,

        // [Display(Name = "Inactive", ResourceType = typeof(Resources.EnumLabels))]
        Inactive = 2,

        // [Display(Name = "Locked", ResourceType = typeof(Resources.EnumLabels))]
        Locked = 3,

        // [Display(Name = "Pending", ResourceType = typeof(Resources.EnumLabels))]
        Pending = 4,

        // [Display(Name = "Suspended", ResourceType = typeof(Resources.EnumLabels))]
        Suspended = 5,

        // [Display(Name = "Archived", ResourceType = typeof(Resources.EnumLabels))]
        Archived = 6
    }


}

