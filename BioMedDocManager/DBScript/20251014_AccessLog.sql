SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[AccessLog](
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
 CONSTRAINT [PK_AccessLog] PRIMARY KEY CLUSTERED 
(
	[LogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[AccessLog] ADD  CONSTRAINT [DF_AccessLog_LogId]  DEFAULT (newid()) FOR [LogId]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'紀錄識別碼' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'LogId'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'紀錄時間' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'LogDateTime'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'紀錄時間(UTC)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'LogDateTimeUtc'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'存取記錄類型(參見程式Enums.AccessLogType)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'AccessLogType'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'帳戶類型(參見程式Enums.AccountType)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'AccountType'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'帳戶帳號(account)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'AccountNum'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'帳戶識別碼(id)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'AccountId'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用戶端的IP' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'ClientIp'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'請求的 Http 方法' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'RequestMethod'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'請求的網址' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'RequestUrl'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'請求是從哪個頁面連過來的' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'RequestReferrer'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'功能名稱' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'FunctionName'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'動作名稱' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'ActionName'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否成功' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'IsSuccess'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'嚴重程度(登入失敗時，區分要不要鎖定帳戶)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'Severity'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'描述' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AccessLog', @level2type=N'COLUMN',@level2name=N'Description'
GO
