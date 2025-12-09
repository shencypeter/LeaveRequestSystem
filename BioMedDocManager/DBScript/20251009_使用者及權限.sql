-- 建立Department表，儲存部門基本資料(1)
-- 建立Role、User、UserGroup表，儲存角色(1)、使用者(使用者N--1部門)與使用者群組(1)基本資料
-- 建立UserGroupMember、UserGroupRole，儲存使用者群組成員(使用者群組成員N--N使用者)與使用者群組角色(使用者群組角色N--N角色)
-- 建立Resource、AppAction，儲存資源(1)、動作(1)
-- 建立RolePermission、MenuItem，儲存角色權限(角色N--N資源N--N動作)與選單(選單1--1資源)
-- 建立UserRole，儲存使用者角色對應表(使用者角色N--N使用者)，通常應使用「使用者群組」方式來管理使用者角色，除非特殊情況需要直接對應角色，否則不建議使用此表

-- 部門表
/*
DepartmentCode，DepartmentName
admin，行政部
it，資訊部
*/
CREATE TABLE [dbo].[Department] (
    [DepartmentId]         INT IDENTITY(1,1) NOT NULL,
    [DepartmentCode]       NVARCHAR(50)  NOT NULL,
    [DepartmentName]       NVARCHAR(100) NOT NULL,
    [DepartmentParentId]  INT NULL,  -- 自我參照
    [DepartmentIsActive]  BIT NOT NULL DEFAULT 1,
    [CreatedAt]            DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]            INT NULL,
    [UpdatedAt]            DATETIME NULL,
    [UpdatedBy]            INT NULL,
    [DeletedAt]            DATETIME NULL,
    [DeletedBy]            INT NULL,
    CONSTRAINT [PKDepartment] PRIMARY KEY CLUSTERED ([DepartmentId] ASC),
    CONSTRAINT [FKDepartmentParent]
        FOREIGN KEY ([DepartmentParentId])
        REFERENCES [dbo].[Department]([DepartmentId])
        -- 自我參照不能做級聯
        ON DELETE NO Action
        ON UPDATE NO Action
);
GO

-- 角色表
/*
RoleName，RoleGroup
請購人，採購
採購人，採購
評核人，採購
領用人，文管
負責人，文管
系統管理者，系統
*/
CREATE TABLE [dbo].[Role] (
    [RoleId]       INT IDENTITY(1,1) NOT NULL,
    [RoleName]     NVARCHAR(100) NOT NULL,
    [RoleGroup]    NVARCHAR(100) NOT NULL,
    [CreatedAt]    DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]    INT NULL,
    [UpdatedAt]    DATETIME NULL,
    [UpdatedBy]    INT NULL,
    [DeletedAt]    DATETIME NULL,
    [DeletedBy]    INT NULL,
    CONSTRAINT [PKRole] PRIMARY KEY CLUSTERED ([RoleId] ASC)
);
GO

-- 使用者表
/*
UserAccount，UserPasswordHash，UserFullName
534159，Abcd534159，範例使用者1
*/
CREATE TABLE [dbo].[User] (
    [UserId]                   INT IDENTITY(1,1) NOT NULL,
    [UserAccount]             NVARCHAR(100) NOT NULL,
    [UserPasswordHash]        NVARCHAR(255) NOT NULL,
    [UserFullName]            NVARCHAR(100) NOT NULL,
    [UserJobTitle]            NVARCHAR(100) NULL,    
    [UserEmail]                NVARCHAR(255) NOT NULL,
    [UserPhone]                NVARCHAR(50) NULL,
    [UserMobile]               NVARCHAR(50) NULL,
    [UserIsActive]            BIT NOT NULL DEFAULT 1,
    [UserIsLocked]            BIT NOT NULL DEFAULT 0,
    [UserLockedUntil]         DATETIME NULL,
    [UserLoginFailedCount]   INT NOT NULL DEFAULT 0,
    [UserLastLoginAt]        DATETIME NULL,
    [UserLastLoginIp]        NVARCHAR(50) NULL,
    [UserPasswordChangedAt]  DATETIME NULL,
    [UserStatus]               INT NULL,
    [UserRemarks]              NVARCHAR(255) NULL,
    [DepartmentId]             INT NULL,  -- 被[部門表]參照，與PK名稱一致
    [CreatedAt]                DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]                INT NULL,
    [UpdatedAt]                DATETIME NULL,
    [UpdatedBy]                INT NULL,
    [DeletedAt]                DATETIME NULL,
    [DeletedBy]                INT NULL,
    CONSTRAINT [PKUser] PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FKUserDepartment]
        FOREIGN KEY ([DepartmentId])
        REFERENCES [dbo].[Department]([DepartmentId])
        ON DELETE SET NULL
        ON UPDATE CASCADE
);
GO

-- 使用者群組
/*
UserGroupName，UserGroupDescription
研發部門，研發部門所有成員
A專案小組，負責A專案的成員
行政部B組，行政部B組所有成員
*/
CREATE TABLE [dbo].[UserGroup] (
    [UserGroupId]          INT IDENTITY(1,1) NOT NULL,
    [UserGroupName]        NVARCHAR(100) NOT NULL,
    [UserGroupDescription] NVARCHAR(255) NULL,
    [CreatedAt]             DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]             INT NULL,
    [UpdatedAt]             DATETIME NULL,
    [UpdatedBy]             INT NULL,
    [DeletedAt]             DATETIME NULL,
    [DeletedBy]             INT NULL,
    CONSTRAINT [PKUserGroup] PRIMARY KEY CLUSTERED ([UserGroupId] ASC)
);
GO

-- 使用者群組成員
/*
UserGroupId，UserId
1，1   研發部門有使用者1
1，2   研發部門有使用者2
2，3   A專案小組有使用者3
3，2   行政部B組有使用者2
*/
CREATE TABLE [dbo].[UserGroupMember] (
    [UserGroupId] INT NOT NULL,  -- 被[使用者群組表]參照，與PK名稱一致
    [UserId]       INT NOT NULL,  -- 被[使用者表]參照，與PK名稱一致
    CONSTRAINT [PKUserGroupMember] PRIMARY KEY CLUSTERED ([UserGroupId] ASC, [UserId] ASC),
    CONSTRAINT [FKugmGroup]
        FOREIGN KEY ([UserGroupId])
        REFERENCES [dbo].[UserGroup]([UserGroupId])
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT [FKugmUser]
        FOREIGN KEY ([UserId])
        REFERENCES [dbo].[User]([UserId])
        ON DELETE CASCADE
        ON UPDATE CASCADE
);
GO

-- 使用者群組角色（群組帶來的角色）
/*
UserGroupId，RoleId
1，1  研發部門有請購人角色
1，2  研發部門有採購人角色
2，3  A專案小組有評核人角色
3，5  行政部B組有負責人角色
*/
CREATE TABLE [dbo].[UserGroupRole] (
    [UserGroupId] INT NOT NULL,  -- 被[使用者群組表]參照，與PK名稱一致
    [RoleId]       INT NOT NULL,  -- 被[角色表]參照，與PK名稱一致
    CONSTRAINT [PKUserGroupRole] PRIMARY KEY ([UserGroupId], [RoleId]),
    CONSTRAINT [FKugrGroup]
        FOREIGN KEY ([UserGroupId])
        REFERENCES [dbo].[UserGroup]([UserGroupId])
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT [FKugrRole]
        FOREIGN KEY ([RoleId])
        REFERENCES [dbo].[Role]([RoleId])
        ON DELETE CASCADE
        ON UPDATE CASCADE
);
GO

-- 資源表
/*
Resourcetype，Resourcekey，ResourceDisplayName
PAGE，Control，文件管理
PAGE，CFileQuery，文件查詢
PAGE，CIssueTables，表單發行
*/
CREATE TABLE [dbo].[Resource] (
    [ResourceId]           INT IDENTITY(1,1) NOT NULL,
    [ResourceType]         NVARCHAR(50)  NOT NULL, -- PAGE/API
    [ResourceKey]          NVARCHAR(200) NOT NULL, -- 唯一識別，例如：Controller、/API/V1/Controller等
    [ResourceDisplayName] NVARCHAR(200) NOT NULL, -- 顯示名稱
    [ResourceIsActive]    BIT NOT NULL DEFAULT 1,
    [CreatedAt]            DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]            INT NULL,
    [UpdatedAt]            DATETIME NULL,
    [UpdatedBy]            INT NULL,
    [DeletedAt]            DATETIME NULL,
    [DeletedBy]            INT NULL,
    CONSTRAINT [PKResource] PRIMARY KEY CLUSTERED ([ResourceId] ASC)
);
GO

-- 動作表
/*
AppActionName，AppActionDisplayName
index，首頁/列表
create，新增
edit，編輯
delete，刪除
details，檢視明細
export，匯出
import，匯入
*/
CREATE TABLE [dbo].[AppAction] (
    [AppActionId]             INT IDENTITY(1,1) NOT NULL,
    [AppActionName]           NVARCHAR(50)  NOT NULL, -- 'index','create','edit','delete','details','export','import',...    
    [AppActionDisplayName]   NVARCHAR(100) NOT NULL,
    [AppActionOrder]     INT NOT NULL DEFAULT 0,
    [CreatedAt]            DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]            INT NULL,
    [UpdatedAt]            DATETIME NULL,
    [UpdatedBy]            INT NULL,
    [DeletedAt]            DATETIME NULL,
    [DeletedBy]            INT NULL,
    CONSTRAINT [PKAppAction] PRIMARY KEY CLUSTERED ([AppActionId] ASC)
);
GO

-- 角色對資源動作的允許（有列入的表示「允許」）
/*
RoleId，ResourceId，AppActionId
1，2，1  請購人對文件查詢有列表權限
2，3，1  採購人對表單發行有列表權限
*/
CREATE TABLE [dbo].[RolePermission] (
    [RoleId]       INT NOT NULL,      -- 被[角色表]參照，與PK名稱一致
    [ResourceId]   INT NOT NULL,      -- 被[資源表]參照，與PK名稱一致
    [AppActionId]     INT NOT NULL,      -- 被[動作表]參照，與PK名稱一致
    CONSTRAINT [PKRolePermission]
        PRIMARY KEY ([RoleId], [ResourceId], [AppActionId]),
    CONSTRAINT [FKrpRole]
        FOREIGN KEY ([RoleId])
        REFERENCES [dbo].[Role]([RoleId])
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT [FKrpResource]
        FOREIGN KEY ([ResourceId])
        REFERENCES [dbo].[Resource]([ResourceId])
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT [FKrpAppAction]
        FOREIGN KEY ([AppActionId])
        REFERENCES [dbo].[AppAction]([AppActionId])
        ON DELETE CASCADE
        ON UPDATE CASCADE
);
GO

CREATE INDEX [IXRolePermissionbyResource]
ON [dbo].[RolePermission]([ResourceId], [AppActionId], [RoleId]);
GO

-- 選單表（對應 Resource，可做角色導覽控制）
/*
MenuItemId，ParentId，MenuItemTitle，MenuItemIcon，ResourceId，MenuItemDisplayOrder，MenuItemIsActive
1，null，文件管理，fa-folder，1，1，1
2，1，文件查詢，fa-file，1，1，1
3，1，表單發行，fa-file-alt，2，2，1
*/
CREATE TABLE [dbo].[MenuItem] (
    [MenuItemId]            INT IDENTITY(1,1) NOT NULL,
    [MenuItemParentId]     INT NULL,       -- 自我參照
    [MenuItemTitle]         NVARCHAR(100) NOT NULL,
    [MenuItemIcon]          NVARCHAR(100) NULL,
    [MenuItemDisplayOrder] INT NOT NULL DEFAULT 0,
    [MenuItemIsActive]     BIT NOT NULL DEFAULT 1,
    [ResourceId]             INT NULL,       -- 被[資源表]參照，與PK名稱一致
    [CreatedAt]            DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]            INT NULL,
    [UpdatedAt]            DATETIME NULL,
    [UpdatedBy]            INT NULL,
    [DeletedAt]            DATETIME NULL,
    [DeletedBy]            INT NULL,
    CONSTRAINT [PKMenuItem] PRIMARY KEY CLUSTERED ([MenuItemId] ASC),
    CONSTRAINT [FKMenuItemParent]
        FOREIGN KEY ([MenuItemParentId])
        REFERENCES [dbo].[MenuItem]([MenuItemId])
        -- 自我參照不做級聯
        ON DELETE NO Action
        ON UPDATE NO Action,
    CONSTRAINT [FKMenuItemResource]
        FOREIGN KEY ([ResourceId])
        REFERENCES [dbo].[Resource]([ResourceId])
        ON DELETE SET NULL
        ON UPDATE NO Action
);
GO

-- 使用者角色對應表(不建議直接使用此表，建議改用UserGroup來管理)
-- 通常使用群組來管理使用者角色，但有些情況需要直接對應角色
/*
UserId，RoleId
1，1
1，2
*/
CREATE TABLE [dbo].[UserRole](
    [UserId]    INT NOT NULL,  -- 被[使用者表]參照，與PK名稱一致
    [RoleId]    INT NOT NULL,  -- 被[權限表]參照，與PK名稱一致
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy] INT NULL,
    [UpdatedAt] DATETIME NULL,
    [UpdatedBy] INT NULL,
    [DeletedAt] DATETIME NULL,
    [DeletedBy] INT NULL,
    CONSTRAINT [PKUserRole] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    CONSTRAINT [FKUserRoleUser]
        FOREIGN KEY ([UserId])
        REFERENCES [dbo].[User]([UserId])
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT [FKUserRoleRole]
        FOREIGN KEY ([RoleId])
        REFERENCES [dbo].[Role]([RoleId])
        ON DELETE CASCADE
        ON UPDATE CASCADE
);
GO














-- insert 初始範例資料
-- insert部門
INSERT INTO [dbo].[Department] ([DepartmentCode],[DepartmentName],[DepartmentParentId],[DepartmentIsActive])
VALUES
(N'admin', N'行政部', NULL, 1),
(N'it',    N'資訊部', NULL, 1);

-- insert角色
INSERT [dbo].[Role] ([RoleName], [RoleGroup]) VALUES (N'請購人', N'採購');
INSERT [dbo].[Role] ([RoleName], [RoleGroup]) VALUES (N'採購人', N'採購');
INSERT [dbo].[Role] ([RoleName], [RoleGroup]) VALUES (N'評核人', N'採購');
INSERT [dbo].[Role] ([RoleName], [RoleGroup]) VALUES (N'領用人', N'文管');
INSERT [dbo].[Role] ([RoleName], [RoleGroup]) VALUES (N'負責人', N'文管');
INSERT [dbo].[Role] ([RoleName], [RoleGroup]) VALUES (N'系統管理者', N'系統');

-- insert使用者 (密碼：Abcd+帳號)
INSERT INTO [dbo].[User] ( [UserAccount], [UserPasswordHash], [UserFullName], [UserJobTitle], [UserEmail], [UserPhone], [UserMobile],    [UserIsActive], [UserIsLocked], [UserLoginFailedCount],  [UserLastLoginAt], [UserLastLoginIp], [UserPasswordChangedAt], [UserStatus], [UserRemarks], [DepartmentId],   [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [DeletedAt], [DeletedBy]) VALUES
(N'534159', N'AQAAAAIAAYagAAAAEAuGmeU7ZK3mDlRyENROFEB45r8V9rk2pVH4BJUZYQ3Nwgz0UDiBQxcpicRd1MlSfw==', N'範例使用者1', NULL, N'User1@example.com', NULL, NULL, 1, 0, 0, NULL, NULL, NULL, NULL, NULL, 1,  '2020-05-29T00:00:00', NULL, NULL, NULL, NULL, NULL),
(N'970265', N'AQAAAAIAAYagAAAAECpu7Md8zrZ5a5JhFj+q16dQI4zk04yj2jRIiBCzUn2DSfM4tPhPZnPxHzwIu/cjxg==', N'範例使用者2', NULL, N'User2@example.com', NULL, NULL, 1, 0, 0, NULL, NULL, NULL, NULL, NULL, 1,  GETDATE(), NULL, NULL, NULL, NULL, NULL),
(N'990205', N'AQAAAAIAAYagAAAAEP1XSiS1hCBP1//TP7veqi+o1YGV+cfxjzDdShk+m5pdg6OjQSpLeZNCkbiQs3VlrA==', N'範例使用者3', NULL, N'User3@example.com', NULL, NULL, 1, 0, 0, NULL, NULL, NULL, NULL, NULL, 2,  '2023-03-08T00:00:00', NULL, NULL, NULL, NULL, NULL);

-- insert使用者群組
INSERT INTO [dbo].[UserGroup] ([UserGroupName],[UserGroupDescription])
VALUES
(N'研發部門', N'研發部門所有成員'),
(N'A專案小組', N'負責A專案的成員'),
(N'行政部B組', N'行政部B組所有成員');

-- insert使用者群組成員
-- 先抓使用者群組與使用者的 ID
DECLARE @gidrd   INT, @gidprojA INT, @gidadminB INT;
DECLARE @uid1 INT, @uid2 INT, @uid3 INT;

SELECT @gidrd    = [UserGroupId] FROM [dbo].[UserGroup] WHERE [UserGroupName]=N'研發部門';
SELECT @gidprojA = [UserGroupId] FROM [dbo].[UserGroup] WHERE [UserGroupName]=N'A專案小組';
SELECT @gidadminB= [UserGroupId] FROM [dbo].[UserGroup] WHERE [UserGroupName]=N'行政部B組';

SELECT @uid1 = [UserId] FROM [dbo].[User] WHERE [UserAccount]=N'534159';
SELECT @uid2 = [UserId] FROM [dbo].[User] WHERE [UserAccount]=N'970265';
SELECT @uid3 = [UserId] FROM [dbo].[User] WHERE [UserAccount]=N'990205';

INSERT INTO [dbo].[UserGroupMember] ([UserGroupId],[UserId])
VALUES
(@gidrd,    @uid1),  -- 研發部門有使用者1
(@gidrd,    @uid2),  -- 研發部門有使用者2
(@gidprojA, @uid3),  -- A專案小組有使用者3
(@gidadminB,@uid2);  -- 行政部B組有使用者2

-- insert使用者群組角色
-- 先抓角色 ID
DECLARE @ridreq  INT, @ridbuy INT, @rideval INT, @riduse INT, @ridowner INT, @ridadmin INT;
SELECT @ridreq   = [RoleId] FROM [dbo].[Role] WHERE [RoleName]=N'請購人';
SELECT @ridbuy   = [RoleId] FROM [dbo].[Role] WHERE [RoleName]=N'採購人';
SELECT @rideval  = [RoleId] FROM [dbo].[Role] WHERE [RoleName]=N'評核人';
SELECT @riduse   = [RoleId] FROM [dbo].[Role] WHERE [RoleName]=N'領用人';
SELECT @ridowner = [RoleId] FROM [dbo].[Role] WHERE [RoleName]=N'負責人';
SELECT @ridadmin = [RoleId] FROM [dbo].[Role] WHERE [RoleName]=N'系統管理者';

INSERT INTO [dbo].[UserGroupRole] ([UserGroupId],[RoleId])
VALUES
(@gidrd,    @ridreq),   -- 研發部門有請購人
(@gidrd,    @ridbuy),   -- 研發部門有採購人
(@gidprojA, @rideval),  -- A專案小組有評核人
(@gidadminB,@ridowner); -- 行政部B組有負責人

-- insert資源
INSERT INTO [dbo].[Resource] ([ResourceType],[ResourceKey],[ResourceDisplayName],[ResourceIsActive])
VALUES
(N'PAGE', N'Control',     N'文件管理', 1),
(N'PAGE', N'CFileQuery',  N'文件查詢', 1),
(N'PAGE', N'CIssueTables',N'表單發行', 1);

-- insert動作
INSERT INTO [dbo].[AppAction] ([AppActionName],[AppActionDisplayName],[AppActionOrder]) VALUES
(N'index',   N'首頁/列表', 10),
(N'create',  N'新增',      20),
(N'edit',    N'編輯',      30),
(N'delete',  N'刪除',      40),
(N'details', N'檢視明細',  50),
(N'export',  N'匯出',      60),
(N'import',  N'匯入',      70);

-- insert角色權限
DECLARE @rescontrol INT, @resquery INT, @resissue INT;
SELECT @rescontrol = [ResourceId] FROM [dbo].[Resource] WHERE [ResourceKey]=N'Control';
SELECT @resquery   = [ResourceId] FROM [dbo].[Resource] WHERE [ResourceKey]=N'CFileQuery';
SELECT @resissue   = [ResourceId] FROM [dbo].[Resource] WHERE [ResourceKey]=N'CIssueTables';

DECLARE @actindex INT, @actcreate INT, @actedit INT, @actdelete INT, @actdetails INT, @actexport INT, @actimport INT;
SELECT @actindex   = [AppActionId] FROM [dbo].[AppAction] WHERE [AppActionName]=N'index';
SELECT @actcreate  = [AppActionId] FROM [dbo].[AppAction] WHERE [AppActionName]=N'create';
SELECT @actedit    = [AppActionId] FROM [dbo].[AppAction] WHERE [AppActionName]=N'edit';
SELECT @actdelete  = [AppActionId] FROM [dbo].[AppAction] WHERE [AppActionName]=N'delete';
SELECT @actdetails = [AppActionId] FROM [dbo].[AppAction] WHERE [AppActionName]=N'details';
SELECT @actexport  = [AppActionId] FROM [dbo].[AppAction] WHERE [AppActionName]=N'export';
SELECT @actimport  = [AppActionId] FROM [dbo].[AppAction] WHERE [AppActionName]=N'import';

-- 範例授權
-- 請購人、採購人、領用人：可看查詢頁 列表/明細
INSERT INTO [dbo].[RolePermission] ([RoleId],[ResourceId],[AppActionId]) VALUES
(@ridreq, @resquery, @actindex),
(@ridreq, @resquery, @actdetails),
(@ridbuy, @resquery, @actindex),
(@ridbuy, @resquery, @actdetails),
(@riduse, @resquery, @actindex),
(@riduse, @resquery, @actdetails);

-- 評核人：查詢頁 列表/明細 + 表單發行 新增/編輯（含進入頁的 index）
INSERT INTO [dbo].[RolePermission] ([RoleId],[ResourceId],[AppActionId]) VALUES
(@rideval, @resquery, @actindex),
(@rideval, @resquery, @actdetails),
(@rideval, @resissue, @actindex),
(@rideval, @resissue, @actcreate),
(@rideval, @resissue, @actedit);

-- 負責人：查詢頁 列表/明細 + 匯出；表單發行 新增/編輯（含 index）
INSERT INTO [dbo].[RolePermission] ([RoleId],[ResourceId],[AppActionId]) VALUES
(@ridowner, @resquery, @actindex),
(@ridowner, @resquery, @actdetails),
(@ridowner, @resquery, @actexport),
(@ridowner, @resissue, @actindex),
(@ridowner, @resissue, @actcreate),
(@ridowner, @resissue, @actedit);

-- 系統管理者：全資源全動作
INSERT INTO [dbo].[RolePermission] ([RoleId],[ResourceId],[AppActionId])
SELECT @ridadmin, r.[ResourceId], a.[AppActionId]
FROM [dbo].[Resource] r CROSS JOIN [dbo].[AppAction] a;


-- insert選單
-- 系統選單(固定)
INSERT [dbo].[MenuItem] ([MenuItemId], [MenuItemParentId], [MenuItemTitle], [MenuItemIcon], [MenuItemDisplayOrder], [MenuItemIsActive], [ResourceId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [DeletedAt], [DeletedBy]) VALUES (1, NULL, N'系統設定', N'fa-solid fa-gear', 1, 1, NULL, CAST(N'2025-12-09T03:11:00.000' AS DateTime), 1, NULL, NULL, NULL, NULL)
INSERT [dbo].[MenuItem] ([MenuItemId], [MenuItemParentId], [MenuItemTitle], [MenuItemIcon], [MenuItemDisplayOrder], [MenuItemIsActive], [ResourceId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [DeletedAt], [DeletedBy]) VALUES (2, 1, N'系統資源設定', N'fa-solid fa-square-poll-horizontal', 1, 1, 8, CAST(N'2025-12-09T15:12:00.000' AS DateTime), 1, NULL, NULL, NULL, NULL)
INSERT [dbo].[MenuItem] ([MenuItemId], [MenuItemParentId], [MenuItemTitle], [MenuItemIcon], [MenuItemDisplayOrder], [MenuItemIsActive], [ResourceId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [DeletedAt], [DeletedBy]) VALUES (3, 1, N'系統動作設定', N'fa-solid fa-wrench', 2, 1, 10, CAST(N'2025-12-09T15:13:00.000' AS DateTime), 1, NULL, NULL, NULL, NULL)
INSERT [dbo].[MenuItem] ([MenuItemId], [MenuItemParentId], [MenuItemTitle], [MenuItemIcon], [MenuItemDisplayOrder], [MenuItemIsActive], [ResourceId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [DeletedAt], [DeletedBy]) VALUES (4, 1, N'選單項目管理', N'fa-brands fa-elementor', 3, 1, 11, CAST(N'2025-12-09T15:15:00.000' AS DateTime), 1, NULL, NULL, NULL, NULL)
INSERT [dbo].[MenuItem] ([MenuItemId], [MenuItemParentId], [MenuItemTitle], [MenuItemIcon], [MenuItemDisplayOrder], [MenuItemIsActive], [ResourceId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [DeletedAt], [DeletedBy]) VALUES (5, NULL, N'帳號管理', N'fa-solid fa-id-badge', 2, 1, NULL, CAST(N'2025-12-09T14:45:00.000' AS DateTime), 1, CAST(N'2025-12-09T03:12:00.000' AS DateTime), 1, NULL, NULL)
INSERT [dbo].[MenuItem] ([MenuItemId], [MenuItemParentId], [MenuItemTitle], [MenuItemIcon], [MenuItemDisplayOrder], [MenuItemIsActive], [ResourceId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [DeletedAt], [DeletedBy]) VALUES (6, 5, N'使用者帳號管理', N'fa-solid fa-id-badge', 1, 1, 4, CAST(N'2025-12-09T15:08:00.000' AS DateTime), 1, NULL, NULL, NULL, NULL)
INSERT [dbo].[MenuItem] ([MenuItemId], [MenuItemParentId], [MenuItemTitle], [MenuItemIcon], [MenuItemDisplayOrder], [MenuItemIsActive], [ResourceId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [DeletedAt], [DeletedBy]) VALUES (7, 5, N'使用者群組管理', N'fa-solid fa-people-group', 2, 1, 5, CAST(N'2025-12-09T14:46:00.000' AS DateTime), 1, CAST(N'2025-12-09T03:08:00.000' AS DateTime), 1, NULL, NULL)
INSERT [dbo].[MenuItem] ([MenuItemId], [MenuItemParentId], [MenuItemTitle], [MenuItemIcon], [MenuItemDisplayOrder], [MenuItemIsActive], [ResourceId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [DeletedAt], [DeletedBy]) VALUES (8, 5, N'角色管理', N'fa-solid fa-person', 3, 1, 7, CAST(N'2025-12-09T15:10:00.000' AS DateTime), 1, NULL, NULL, NULL, NULL)

-- 選單項目範例資料
-- 父節點：文件管理（對應 Resourcekey=Control）
DECLARE @residcontrol INT, @residquery INT, @residissue INT;
SELECT @residcontrol = [ResourceId] FROM [dbo].[Resource] WHERE [ResourceKey]=N'Control';
SELECT @residquery   = [ResourceId] FROM [dbo].[Resource] WHERE [ResourceKey]=N'CFileQuery';
SELECT @residissue   = [ResourceId] FROM [dbo].[Resource] WHERE [ResourceKey]=N'CIssueTables';

INSERT INTO [dbo].[MenuItem] ([MenuItemParentId],[MenuItemTitle],[MenuItemIcon],[MenuItemDisplayOrder],[MenuItemIsActive],[ResourceId])
VALUES (NULL, N'文件管理', N'fa-solid fa-folder',3, 1, @residcontrol);

DECLARE @menucontrolId INT = SCOPE_IDENTITY();

-- 子節點：文件查詢、表單發行
INSERT INTO [dbo].[MenuItem] ([MenuItemParentId],[MenuItemTitle],[MenuItemIcon],[MenuItemDisplayOrder],[MenuItemIsActive],[ResourceId])
VALUES
(@menucontrolId, N'文件查詢', N'fa-solid fa-file',     1, 1, @residquery),
(@menucontrolId, N'表單發行', N'fa-solid fa-file-alt', 2, 1, @residissue);