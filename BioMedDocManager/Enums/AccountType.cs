using Humanizer.Localisation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BioMedDocManager.Enums
{
    /// <summary>
    /// 帳戶類型
    /// </summary>
    public enum AccountType : int
    {
        [Description("未知")]
        Unknow = 0,

        [Description("後台管理者")]
        Admin = 1,

        [Description("前台會員")]
        Member = 2
    }

}

