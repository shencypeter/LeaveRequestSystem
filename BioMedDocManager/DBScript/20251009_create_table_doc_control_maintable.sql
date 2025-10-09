-- 建立文件管理資料表(範例資料表)
CREATE TABLE [dbo].[doc_control_maintable](
	[type] [nvarchar](50) NULL,
	[date_time] [date] NULL,
	[id] [nvarchar](50) NULL,
	[person_name] [nvarchar](50) NULL,
	[id_no] [nvarchar](50) NOT NULL,
	[name] [nvarchar](max) NULL,
	[purpose] [nvarchar](max) NULL,
	[original_doc_no] [nvarchar](max) NULL,
	[doc_ver] [nvarchar](10) NULL,
	[in_time] [date] NULL,
	[unuse_time] [date] NULL,
	[reject_reason] [nvarchar](max) NULL,
	[project_name] [nvarchar](50) NULL,
	[file_extension] [nvarchar](10) NULL,
	[is_confidential] [bit] NULL,
	[is_sensitive] [bit] NULL,
	[in_time_modify_by] [nvarchar](50) NULL,
	[in_time_modify_at] [datetime] NULL,
	[unuse_time_modify_by] [nvarchar](50) NULL,
	[unuse_time_modify_at] [datetime] NULL,
 CONSTRAINT [PK_doc_control_maintable] PRIMARY KEY CLUSTERED 
(
	[id_no] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'工號' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'doc_control_maintable', @level2type=N'COLUMN',@level2name=N'id'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'請購人員' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'doc_control_maintable', @level2type=N'COLUMN',@level2name=N'person_name'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'文件編號' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'doc_control_maintable', @level2type=N'COLUMN',@level2name=N'id_no'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'doc_control_maintable', @level2type=N'COLUMN',@level2name=N'name'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'目的' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'doc_control_maintable', @level2type=N'COLUMN',@level2name=N'purpose'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'BMP單號' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'doc_control_maintable', @level2type=N'COLUMN',@level2name=N'original_doc_no'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'版本' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'doc_control_maintable', @level2type=N'COLUMN',@level2name=N'doc_ver'
GO

EXEC sys.sp_addextendedproperty @name=N'說明', @value=N'文管請購單' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'doc_control_maintable'
GO



-- insert 初始資料
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-01-02' AS Date), N'534159', N'鍾葦蓉', N'B202001001', N'文件制修訂申請單---1', N'QP01改版v3.0---1', N'BMP-QP01-TR001', N'2.0', CAST(N'2020-01-02' AS Date), NULL, NULL, N'---1', N'docx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-01-02' AS Date), N'534159', N'鍾葦蓉', N'B202001002', N'產品配方與製程參數紀錄表', N'安定性試驗報告(35度加速T=1年)', N'BMP-QP08-TR020', N'2.4', CAST(N'2020-01-22' AS Date), NULL, NULL, N'CSD', N'docx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-01-02' AS Date), N'534159', N'鍾葦蓉', N'B202001003', N'文件更新表', N'2020/01~2020/03文件更新紀錄', N'BMP-QP01-TR013', N'3.0', CAST(N'2020-03-31' AS Date), NULL, NULL, N'', N'docx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-01-02' AS Date), N'534159', N'鍾葦蓉', N'B202001004', N'品質紀錄領用入庫紀錄表', N'2020/01~2020/06品質文件領用入庫紀錄', N'BMP-QP01-TR017', N'3.0', CAST(N'2020-06-30' AS Date), NULL, NULL, N'', N'docx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-01-02' AS Date), N'534159', N'鍾葦蓉', N'B202001005', N'不符合改善措施執行成果審核紀錄表', N'不符合改善措施執行成果審核紀錄', N'BMP-QP10-TR002', N'3.2', CAST(N'2020-01-02' AS Date), NULL, NULL, N'', N'docx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-06-30' AS Date), N'A50606', N'盧珈蓉', N'B202006018', N'管理審查會議表單', N'行銷室管理審查', N'BMP-QP03-TR007', N'2.4', CAST(N'2020-06-30' AS Date), NULL, NULL, N'', N'pptx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-06-01' AS Date), N'A80151', N'陳靖眉', N'B202006051', N'倉儲總覽表', N'6月倉儲總覽表', N'BMP-QP13-TR010', N'3.1', CAST(N'2020-06-30' AS Date), NULL, NULL, N'', N'xlsx', NULL, NULL, NULL, NULL, NULL, NULL);