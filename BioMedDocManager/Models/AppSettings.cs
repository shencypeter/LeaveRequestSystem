namespace BioMedDocManager.Models;

/// <summary>
/// 系統設定值
/// </summary>
public static class AppSettings
{
    private static IConfiguration _cfg = default!;

    /// <summary>
    /// 2FA Session 狀態物件
    /// </summary>
    public static void Initialize(IConfiguration configuration) => _cfg = configuration;

    /// <summary>
    /// 2FA Session 狀態物件
    /// </summary>
    public const string TwoFactorSessionKey = "TwoFactorState";

    /// <summary>
    /// 變更密碼狀態物件-強制改密碼
    /// </summary>
    public const string ForceChangePasswordRequiredKey = "ForceChangePasswordRequired";

    /// <summary>
    /// 變更密碼狀態物件-強制首次登入改密碼
    /// </summary>
    public const string ForceChangePasswordReasonFirstKey = "ForceChangePasswordReasonFirstLogin";

    /// <summary>
    /// 變更密碼狀態物件-密碼過期(過久未變更)
    /// </summary>
    public const string ForceChangePasswordReasonExpireKey = "ForceChangePasswordReasonExpire";

    /// <summary>
    /// Exception Log 的存放路徑(抓appsettings.json)
    /// </summary>
    private static readonly Lazy<string> _exceptionLogPath =
                new Lazy<string>(() => GetSettingString("ExceptionLogPath", "", false), isThreadSafe: true);

    /// <summary>
    /// Exception Log 的存放路徑
    /// </summary>
    public static string ExceptionLogPath => _exceptionLogPath.Value;

    /// <summary>
    /// 登入失敗次數限制(抓appsettings.json)
    /// </summary>
    private static readonly Lazy<int> _loginFailedLimit =
                new Lazy<int>(() => GetSettingInt("LoginFailedLimit", 3, false), isThreadSafe: true);

    /// <summary>
    /// 登入失敗次數限制
    /// </summary>
    public static int LoginFailedLimit => _loginFailedLimit.Value;

    /// <summary>
    /// 登入鎖定時間(分鐘)(抓appsettings.json)
    /// </summary>
    private static readonly Lazy<int> _loginLockTime =
                new Lazy<int>(() => GetSettingInt("LoginLockTime", 15, false), isThreadSafe: true);

    /// <summary>
    /// 登入鎖定時間(分鐘)
    /// </summary>
    public static int LoginLockTime => _loginLockTime.Value;

    /// <summary>
    /// 合法的上傳檔案屬性
    /// </summary>
    public static readonly string[] AllowedExtensions = [".docx", ".xlsx", ".pptx"];

    /// <summary>
    /// Word範本檔案清單
    /// </summary>
    public static readonly Dictionary<string, (string TemplateFile, string FileTitle)> WordTemplates =
    new Dictionary<string, (string, string)>
    {
            { "Purchase", ("請購單4.0_套版.docx", "請購單(V4.0)") },
            { "Acceptance", ("收貨驗收單4.0_套版.docx", "收貨驗收單(V4.0)") },
            { "FirstAssess", ("初次供應商評核表6.0_套版.docx", "初次供應商評核表(V6.0)") },
            { "SupplierEval", ("供應商評核表6.0_套版.docx", "供應商評核表(V6.0)") },
            { "DocumentManageList", ("品質紀錄領用入庫紀錄表4.0_套版.docx", "品質紀錄領用入庫紀錄表(V4.0)") }
    };

    /// <summary>
    /// 讀取設定字串；支援巢狀 key（用冒號），例如 "AppSettings:ExceptionLogPath"
    /// </summary>
    public static string GetSettingString(string key, string defaultValue = "", bool allowNull = false)
    {
        var s = _cfg[key]; // 等同 Configuration["AppSettings:ExceptionLogPath"]
        if (!allowNull && s is null)
            throw new Exception($"{key} 的設定不存在！");
        return string.IsNullOrWhiteSpace(s) ? defaultValue : s!;
    }


    /// <summary>
    /// 讀取設定數值；支援巢狀 key（用冒號），例如 "AppSettings:ExceptionLogPath"
    /// </summary>
    public static int GetSettingInt(string key, int defaultValue = 0, bool allowNull = false)
    {
        var s = _cfg[key]; // 等同 Configuration["AppSettings:ExceptionLogPath"]

        if (!allowNull && s is null)
            throw new Exception($"{key} 的設定不存在！");

        if (int.TryParse(s, out int result))
        {
            return result;
        }

        return defaultValue;
    }


}
