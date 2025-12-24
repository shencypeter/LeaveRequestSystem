CREATE TABLE [dbo].[LocalizationString] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LocalizationString PRIMARY KEY,    
    [Key] NVARCHAR(200) NOT NULL,-- 例如：Security.Password.MinLength.Label    
    [Culture] NVARCHAR(20) NOT NULL,-- 例如：zh-TW / en-US（建議用 Culture Name）   
    [Value] NVARCHAR(MAX) NOT NULL, -- 顯示文字    
    [Category] NVARCHAR(100) NULL,-- 分組/模組（可選）：例如 Security / Common / Menu    
    [Description] NVARCHAR(500) NULL,-- 備註（可選）：給管理者看的說明
    [IsActive] BIT NOT NULL CONSTRAINT DF_LocalizationString_IsActive DEFAULT (1),
    [CreatedAt] DATETIME2(0) NOT NULL DEFAULT (SYSDATETIME()),
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME2(0) NULL,
    [UpdatedBy] INT NULL,
    [DeletedAt] DATETIME2(0) NULL,
    [DeletedBy] INT NULL,
    CONSTRAINT UQ_LocalizationString_Key_Culture UNIQUE ([Key], [Culture])
);

-- 常用索引（查 Key/Culture 會很頻繁）
CREATE INDEX IX_LocalizationString_Culture_Key
ON [dbo].[LocalizationString] ([Culture], [Key])
INCLUDE ([IsActive]);


-- 新增資料
INSERT INTO [dbo].[LocalizationString] ([Key],[Culture],[Value],[Category],[IsActive])
VALUES
-- ===== 系統基礎 =====
(N'Common.SystemName', N'zh-TW', N'文管與電子採購系統', N'Common', 1),
(N'Common.SystemName', N'en-US', N'Document Control & E-Procurement System', N'Common', 1),

(N'Common.Welcome', N'zh-TW', N'歡迎使用', N'Common', 1),
(N'Common.Welcome', N'en-US', N'Welcome', N'Common', 1),

(N'Common.Loading', N'zh-TW', N'資料載入中…', N'Common', 1),
(N'Common.Loading', N'en-US', N'Loading…', N'Common', 1),

(N'Common.Required', N'zh-TW', N'必填', N'Common', 1),
(N'Common.Required', N'en-US', N'Required', N'Common', 1),

-- ===== 操作結果 =====
(N'Common.Success', N'zh-TW', N'操作成功', N'Common', 1),
(N'Common.Success', N'en-US', N'Operation succeeded', N'Common', 1),

(N'Common.Failed', N'zh-TW', N'操作失敗', N'Common', 1),
(N'Common.Failed', N'en-US', N'Operation failed', N'Common', 1),

(N'Common.NoData', N'zh-TW', N'查無資料', N'Common', 1),
(N'Common.NoData', N'en-US', N'No data found', N'Common', 1),

(N'Common.Confirm', N'zh-TW', N'請確認', N'Common', 1),
(N'Common.Confirm', N'en-US', N'Please confirm', N'Common', 1),

(N'Common.Cancelled', N'zh-TW', N'操作已取消', N'Common', 1),
(N'Common.Cancelled', N'en-US', N'Operation cancelled', N'Common', 1),

-- ===== CRUD / 表單 =====
(N'Common.Search', N'zh-TW', N'查詢', N'Common', 1),
(N'Common.Search', N'en-US', N'Search', N'Common', 1),

(N'Common.Create', N'zh-TW', N'新增', N'Common', 1),
(N'Common.Create', N'en-US', N'Create', N'Common', 1),

(N'Common.Edit', N'zh-TW', N'編輯', N'Common', 1),
(N'Common.Edit', N'en-US', N'Edit', N'Common', 1),

(N'Common.Delete', N'zh-TW', N'刪除', N'Common', 1),
(N'Common.Delete', N'en-US', N'Delete', N'Common', 1),

(N'Common.View', N'zh-TW', N'檢視', N'Common', 1),
(N'Common.View', N'en-US', N'View', N'Common', 1),

(N'Common.Save', N'zh-TW', N'儲存', N'Common', 1),
(N'Common.Save', N'en-US', N'Save', N'Common', 1),

(N'Common.Submit', N'zh-TW', N'送出', N'Common', 1),
(N'Common.Submit', N'en-US', N'Submit', N'Common', 1),

(N'Common.Reset', N'zh-TW', N'清除', N'Common', 1),
(N'Common.Reset', N'en-US', N'Reset', N'Common', 1),

(N'Common.Close', N'zh-TW', N'關閉', N'Common', 1),
(N'Common.Close', N'en-US', N'Close', N'Common', 1),

-- ===== 狀態 =====
(N'Common.Yes', N'zh-TW', N'是', N'Common', 1),
(N'Common.Yes', N'en-US', N'Yes', N'Common', 1),

(N'Common.No', N'zh-TW', N'否', N'Common', 1),
(N'Common.No', N'en-US', N'No', N'Common', 1),

(N'Common.Enabled', N'zh-TW', N'啟用', N'Common', 1),
(N'Common.Enabled', N'en-US', N'Enabled', N'Common', 1),

(N'Common.Disabled', N'zh-TW', N'停用', N'Common', 1),
(N'Common.Disabled', N'en-US', N'Disabled', N'Common', 1),

(N'Common.All', N'zh-TW', N'全部', N'Common', 1),
(N'Common.All', N'en-US', N'All', N'Common', 1),


-- ===== 各頁面文字 =====
(N'Home.Welcome.Title', N'zh-TW', N'歡迎使用', N'Home', 1),
(N'Home.Welcome.Title', N'en-US', N'Welcome', N'Home', 1),

(N'Home.Welcome.SubTitle', N'zh-TW', N'文管與電子採購系統（範例）', N'Home', 1),
(N'Home.Welcome.SubTitle', N'en-US', N'Document Control & E-Procurement System (Demo)', N'Home', 1),





(N'Parameter.SEC_PASSWORD_MIN_LENGTH.Label', N'zh-TW', N'密碼最少長度', N'Security', N'密碼政策設定頁欄位標籤', 1),
(N'Parameter.SEC_PASSWORD_MIN_LENGTH.Label', N'en-US', N'Minimum password length', N'Security', N'Password policy settings label', 1),

(N'Parameter.SEC_PASSWORD_EXPIRE_DAYS.Label', N'zh-TW', N'密碼過期天數', N'Security', N'', 1),
(N'Parameter.SEC_PASSWORD_EXPIRE_DAYS.Label', N'en-US', N'Password expiry (days)', N'Security', N'', 1),

(N'Parameter.SEC_2FA_TOTP_ENABLED.Label', N'zh-TW', N'啟用 2FA（TOTP）', N'Security', N'', 1),
(N'Parameter.SEC_2FA_TOTP_ENABLED.Label', N'en-US', N'Enable 2FA (TOTP)', N'Security', N'', 1);
