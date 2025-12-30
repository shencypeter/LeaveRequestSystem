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


CREATE TABLE [dbo].[issue_table](
	[name] [nvarchar](max) NULL,
	[issue_datetime] [date] NULL,
	[original_doc_no] [nvarchar](50) NOT NULL,
	[doc_ver] [nvarchar](10) NOT NULL,
	[file_extension] [nvarchar](10) NULL,
 CONSTRAINT [PK_issue_table] PRIMARY KEY CLUSTERED 
(
	[original_doc_no] ASC,
	[doc_ver] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

-- ===== 一般頁面相關範例資料 =====
-- insert 領用紀錄表
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-01-02' AS Date), N'534159', N'鍾葦蓉', N'B202001001', N'文件制修訂申請單---1', N'QP01改版v3.0---1', N'BMP-QP01-TR001', N'2.0', CAST(N'2020-01-02' AS Date), NULL, NULL, N'---1', N'docx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-01-02' AS Date), N'534159', N'鍾葦蓉', N'B202001002', N'產品配方與製程參數紀錄表', N'安定性試驗報告(35度加速T=1年)', N'BMP-QP08-TR020', N'2.4', CAST(N'2020-01-22' AS Date), NULL, NULL, N'CSD', N'docx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-01-02' AS Date), N'534159', N'鍾葦蓉', N'B202001003', N'文件更新表', N'2020/01~2020/03文件更新紀錄', N'BMP-QP01-TR013', N'3.0', CAST(N'2020-03-31' AS Date), NULL, NULL, N'', N'docx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-01-02' AS Date), N'534159', N'鍾葦蓉', N'B202001004', N'品質紀錄領用入庫紀錄表', N'2020/01~2020/06品質文件領用入庫紀錄', N'BMP-QP01-TR017', N'3.0', CAST(N'2020-06-30' AS Date), NULL, NULL, N'', N'docx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-01-02' AS Date), N'534159', N'鍾葦蓉', N'B202001005', N'不符合改善措施執行成果審核紀錄表', N'不符合改善措施執行成果審核紀錄', N'BMP-QP10-TR002', N'3.2', CAST(N'2020-01-02' AS Date), NULL, NULL, N'', N'docx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-06-30' AS Date), N'A50606', N'盧珈蓉', N'B202006018', N'管理審查會議表單', N'行銷室管理審查', N'BMP-QP03-TR007', N'2.4', CAST(N'2020-06-30' AS Date), NULL, NULL, N'', N'pptx', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT [dbo].[doc_control_maintable] ([type], [date_time], [id], [person_name], [id_no], [name], [purpose], [original_doc_no], [doc_ver], [in_time], [unuse_time], [reject_reason], [project_name], [file_extension], [is_confidential], [is_sensitive], [in_time_modify_by], [in_time_modify_at], [unuse_time_modify_by], [unuse_time_modify_at]) VALUES (N'B', CAST(N'2020-06-01' AS Date), N'A80151', N'陳靖眉', N'B202006051', N'倉儲總覽表', N'6月倉儲總覽表', N'BMP-QP13-TR010', N'3.1', CAST(N'2020-06-30' AS Date), NULL, NULL, N'', N'xlsx', NULL, NULL, NULL, NULL, NULL, NULL);

-- insert 發行表單
INSERT INTO [issue_table] ( [name], [issue_datetime], [original_doc_no], [doc_ver], [file_extension])
VALUES
(N'產品配方與製程參數紀錄表', '2019-08-30', 'BMP-QP08-TR020', '2.4', 'docx'),
(N'文件更新表', '2020-01-02', 'BMP-QP01-TR013', '3.0', 'docx'),
(N'品質紀錄領用入庫紀錄表', '2020-01-02', 'BMP-QP01-TR017', '3.0', 'docx'),
(N'不符合改善措施執行成果審核紀錄表', '2018-03-23', 'BMP-QP10-TR002', '3.2', 'docx'),
(N'管理審查會議表單', '2019-03-08', 'BMP-QP03-TR007', '2.4', 'pptx'),
(N'倉儲總覽表', '2018-12-27', 'BMP-QP13-TR010', '3.1', 'xlsx');







-- ==== 資源、權限相關範例資料 =====
-- insert 資源
INSERT INTO Resource (ResourceType, ResourceKey, ResourceDisplayName, ResourceIsActive, CreatedAt)
VALUES
('PAGE', 'CFileQuery', '文件查詢', 1, GETDATE()),
('PAGE', 'CIssueTables', '表單發行', 1, GETDATE()),
('PAGE', 'Tree', '表單查詢樹', 1, GETDATE()),
('PAGE', 'File', '檔案', 1, GETDATE());


-- insert 動作
INSERT INTO [dbo].[AppAction] ([AppActionCode],[AppActionDisplayName],[AppActionOrder]) VALUES
(N'SearchAll',  N'查詢全部', 120),
(N'GetTreeDataVerLatest',  N'顯示最新版本的查詢樹', 130),
(N'GetTreeDataVer',  N'顯示所有版本的查詢樹', 140),
(N'GetClaimFile',  N'下載檔案', 150),
(N'GetClaimFileByAdmin',  N'管理者下載檔案', 160),
(N'NewVersion',  N'發行新版', 170),
(N'History',  N'入庫歷程', 180);


-- 宣告變數，動態取得 ResourceId / AppActionId
-- ==== ResourceId ====
DECLARE 
    @Res_CFileQuery     INT,
    @Res_CIssueTables   INT,
    @Res_Tree           INT,
    @Res_File           INT;

SELECT @Res_CFileQuery   = ResourceId FROM Resource WHERE ResourceKey = 'CFileQuery';
SELECT @Res_CIssueTables = ResourceId FROM Resource WHERE ResourceKey = 'CIssueTables';
SELECT @Res_Tree         = ResourceId FROM Resource WHERE ResourceKey = 'Tree';
SELECT @Res_File         = ResourceId FROM Resource WHERE ResourceKey = 'File';

-- ==== AppActionId ====
DECLARE
    @Act_Index              INT,
    @Act_Details            INT,
    @Act_Create             INT,
    @Act_Edit               INT,
    @Act_Delete             INT,
    @Act_Export             INT,
    @Act_Import             INT,
    @Act_SearchAll          INT,
    @Act_GetTreeLatest      INT,
    @Act_GetTreeAll         INT,
    @Act_GetFile            INT,
    @Act_GetFileAdmin       INT,
    @Act_NewVersion         INT,
    @Act_History            INT;

SELECT @Act_Index   = AppActionId FROM AppAction WHERE AppActionCode = 'Index';
SELECT @Act_Details = AppActionId FROM AppAction WHERE AppActionCode = 'Details';
SELECT @Act_Create  = AppActionId FROM AppAction WHERE AppActionCode = 'Create';
SELECT @Act_Edit    = AppActionId FROM AppAction WHERE AppActionCode = 'Edit';
SELECT @Act_Delete  = AppActionId FROM AppAction WHERE AppActionCode = 'Delete';
SELECT @Act_Export  = AppActionId FROM AppAction WHERE AppActionCode = 'Export';
SELECT @Act_Import  = AppActionId FROM AppAction WHERE AppActionCode = 'Import';
SELECT @Act_SearchAll     = AppActionId FROM AppAction WHERE AppActionCode = 'SearchAll';
SELECT @Act_GetTreeLatest = AppActionId FROM AppAction WHERE AppActionCode = 'GetTreeDataVerLatest';
SELECT @Act_GetTreeAll    = AppActionId FROM AppAction WHERE AppActionCode = 'GetTreeDataVer';
SELECT @Act_GetFile       = AppActionId FROM AppAction WHERE AppActionCode = 'GetClaimFile';
SELECT @Act_GetFileAdmin  = AppActionId FROM AppAction WHERE AppActionCode = 'GetClaimFileByAdmin';
SELECT @Act_NewVersion    = AppActionId FROM AppAction WHERE AppActionCode = 'NewVersion';
SELECT @Act_History       = AppActionId FROM AppAction WHERE AppActionCode = 'History';



-- insert 角色權限 (系統管理者（RoleId = 1）：全資源全動作)

-- 文件查詢
INSERT INTO RolePermission (RoleId, ResourceId, AppActionId)
SELECT 1, @Res_CFileQuery, v.ActId
FROM (VALUES
    (@Act_Index), (@Act_Details), (@Act_Create),
    (@Act_Edit), (@Act_Delete), (@Act_Export), (@Act_Import)
) v(ActId)
WHERE NOT EXISTS (
    SELECT 1 FROM RolePermission rp
    WHERE rp.RoleId = 1
      AND rp.ResourceId = @Res_CFileQuery
      AND rp.AppActionId = v.ActId
);

-- 表單發行
INSERT INTO RolePermission (RoleId, ResourceId, AppActionId)
SELECT 1, @Res_CIssueTables, v.ActId
FROM (VALUES
    (@Act_Index), (@Act_Details), (@Act_Create),
    (@Act_Edit), (@Act_Delete), (@Act_Export), (@Act_Import),
    (@Act_NewVersion), (@Act_History)
) v(ActId)
WHERE NOT EXISTS (
    SELECT 1 FROM RolePermission rp
    WHERE rp.RoleId = 1
      AND rp.ResourceId = @Res_CIssueTables
      AND rp.AppActionId = v.ActId
);

-- 查詢樹
INSERT INTO RolePermission (RoleId, ResourceId, AppActionId)
SELECT 1, @Res_Tree, v.ActId
FROM (VALUES
    (@Act_Index),
    (@Act_SearchAll),
    (@Act_GetTreeLatest),
    (@Act_GetTreeAll)
) v(ActId)
WHERE NOT EXISTS (
    SELECT 1 FROM RolePermission rp
    WHERE rp.RoleId = 1
      AND rp.ResourceId = @Res_Tree
      AND rp.AppActionId = v.ActId
);

-- 檔案下載
INSERT INTO RolePermission (RoleId, ResourceId, AppActionId)
SELECT 1, @Res_File, v.ActId
FROM (VALUES
    (@Act_GetFile),
    (@Act_GetFileAdmin)
) v(ActId)
WHERE NOT EXISTS (
    SELECT 1 FROM RolePermission rp
    WHERE rp.RoleId = 1
      AND rp.ResourceId = @Res_File
      AND rp.AppActionId = v.ActId
);

/*
INSERT INTO [dbo].[RolePermission] ([RoleId],[ResourceId],[AppActionId])
VALUES
	-- [範例] 文件查詢
    (1, 9, 1),
    (1, 9, 2),
    (1, 9, 3),
    (1, 9, 4),
    (1, 9, 5),
    (1, 9, 6),
    (1, 9, 7),

	-- [範例] 表單發行
    (1, 10, 1),
    (1, 10, 2),
    (1, 10, 3),
    (1, 10, 4),
    (1, 10, 5),
    (1, 10, 6),
    (1, 10, 7),
    (1, 10, 17), -- 特例：發行新版
    (1, 10, 18), -- 特例：入庫歷程
	
	-- [範例] 查詢樹
    (1, 11, 1),
    (1, 11, 12),
    (1, 11, 13),
    (1, 11, 14),

    -- [範例] 檔案下載
    (1, 12, 15),
    (1, 12, 16);
*/


-- ==== MenuItem ====

DECLARE @Menu_DocRoot INT;

-- 父選單
IF NOT EXISTS (SELECT 1 FROM MenuItem WHERE MenuItemTitle = N'文件管理' AND MenuItemParentId IS NULL)
BEGIN
    INSERT INTO MenuItem
    (MenuItemParentId, MenuItemTitle, MenuItemIcon, MenuItemDisplayOrder, MenuItemIsActive, ResourceId, CreatedAt, CreatedBy)
    VALUES
    (NULL, N'文件管理', 'fa-solid fa-folder', 3, 1, NULL, GETDATE(), NULL);

    SET @Menu_DocRoot = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT @Menu_DocRoot = MenuItemId
    FROM MenuItem
    WHERE MenuItemTitle = N'文件管理' AND MenuItemParentId IS NULL;
END

-- 文件查詢
IF NOT EXISTS (SELECT 1 FROM MenuItem WHERE MenuItemTitle = N'文件查詢' AND MenuItemParentId = @Menu_DocRoot)
BEGIN
    INSERT INTO MenuItem
    (MenuItemParentId, MenuItemTitle, MenuItemIcon, MenuItemDisplayOrder, MenuItemIsActive, ResourceId, CreatedAt, CreatedBy)
    VALUES
    (@Menu_DocRoot, N'文件查詢', 'fa-solid fa-file', 1, 1, @Res_CFileQuery, GETDATE(), NULL);
END

-- 表單發行
IF NOT EXISTS (SELECT 1 FROM MenuItem WHERE MenuItemTitle = N'表單發行' AND MenuItemParentId = @Menu_DocRoot)
BEGIN
    INSERT INTO MenuItem
    (MenuItemParentId, MenuItemTitle, MenuItemIcon, MenuItemDisplayOrder, MenuItemIsActive, ResourceId, CreatedAt, CreatedBy)
    VALUES
    (@Menu_DocRoot, N'表單發行', 'fa-solid fa-book', 2, 1, @Res_CIssueTables, GETDATE(), NULL);
END
