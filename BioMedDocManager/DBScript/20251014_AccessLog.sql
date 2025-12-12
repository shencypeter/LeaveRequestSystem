SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[AccessLogs](
	[LogId] [uniqueidentifier] NOT NULL,
	[LogDateTime] [datetime] NOT NULL,
	[LogDateTimeUtc] [datetime] NOT NULL,
	[AccessLogType] [int] NOT NULL,
	[AccountType] [int] NULL,
	[AccountNum] [int] NULL,
	[AccountId] [int] NULL,
	[ClientIp] [nvarchar](255) NULL,
	[RequestMethod] [nvarchar](50) NULL,
	[RequestUrl] [nvarchar](max) NULL,
	[RequestReferrer] [nvarchar](max) NULL,
	[FunctionName] [nvarchar](255) NULL,
	[ActionName] [nvarchar](50) NULL,
	[IsSuccess] [bit] NULL,
	[Severity] [int] NULL,
	[Description] [nvarchar](max) NULL,
 CONSTRAINT [PK_AccessLogs] PRIMARY KEY CLUSTERED 
(
	[LogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[AccessLogs] ADD  CONSTRAINT [DF_AccessLogs_LogId]  DEFAULT (newid()) FOR [LogId]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'紀錄識別碼' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'LogId'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'紀錄時間' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'LogDateTime'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'紀錄時間(UTC)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'LogDateTimeUtc'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'存取記錄類型(參見程式Enums.AccessLogType)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'AccessLogType'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'帳戶類型(參見程式Enums.AccountType)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'AccountType'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'帳戶帳號(account)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'AccountNum'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'帳戶識別碼(id)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'AccountId'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用戶端的IP' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'ClientIp'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'請求的 Http 方法' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'RequestMethod'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'請求的網址' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'RequestUrl'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'請求是從哪個頁面連過來的' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'RequestReferrer'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'功能名稱' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'FunctionName'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'動作名稱' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'ActionName'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否成功' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'IsSuccess'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'嚴重程度(登入失敗時，區分要不要鎖定帳戶)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'Severity'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'描述' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLogs', @level2type=N'COLUMN',@level2name=N'Description'
GO


-- debug用：檢視群組各資源權限
CREATE VIEW AccessLogViewer
AS
SELECT 
    al.LogDateTime        AS 紀錄時間,
    CASE al.AccessLogType
        WHEN 1 THEN N'登入紀錄'
        WHEN 2 THEN N'操作紀錄'
        WHEN 3 THEN N'密碼紀錄'
        ELSE N'未知'
    END AS 紀錄類型,
    CASE al.AccountType
        WHEN 0 THEN N'未知'
        WHEN 1 THEN N'後台管理者'
        WHEN 2 THEN N'前台會員'
        ELSE N'未知'
    END AS 帳號類型,
    al.AccountNum         AS 使用者帳號,
    u.UserFullName        AS 使用者姓名,
    al.FunctionName       AS 功能,
    al.ActionName         AS 動作,
    al.RequestUrl         AS 請求網址,
    al.RequestMethod      AS 請求方法,
    al.RequestReferrer    AS 來源網址,
    al.ClientIp           AS 使用者IP,
    /*
    CASE al.IsSuccess
        WHEN 1 THEN N'是'
        ELSE N'否'
    END AS 是否成功,
    */
    al.Severity           AS 嚴重度,
    al.Description        AS 額外敘述
FROM [DocControl0].[dbo].[AccessLogs] al
LEFT JOIN [DocControl0].[dbo].[User] u
    ON al.AccountId = u.[UserId]
ORDER BY LogDateTime DESC
OFFSET 0 ROWS
GO