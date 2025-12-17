CREATE TABLE [dbo].[LocalizationString] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LocalizationString PRIMARY KEY,    
    [Key] NVARCHAR(200) NOT NULL,-- 例如：Security.Password.MinLength.Label    
    [Culture] NVARCHAR(20) NOT NULL,-- 例如：zh-TW / en-US（建議用 Culture Name）   
    [Value] NVARCHAR(MAX) NOT NULL, -- 顯示文字    
    [Category] NVARCHAR(100) NULL,-- 分組/模組（可選）：例如 Security / Common / Menu    
    [Description] NVARCHAR(500) NULL,-- 備註（可選）：給管理者看的說明
    [IsActive] BIT NOT NULL CONSTRAINT DF_LocalizationString_IsActive DEFAULT (1),
    [CreatedAt] DATETIME2(0) NOT NULL CONSTRAINT DF_Parameter_CreatedAt DEFAULT (SYSDATETIME()),
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


INSERT INTO [dbo].[LocalizationString] ([Key],[Culture],[Value],[Category],[Description],[IsActive])
VALUES
(N'Parameter.SEC_PASSWORD_MIN_LENGTH.Label', N'zh-TW', N'密碼最少長度', N'Security', N'密碼政策設定頁欄位標籤', 1),
(N'Parameter.SEC_PASSWORD_MIN_LENGTH.Label', N'en-US', N'Minimum password length', N'Security', N'Password policy settings label', 1),

(N'Parameter.SEC_PASSWORD_EXPIRE_DAYS.Label', N'zh-TW', N'密碼過期天數', N'Security', N'', 1),
(N'Parameter.SEC_PASSWORD_EXPIRE_DAYS.Label', N'en-US', N'Password expiry (days)', N'Security', N'', 1),

(N'Parameter.SEC_2FA_TOTP_ENABLED.Label', N'zh-TW', N'啟用 2FA（TOTP）', N'Security', N'', 1),
(N'Parameter.SEC_2FA_TOTP_ENABLED.Label', N'en-US', N'Enable 2FA (TOTP)', N'Security', N'', 1);
