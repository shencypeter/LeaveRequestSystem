-- 建立Department表，儲存部門基本資料(1)
-- 建立Role、User、UserGroup表，儲存角色(1)、使用者(使用者N--1部門)與使用者群組(1)基本資料
-- 建立UserGroupMember、UserGroupRole，儲存使用者群組成員(使用者群組成員N--N使用者)與使用者群組角色(使用者群組角色N--N角色)
-- 建立Resource、AppAction，儲存資源(1)、動作(1)
-- 建立RolePermission、MenuItem，儲存角色權限(角色N--N資源N--N動作)與選單(選單1--1資源)
-- 建立UserRole，儲存使用者角色對應表(使用者角色N--N使用者)，通常應使用「使用者群組」方式來管理使用者角色，除非特殊情況需要直接對應角色，否則不建議使用此表

-- 部門表
/*
DepartmentCode
admin
it
*/
CREATE TABLE [dbo].[Department] (
    [DepartmentId]          BIGINT IDENTITY(1,1) NOT NULL,
    [DepartmentCode]        NVARCHAR(50)  NOT NULL,
    [DepartmentParentId]    BIGINT NULL,  -- 自我參照
    [DepartmentIsActive]    BIT NOT NULL DEFAULT 1,
    [CreatedAt]             DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]             BIGINT NULL,
    [UpdatedAt]             DATETIME NULL,
    [UpdatedBy]             BIGINT NULL,
    [DeletedAt]             DATETIME NULL,
    [DeletedBy]             BIGINT NULL,
    CONSTRAINT [PKDepartment] PRIMARY KEY CLUSTERED ([DepartmentId] ASC),
    CONSTRAINT [FKDepartmentParent]
        FOREIGN KEY ([DepartmentParentId])
        REFERENCES [dbo].[Department]([DepartmentId])
        -- 自我參照不做級聯
        ON DELETE NO Action
        ON UPDATE NO Action
);
GO

-- 角色表
/*
RoleCode，RoleGroup
系統管理者，系統
請購人，採購
採購人，採購
評核人，採購
領用人，文管
負責人，文管
*/
CREATE TABLE [dbo].[Role] (
    [RoleId]        BIGINT IDENTITY(1,1) NOT NULL,
    [RoleCode]      NVARCHAR(100) NOT NULL,-- 從角色名稱變成角色代碼
    [RoleGroup]     NVARCHAR(100) NOT NULL,
    [CreatedAt]     DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]     BIGINT NULL,
    [UpdatedAt]     DATETIME NULL,
    [UpdatedBy]     BIGINT NULL,
    [DeletedAt]     DATETIME NULL,
    [DeletedBy]     BIGINT NULL,
    CONSTRAINT [PKRole] PRIMARY KEY CLUSTERED ([RoleId] ASC)
);
GO

-- 使用者表
/*
UserAccount，UserPasswordHash，UserFullName
534159，Abcd534159，範例使用者1
*/
CREATE TABLE [dbo].[User] (
    [UserId]                    BIGINT IDENTITY(1,1) NOT NULL,
    [UserAccount]               NVARCHAR(100) NOT NULL,
    [UserPasswordHash]          NVARCHAR(255) NOT NULL,
    [UserFullName]              NVARCHAR(100) NOT NULL,
    [UserJobTitle]              NVARCHAR(100) NULL,    
    [UserEmail]                 NVARCHAR(255) NOT NULL,
    [UserPhone]                 NVARCHAR(50) NULL,
    [UserMobile]                NVARCHAR(50) NULL,
    [UserIsActive]              BIT NOT NULL DEFAULT 1,
    [UserIsLocked]              BIT NOT NULL DEFAULT 0,
    [UserLockedUntil]           DATETIME NULL,
    [UserLoginFailedCount]      INT NOT NULL DEFAULT 0,
    [UserLastLoginAt]           DATETIME NULL,
    [UserLastLoginIp]           NVARCHAR(50) NULL,
    [UserPasswordChangedAt]     DATETIME NULL,
    [UserStatus]                INT NULL,
    [UserRemarks]               NVARCHAR(255) NULL,
    [UserTotpSecret]                NVARCHAR(128) NULL,
    [DepartmentId]              INT NULL,  -- 被[部門表]參照，與PK名稱一致
    [CreatedAt]                 DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]                 BIGINT NULL,
    [UpdatedAt]                 DATETIME NULL,
    [UpdatedBy]                 BIGINT NULL,
    [DeletedAt]                 DATETIME NULL,
    [DeletedBy]                 BIGINT NULL,
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
UserGroupCode，UserGroupDescription
研發部門，研發部門所有成員
A專案小組，負責A專案的成員
行政部B組，行政部B組所有成員
*/
CREATE TABLE [dbo].[UserGroup] (
    [UserGroupId]           BIGINT IDENTITY(1,1) NOT NULL,
    [UserGroupCode]         NVARCHAR(100) NOT NULL, -- 從群組名稱變成群組代碼
    [UserGroupDescription]  NVARCHAR(255) NULL,
    [CreatedAt]             DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]             BIGINT NULL,
    [UpdatedAt]             DATETIME NULL,
    [UpdatedBy]             BIGINT NULL,
    [DeletedAt]             DATETIME NULL,
    [DeletedBy]             BIGINT NULL,
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
    [UserGroupId]   BIGINT NOT NULL,  -- 被[使用者群組表]參照，與PK名稱一致
    [UserId]        BIGINT NOT NULL,  -- 被[使用者表]參照，與PK名稱一致
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
    [UserGroupId]   BIGINT NOT NULL,  -- 被[使用者群組表]參照，與PK名稱一致
    [RoleId]        BIGINT NOT NULL,  -- 被[角色表]參照，與PK名稱一致
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
    [ResourceId]            BIGINT IDENTITY(1,1) NOT NULL,
    [ResourceType]          NVARCHAR(50)  NOT NULL, -- PAGE/API
    [ResourceKey]           NVARCHAR(200) NOT NULL, -- 唯一識別，例如：Controller、/API/V1/Controller等
    --[ResourceDisplayName]   NVARCHAR(200) NOT NULL, -- 顯示名稱 改由多語系表處理(自動抓ResourceKey.Index.Title)
    [ResourceIsActive]      BIT NOT NULL DEFAULT 1,
    [CreatedAt]             DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]             BIGINT NULL,
    [UpdatedAt]             DATETIME NULL,
    [UpdatedBy]             BIGINT NULL,
    [DeletedAt]             DATETIME NULL,
    [DeletedBy]             BIGINT NULL,
    CONSTRAINT [PKResource] PRIMARY KEY CLUSTERED ([ResourceId] ASC)
);
GO

-- 動作表
/*
AppActionCode，AppActionDisplayName
index，首頁/列表
create，新增
edit，編輯
delete，刪除
details，檢視明細
export，匯出
import，匯入
*/
CREATE TABLE [dbo].[AppAction] (
    [AppActionId]           BIGINT IDENTITY(1,1) NOT NULL,
    [AppActionCode]         NVARCHAR(50)  NOT NULL, --從動作名稱變成動作代碼 'index','create','edit','delete','details','export','import',...    
    --[AppActionDisplayName]  NVARCHAR(100) NOT NULL, -- 從多語系表取得顯示名稱
    [AppActionOrder]        INT NOT NULL DEFAULT 0,
    [CreatedAt]             DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]             BIGINT NULL,
    [UpdatedAt]             DATETIME NULL,
    [UpdatedBy]             BIGINT NULL,
    [DeletedAt]             DATETIME NULL,
    [DeletedBy]             BIGINT NULL,
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
    [RoleId]            BIGINT NOT NULL,      -- 被[角色表]參照，與PK名稱一致
    [ResourceId]        BIGINT NOT NULL,      -- 被[資源表]參照，與PK名稱一致
    [AppActionId]       BIGINT NOT NULL,      -- 被[動作表]參照，與PK名稱一致
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
    [MenuItemId]            BIGINT IDENTITY(1,1) NOT NULL,
    [MenuItemParentId]      BIGINT NULL,       -- 自我參照
    [MenuItemCode]          NVARCHAR(100) NOT NULL, -- 選單代碼(因為父類別沒有ResourceId可以對照
    --[MenuItemTitle]         NVARCHAR(100) NOT NULL, -- 改由[資源表]參照，[資源表]本來就有多語系處理
    [MenuItemIcon]          NVARCHAR(100) NULL,
    [MenuItemDisplayOrder]  INT NOT NULL DEFAULT 0,
    [MenuItemIsActive]      BIT NOT NULL DEFAULT 1,
    [ResourceId]            BIGINT NULL,       -- 被[資源表]參照，與PK名稱一致
    [CreatedAt]             DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]             BIGINT NULL,
    [UpdatedAt]             DATETIME NULL,
    [UpdatedBy]             BIGINT NULL,
    [DeletedAt]             DATETIME NULL,
    [DeletedBy]             BIGINT NULL,
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

-- 使用者密碼歷史表
CREATE TABLE [dbo].[UserPasswordHistory] (
    [UserPasswordHistoryId] BIGINT IDENTITY(1,1) NOT NULL,
    [UserId]                BIGINT NOT NULL,
    [PasswordHash]          NVARCHAR(512) NOT NULL,
    [CreatedAt]             DATETIME2(0) NOT NULL 
        CONSTRAINT [DF_UserPasswordHistory_CreatedAt] DEFAULT (SYSDATETIME()),
    [CreatedBy]             BIGINT NULL,
    CONSTRAINT [PK_UserPasswordHistory] PRIMARY KEY CLUSTERED ([UserPasswordHistoryId])
);
GO

ALTER TABLE [dbo].[UserPasswordHistory] WITH CHECK
ADD CONSTRAINT [FK_UserPasswordHistory_Users_UserId]
    FOREIGN KEY([UserId]) REFERENCES [dbo].[User]([UserId])
    ON DELETE NO ACTION ON UPDATE CASCADE;
GO

-- 常用查詢：抓某個 User 最近 N 筆歷史
CREATE INDEX [IX_UserPasswordHistory_UserId_CreatedAt]
ON [dbo].[UserPasswordHistory]([UserId], [CreatedAt] DESC);
GO



-- ====【不建議使用】=== 使用者角色對應表(建議改用UserGroup來管理)
-- 通常使用群組來管理使用者角色，但有些情況需要直接對應角色
/*
UserId，RoleId
1，1
1，2
*/
/*
CREATE TABLE [dbo].[UserRole](
    [UserId]        BIGINT NOT NULL,  -- 被[使用者表]參照，與PK名稱一致
    [RoleId]        BIGINT NOT NULL,  -- 被[權限表]參照，與PK名稱一致
    [CreatedAt]     DATETIME NOT NULL DEFAULT GETDATE(),
    [CreatedBy]     BIGINT NULL,
    [UpdatedAt]     DATETIME NULL,
    [UpdatedBy]     BIGINT NULL,
    [DeletedAt]     DATETIME NULL,
    [DeletedBy]     BIGINT NULL,
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
*/

-- Debug：檢視群組各資源權限
CREATE VIEW RolePermissionViewer
AS
SELECT 
    r.RoleId,
    r.RoleCode,
    res.ResourceId,
    res.ResourceKey,
    a.AppActionId,
    a.AppActionCode
FROM RolePermission rp
INNER JOIN Role r 
    ON rp.RoleId = r.RoleId
INNER JOIN Resource res
    ON rp.ResourceId = res.ResourceId
INNER JOIN AppAction a
    ON rp.AppActionId = a.AppActionId
ORDER BY ResourceId,AppActionOrder
OFFSET 0 ROWS
GO


/*
清空資料
-- 1) 用 DELETE 依 FK 由子到父清空
DELETE FROM [dbo].[RolePermission];
DELETE FROM [dbo].[UserGroupMember];
DELETE FROM [dbo].[UserGroupRole];
DELETE FROM [dbo].[UserGroup];
DELETE FROM [dbo].[UserRole];
DELETE FROM [dbo].[AppAction];
DELETE FROM [dbo].[MenuItem];
DELETE FROM [dbo].[Role];
DELETE FROM [dbo].[Resource];
DELETE FROM [dbo].[Department];
DELETE FROM [dbo].[User];

-- 2) 如果有 Identity，要重設從 1 開始
DBCC CHECKIDENT ('[dbo].[UserGroup]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[AppAction]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[MenuItem]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Role]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Resource]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Department]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[User]', RESEED, 0);
*/



-- insert 初始範例資料
-- insert部門
INSERT INTO [dbo].[Department] ([DepartmentCode],[DepartmentParentId],[DepartmentIsActive])
VALUES
(N'Admin', NULL, 1),
(N'IT',    NULL, 1);

-- insert使用者 (密碼：Abcd+帳號)
INSERT INTO [dbo].[User]
([UserAccount],[UserPasswordHash],[UserFullName],[UserJobTitle],[UserEmail],[UserPhone],[UserMobile],
 [UserIsActive],[UserIsLocked],[UserLoginFailedCount],[UserLastLoginAt],[UserLastLoginIp],[UserPasswordChangedAt],
 [UserStatus],[UserRemarks],[DepartmentId],
 [CreatedBy],[UpdatedAt],[UpdatedBy],[DeletedAt],[DeletedBy])
VALUES
(N'534159', N'AQAAAAIAAYagAAAAEAuGmeU7ZK3mDlRyENROFEB45r8V9rk2pVH4BJUZYQ3Nwgz0UDiBQxcpicRd1MlSfw==', N'範例使用者1', NULL, N'User1@example.com', NULL, NULL, 1, 0, 0, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL, NULL, NULL, NULL),
(N'970265', N'AQAAAAIAAYagAAAAECpu7Md8zrZ5a5JhFj+q16dQI4zk04yj2jRIiBCzUn2DSfM4tPhPZnPxHzwIu/cjxg==', N'範例使用者2', NULL, N'User2@example.com', NULL, NULL, 1, 0, 0, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL, NULL, NULL, NULL),
(N'990205', N'AQAAAAIAAYagAAAAEP1XSiS1hCBP1//TP7veqi+o1YGV+cfxjzDdShk+m5pdg6OjQSpLeZNCkbiQs3VlrA==', N'範例使用者3', NULL, N'User3@example.com', NULL, NULL, 1, 0, 0, NULL, NULL, NULL, NULL, NULL, 2, NULL, NULL, NULL, NULL, NULL);


-- insert角色
INSERT [dbo].[Role] ([RoleCode], [RoleGroup]) VALUES
(N'SYSTEM_ADMIN', N'SYSTEM'),        -- 系統管理者 / 系統
(N'REQUESTER',    N'PROCUREMENT'),   -- 請購人 / 採購
(N'PURCHASER',    N'PROCUREMENT'),   -- 採購人 / 採購
(N'EVALUATOR',    N'PROCUREMENT'),   -- 評核人 / 採購
(N'CONSUMER',     N'DOCUMENT'),      -- 領用人 / 文管
(N'OWNER',        N'DOCUMENT');      -- 負責人 / 文管



-- insert使用者群組
INSERT INTO [dbo].[UserGroup] ([UserGroupCode],[UserGroupDescription])
VALUES
(N'系統組', N'系統管理所有成員'),
(N'研發部門', N'研發部門所有成員'),
(N'A專案小組', N'負責A專案的成員'),
(N'行政部B組', N'行政部B組所有成員');

-- insert使用者群組成員
INSERT INTO [dbo].[UserGroupMember] ([UserGroupId],[UserId])
VALUES
(1, 1);  -- 系統組有使用者1

-- insert使用者群組角色
INSERT INTO [dbo].[UserGroupRole] ([UserGroupId],[RoleId])
VALUES
(1, 1);   -- 系統組有系統管理者

-- insert資源
INSERT INTO Resource ([ResourceType], [ResourceKey], [ResourceIsActive])
VALUES
('PAGE', 'Resource',        1),  -- id=1 系統資源管理
('PAGE', 'AppAction',       1),  -- id=2 系統動作管理
('PAGE', 'MenuItem',        1),  -- id=3 選單項目管理
('PAGE', 'AccountSettings', 1),  -- id=4 帳號管理
('PAGE', 'UserGroup',       1),  -- id=5 使用者群組
('PAGE', 'Role',            1),  -- id=6 角色管理
('PAGE', 'UserGroupRole',   1),  -- id=7 使用者群組權限管理
('PAGE', 'RolePermission',  1);  -- id=8 角色權限管理

-- insert動作
INSERT INTO [dbo].[AppAction] ([AppActionCode],[AppActionOrder]) VALUES
(N'Index',              10),   -- 首頁 / 列表
(N'Create',             20),   -- 新增
(N'Edit',               30),   -- 編輯
(N'Delete',             40),   -- 刪除
(N'Details',            50),   -- 檢視明細
(N'Export',             60),   -- 匯出
(N'Import',             70),   -- 匯入
(N'EditGroup',          80),   -- 編輯群組
(N'PreviewPermissions', 90),   -- 預覽使用者群組角色權限
(N'ChangePassword',     100),  -- 變更密碼
(N'ResetPassword',      110);  -- 重設密碼

-- 系統管理者：全資源全動作
INSERT INTO [dbo].[RolePermission] ([RoleId],[ResourceId],[AppActionId])
VALUES
    (1, 1, 1),
    (1, 1, 2),
    (1, 1, 3),
    (1, 1, 4),
    (1, 1, 5),
    (1, 1, 6),
    (1, 1, 7),

    (1, 2, 1),
    (1, 2, 2),
    (1, 2, 3),
    (1, 2, 4),
    (1, 2, 5),
    (1, 2, 6),
    (1, 2, 7),

    (1, 3, 1),
    (1, 3, 2),
    (1, 3, 3),
    (1, 3, 4),
    (1, 3, 5),
    (1, 3, 6),
    (1, 3, 7),

    (1, 4, 1),
    (1, 4, 2),
    (1, 4, 3),
    (1, 4, 4),
    (1, 4, 5),
    (1, 4, 6),
    (1, 4, 7),
     
    -- 額外 EditGroup/PreviewPermissions/ChangePassword/ResetPassword (AppActionId = 8,9,10,11)
    (1, 4, 8),
    (1, 4, 9),
    (1, 4, 10),
    (1, 4, 11),

    (1, 5, 1),
    (1, 5, 2),
    (1, 5, 3),
    (1, 5, 4),
    (1, 5, 5),
    (1, 5, 6),
    (1, 5, 7),

    (1, 6, 1),
    (1, 6, 2),
    (1, 6, 3),
    (1, 6, 4),
    (1, 6, 5),
    (1, 6, 6),
    (1, 6, 7),

    -- 使用者群組權限管理 UserGroupRole 只有編輯和預覽權限
    (1, 7, 3),
    (1, 7, 9),-- 額外 PreviewPermissions (AppActionId = 9)

    -- 角色權限管理	RolePermission 只有編輯  
    (1, 8, 3);

-- insert選單
-- 系統選單(固定)
INSERT INTO MenuItem (MenuItemParentId, MenuItemIcon, MenuItemDisplayOrder, MenuItemIsActive, ResourceId, CreatedBy)
VALUES
(NULL, 'fa-solid fa-gear',                   1, 1, NULL, 1), -- 系統管理
(1,    'fa-solid fa-square-poll-horizontal', 1, 1, 1,    1), -- 系統資源管理
(1,    'fa-solid fa-wrench',                 2, 1, 2,    1), -- 系統動作管理
(1,    'fa-brands fa-elementor',             3, 1, 3,    1), -- 選單項目管理
(NULL, 'fa-solid fa-id-badge',               2, 1, NULL, 1), -- 帳號管理
(5,    'fa-solid fa-id-badge',               1, 1, 4,    1), -- 使用者帳號管理
(5,    'fa-solid fa-people-group',           2, 1, 5,    1), -- 使用者群組管理
(5,    'fa-solid fa-person',                 3, 1, 6,    1); -- 角色管理