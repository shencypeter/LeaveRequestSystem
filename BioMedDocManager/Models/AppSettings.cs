namespace BioMedDocManager.Models;

/// <summary>
/// 系統設定值
/// </summary>
public static class AppSettings
{
    private static IConfiguration _cfg = default!;

    // 在 Program.cs 啟動時呼叫一次
    public static void Initialize(IConfiguration configuration) => _cfg = configuration;

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

    public static class AdminRoleStrings
    {
        public const string 系統管理者 = "系統管理者";
    }

    public static class DocRoleStrings
    {
        public const string 領用人 = "領用人";
        public const string 負責人 = "負責人";

        public static readonly string[] Anyone = { 領用人, 負責人 };
    }

    public static class PurchaseRoleStrings
    {
        // 定義：本廠內任何需求人員皆可進行請購作業。
        public const string 請購人 = "請購人";

        // 定義：本廠內行政部人員皆可進行採購作業。
        public const string 採購人 = "採購人";

        // 定義：需具備新版醫療器材品質管理系統(QMS)或ISO 13485品質系統相關訓練證明，並依「人力資源管理作業程序書(BMP-QP04)」程序進行專業人員任命作業。
        public const string 評核人 = "評核人";

        /// <summary>
        /// 僅限特殊畫面使用
        /// </summary>
        public static readonly string[] Anyone = { 請購人, 採購人, 評核人 };
    }
    public static class CombinedRoles
    {
        // 這些來源必須都是 const 才能在這裡拼接
        public const string DocAndPurchase =
            DocRoleStrings.領用人 + "," +
            DocRoleStrings.負責人 + "," +
            PurchaseRoleStrings.請購人 + "," +
            PurchaseRoleStrings.採購人 + "," +
            PurchaseRoleStrings.評核人;
    }


    /// <summary>
    /// 小工具：給 Authorize(Roles="a,b,c")
    /// </summary>
    public static string ToAuthorizeRoles(this IEnumerable<string> roles)
        => string.Join(",", roles);

    /// <summary>
    /// 系統初始選單
    /// </summary>
    public static readonly PageLink[] SystemPages =
    [
        new PageLink { Controller = "Purchase", Label = "電子採購" , Roles = [PurchaseRoleStrings.Anyone.ToAuthorizeRoles()] },
        new PageLink { Controller = "Control",  Label = "文件管理" , Roles = [DocRoleStrings.Anyone.ToAuthorizeRoles()] },
    ];

    /// <summary>
    /// 帳號管理頁面選單
    /// </summary>
    public static readonly PageLink[] AccountPages =
    [
        new PageLink { Controller = "AccountSettings", Label = "帳號設定", Roles = [AdminRoleStrings.系統管理者] }
    ];

    /// <summary>
    /// 文管系統-各頁面選單
    /// </summary>
    public static readonly PageLink[] DocControlPages =
    [

        // new PageLink { Controller = "CDocumentClaim", Label = "文件領用", Roles = [DocRoleStrings.領用人] },
        new PageLink { Controller = "CFileQuery", Label = "文件查詢", Roles = [DocRoleStrings.領用人] },
            /*
            new PageLink { Controller = "CDocumentCancel", Label = "文件註銷", Roles = [DocRoleStrings.領用人] },
            new PageLink { Controller = "COldDocCtrlMaintables", Label = "2020年前表單查詢", Roles = [DocRoleStrings.領用人] },
            new PageLink { Controller = "CFormQuery", Label = "表單查詢", Roles = [DocRoleStrings.領用人] },
            new PageLink { Controller = "CDocumentClaimReserve", Label = "保留號文件領用", Roles = [DocRoleStrings.負責人] },
            new PageLink { Controller = "CIssueTables", Label = "表單發行", Roles = [DocRoleStrings.負責人] },
            new PageLink { Controller = "CDocumentManage", Label = "文件管制", Roles = [DocRoleStrings.負責人] },
            new PageLink { Controller = "CBatchStorage", Label = "批量入庫", Roles = [DocRoleStrings.負責人] },
            new PageLink { Controller = "CManagementSettings", Label = "管理設定", Roles = [DocRoleStrings.負責人] }
            */
        ];

    /// <summary>
    /// 電子採購系統-各頁面選單
    /// </summary>
    public static readonly PageLink[] PurchasingPages =
    [
    /*
    new PageLink { Controller = "PSupplier1stAssess", Label = "初供評核", Roles = [PurchaseRoleStrings.評核人] },// 任何人請購人都可初供評核
    new PageLink { Controller = "PProductClass", Label = "品項選單維護",  Roles = [PurchaseRoleStrings.評核人]},// 限評核人才能維護品項選單
    new PageLink { Controller = "PPurchaseTables", Label = "請購", Roles = [PurchaseRoleStrings.Anyone] },// 任何人都可請購
    new PageLink { Controller = "PAcceptance", Label = "驗收", Roles = [PurchaseRoleStrings.Anyone] },// 任何人都可驗收
    new PageLink { Controller = "PAssessment", Label = "評核與其他紀錄", Roles = [PurchaseRoleStrings.評核人] },// 限評核人才能評核
    new PageLink { Controller = "PAssessmentResult", Label = "評核結果查詢", Roles = [PurchaseRoleStrings.Anyone] },// 任何人都可看評核結果
    new PageLink { Controller = "PPurchaseRecords", Label = "請購分析",  Roles = [PurchaseRoleStrings.Anyone]},// 任何人都可看請購分析
    new PageLink { Controller = "PQualifiedSuppliers", Label = "供應商清冊", Roles = [PurchaseRoleStrings.Anyone] },// 任何人都可查看供應商清冊、新增供應商
    new PageLink { Controller = "PSupplierReassessments", Label = "再評估",  Roles = [PurchaseRoleStrings.評核人] },// 限評核人才能再評估
    */
    ];

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
