using Humanizer.Localisation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BioMedDocManager.Enums
{
    /// <summary>
    /// 存取記錄類型
    /// </summary>
    public enum AccessLogType : int
    {
        [Description("登入紀錄")]
        LoginLog = 1,

        [Description("操作紀錄")]
        ActionLog = 2,

        [Description("密碼紀錄")]
        PasswordLog = 3
    }


}

