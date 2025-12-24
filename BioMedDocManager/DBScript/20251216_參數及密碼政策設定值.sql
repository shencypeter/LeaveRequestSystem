-- 參數
CREATE TABLE [dbo].[Parameter] (
    [Parameter_Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Parameter PRIMARY KEY, -- 主鍵
    [Parameter_Code] NVARCHAR(100) NOT NULL,      -- 參數代碼（程式使用）
    [Parameter_Name] NVARCHAR(200) NOT NULL,      -- 參數名稱（顯示用）
    [Parameter_Value] NVARCHAR(MAX) NULL,         -- 參數值（文字 / HTML / JSON）
    [Parameter_Format] NVARCHAR(20) NOT NULL,     -- 參數格式（text / int / html / json）
    [Parameter_IsActive] BIT NOT NULL CONSTRAINT DF_Parameter_IsActive DEFAULT (1), -- 是否啟用
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME()),
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2(0) NULL,
    [UpdatedBy] INT NULL,
    [DeletedAt] DATETIME2(0) NULL,
    [DeletedBy] INT NULL,
    CONSTRAINT UQ_Parameter_Code UNIQUE ([Parameter_Code])
);

INSERT INTO [dbo].[Parameter]
([Parameter_Code],[Parameter_Name],[Parameter_Value],[Parameter_Format],[Parameter_IsActive])
VALUES
-- ===== 密碼政策 =====
(N'SEC_PASSWORD_POLICY_ENABLED', N'啟用密碼政策', N'1', N'int', 1),
(N'SEC_PASSWORD_MIN_LENGTH', N'密碼最少長度', N'10', N'int', 1),
(N'SEC_PASSWORD_REQUIRE_UPPER', N'密碼需包含大寫英文', N'1', N'int', 1),
(N'SEC_PASSWORD_REQUIRE_LOWER', N'密碼需包含小寫英文', N'1', N'int', 1),
(N'SEC_PASSWORD_REQUIRE_DIGIT', N'密碼需包含數字', N'1', N'int', 1),
(N'SEC_PASSWORD_REQUIRE_SPECIAL', N'密碼需包含特殊符號', N'0', N'int', 1),
(N'SEC_PASSWORD_SPECIAL_CHAR_SETS', N'允許的特殊符號集合', N'@#$!%&*()_+-=.`', N'text', 1),

-- 密碼歷史與鎖定
(N'SEC_PASSWORD_HISTORY_COUNT', N'不可重複前 N 次密碼', N'5', N'int', 1),
(N'SEC_PASSWORD_MAX_FAILED_ATTEMPTS', N'登入失敗鎖定門檻', N'5', N'int', 1),
(N'SEC_PASSWORD_LOCKOUT_MINUTES', N'鎖定時間（分鐘）', N'15', N'int', 1),

-- ===== 過期 / 首登 =====
(N'SEC_PASSWORD_EXPIRE_DAYS', N'密碼過期天數（0=不過期）', N'90', N'int', 1),
(N'SEC_PASSWORD_FORCE_CHANGE_FIRST_LOGIN', N'首次登入強制改密碼', N'1', N'int', 1),

-- ===== 2FA =====
(N'SEC_2FA_ENABLED', N'啟用雙因素驗證（2FA）', N'0', N'int', 1),
(N'SEC_2FA_EMAIL_ENABLED', N'2FA-Email OTP', N'0', N'int', 1),
(N'SEC_2FA_TOTP_ENABLED', N'2FA-TOTP', N'0', N'int', 1),

-- ===== 其他雜項參數 =====
(N'SYS_EXCEPTION_LOG_PATH', N'Exception Log 的存放路徑', N'', N'text', 1),

(N'SYS_UPLOAD_ALLOWED_EXTENSIONS', N'允許上傳的副檔名清單', N'[".docx",".xlsx",".pptx"]', N'json', 1),

(N'SYS_WORD_TEMPLATES', N'Word 範本清單', 
 N'{
    "Purchase":{"TemplateFile":"請購單4.0_套版.docx","FileTitle":"請購單(V4.0)"},
    "Acceptance":{"TemplateFile":"收貨驗收單4.0_套版.docx","FileTitle":"收貨驗收單(V4.0)"},
    "FirstAssess":{"TemplateFile":"初次供應商評核表6.0_套版.docx","FileTitle":"初次供應商評核表(V6.0)"},
    "SupplierEval":{"TemplateFile":"供應商評核表6.0_套版.docx","FileTitle":"供應商評核表(V6.0)"},
    "DocumentManageList":{"TemplateFile":"品質紀錄領用入庫紀錄表4.0_套版.docx","FileTitle":"品質紀錄領用入庫紀錄表(V4.0)"}
 }',
 N'json', 1),

 -- ===== Email設定參數 =====
 (N'EMAIL_ACCOUNT', N'email帳號', N'3probeai@gmail.com', N'text', 1),
 (N'EMAIL_APP_PASSWORD', N'email應用程式密碼', N'enljxvouolirbkqa', N'text', 1),
 (N'EMAIL_SMTP_HOST', N'SMTP伺服器主機', N'mtp.gmail.com', N'text', 1),
 (N'EMAIL_SMTP_PORT', N'SMTP埠號', N'465', N'text', 1),

 (N'SITE_NAME', N'網站名稱', N'範例網站', N'text', 1);




-- === 補上資源與選單資料 ===
-- insert資源
INSERT INTO Resource (ResourceType, ResourceKey, ResourceDisplayName, ResourceIsActive, CreatedAt)
VALUES
('PAGE', 'Parameter', '系統參數管理', 1, GETDATE());

-- insert動作
INSERT INTO [dbo].[AppAction] ([AppActionName],[AppActionDisplayName],[AppActionOrder]) VALUES
(N'RegisterTotp',  N'註冊TOTP', 190),
(N'TotpQrCode',  N'顯示TOTP的QRcode', 200);

-- insert選單
-- 系統選單
DECLARE @Res_Parameter INT;
DECLARE @Res_AccountSettings INT;
SELECT @Res_Parameter = ResourceId FROM Resource WHERE ResourceKey = 'Parameter';
SELECT @Res_AccountSettings = ResourceId FROM Resource WHERE ResourceKey = 'AccountSettings';

INSERT INTO MenuItem (MenuItemParentId, MenuItemTitle, MenuItemIcon, MenuItemDisplayOrder, MenuItemIsActive, ResourceId, CreatedAt, CreatedBy)
VALUES
(1, '系統參數管理', 'fa-solid fa-sliders', 4, 1, @Res_Parameter, GETDATE(), 1);


-- ==== AppActionId ====
DECLARE
    @Act_Index              INT,
    @Act_Details            INT,
    @Act_Create             INT,
    @Act_Edit               INT,
    @Act_Delete             INT,
    @Act_Export             INT,
    @Act_Import             INT,
    @Act_RegisterTotp       INT,
    @Act_TotpQrCode         INT;

SELECT @Act_Index   = AppActionId FROM AppAction WHERE AppActionName = 'Index';
SELECT @Act_Details = AppActionId FROM AppAction WHERE AppActionName = 'Details';
SELECT @Act_Create  = AppActionId FROM AppAction WHERE AppActionName = 'Create';
SELECT @Act_Edit    = AppActionId FROM AppAction WHERE AppActionName = 'Edit';
SELECT @Act_Delete  = AppActionId FROM AppAction WHERE AppActionName = 'Delete';
SELECT @Act_Export  = AppActionId FROM AppAction WHERE AppActionName = 'Export';
SELECT @Act_Import  = AppActionId FROM AppAction WHERE AppActionName = 'Import';
SELECT @Act_RegisterTotp  = AppActionId FROM AppAction WHERE AppActionName = 'RegisterTotp';
SELECT @Act_TotpQrCode  = AppActionId FROM AppAction WHERE AppActionName = 'TotpQrCode';

-- insert 角色權限 (系統管理者（RoleId = 1）：全資源全動作)

-- 系統參數管理
INSERT INTO RolePermission (RoleId, ResourceId, AppActionId)
SELECT 1, @Res_Parameter, v.ActId
FROM (VALUES
    (@Act_Index), (@Act_Details), (@Act_Create),
    (@Act_Edit), (@Act_Delete), (@Act_Export), (@Act_Import)
) v(ActId)
WHERE NOT EXISTS (
    SELECT 1 FROM RolePermission rp
    WHERE rp.RoleId = 1
      AND rp.ResourceId = @Res_Parameter
      AND rp.AppActionId = v.ActId
);

--- 帳號設定頁面 (讓使用者能設定2FA)
INSERT INTO RolePermission (RoleId, ResourceId, AppActionId)
SELECT 1, @Res_AccountSettings, v.ActId
FROM (VALUES
    (@Act_RegisterTotp), (@Act_TotpQrCode)
) v(ActId)
WHERE NOT EXISTS (
    SELECT 1 FROM RolePermission rp
    WHERE rp.RoleId = 1
      AND rp.ResourceId = @Res_AccountSettings
      AND rp.AppActionId = v.ActId
);