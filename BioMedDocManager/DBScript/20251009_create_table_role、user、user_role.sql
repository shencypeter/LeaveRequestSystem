-- 建立role、user、user_role
CREATE TABLE [dbo].[role](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[role_name] [nvarchar](100) NOT NULL,
	[role_group] [nvarchar](100) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE TABLE [dbo].[user](
    [id]                    INT IDENTITY(1,1) NOT NULL,
    [username]              NVARCHAR(100) NOT NULL,
    [password]              NVARCHAR(255) NOT NULL,
    [full_name]             NVARCHAR(100) NOT NULL,
    [job_title]             NVARCHAR(100) NULL,
    [department_name]       NVARCHAR(100) NULL,  -- 部門名稱(建議未來關連到部門)
    [email]                 NVARCHAR(255) NOT NULL,
    [phone]                 NVARCHAR(50) NULL,
    [mobile]                NVARCHAR(50) NULL,
    [is_active]             BIT NOT NULL DEFAULT 1,
    [is_locked]             BIT NOT NULL DEFAULT 0,
    [login_failed_count]    INT NOT NULL DEFAULT 0,
    [last_login_at]         DATETIME NULL,
    [last_login_ip]         NVARCHAR(50) NULL,
    [password_changed_at]   DATETIME NULL,
    [status]                INT NULL,
    [remarks]               NVARCHAR(255) NULL,
    [created_at]            DATETIME NOT NULL DEFAULT GETDATE(),
    [created_by]            INT NULL,
    [updated_at]            DATETIME NULL,
    [updated_by]            INT NULL,
    [deleted_at]            DATETIME NULL,
    [deleted_by]            INT NULL,
    CONSTRAINT [PK_user] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_user_username] UNIQUE ([username])
);



CREATE TABLE [dbo].[user_role](
	[user_id] [int] NOT NULL,
	[role_id] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[user_id] ASC,
	[role_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- insert 初始資料
-- 新增角色
INSERT [dbo].[role] ([role_name], [role_group]) VALUES (N'請購人', N'採購');
INSERT [dbo].[role] ([role_name], [role_group]) VALUES (N'採購人', N'採購');
INSERT [dbo].[role] ([role_name], [role_group]) VALUES (N'評核人', N'採購');
INSERT [dbo].[role] ([role_name], [role_group]) VALUES (N'領用人', N'文管');
INSERT [dbo].[role] ([role_name], [role_group]) VALUES (N'負責人', N'文管');
INSERT [dbo].[role] ([role_name], [role_group]) VALUES (N'系統管理者', N'系統');

-- 新增使用者 (密碼：Abcd+帳號)
INSERT INTO [dbo].[user] 
(
    [username], [password], [full_name],
    [job_title], [department_name],
    [email], [phone], [mobile],
    [is_active], [is_locked], [login_failed_count],
    [last_login_at], [last_login_ip], [password_changed_at],
    [status], [remarks],
    [created_at], [created_by], [updated_at], [updated_by], [deleted_at], [deleted_by]
)
VALUES
(N'534159',  N'AQAAAAIAAYagAAAAEAuGmeU7ZK3mDlRyENROFEB45r8V9rk2pVH4BJUZYQ3Nwgz0UDiBQxcpicRd1MlSfw==',  N'範例使用者1', NULL, NULL, N'user1@example.com', NULL, NULL, 
 1, 0, 0,  NULL, NULL, NULL,  NULL, NULL,  '2020-05-29T00:00:00', NULL, NULL, NULL, NULL, NULL),
(N'970265',  N'AQAAAAIAAYagAAAAECpu7Md8zrZ5a5JhFj+q16dQI4zk04yj2jRIiBCzUn2DSfM4tPhPZnPxHzwIu/cjxg==',  N'範例使用者2', NULL, NULL, N'user2@example.com', NULL, NULL,
 1, 0, 0, NULL, NULL, NULL, NULL, NULL, GETDATE(), NULL, NULL, NULL, NULL, NULL),
(N'990205',  N'AQAAAAIAAYagAAAAEP1XSiS1hCBP1//TP7veqi+o1YGV+cfxjzDdShk+m5pdg6OjQSpLeZNCkbiQs3VlrA==',  N'範例使用者3', NULL, NULL, N'user3@example.com', NULL, NULL, 1, 0, 0, NULL, NULL, NULL, NULL, NULL, '2023-03-08T00:00:00', NULL, NULL, NULL, NULL, NULL);


-- 新增使用者角色配對
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (1, 1)
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (1, 2)
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (1, 3)
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (1, 4)
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (1, 5)
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (1, 6)
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (2, 2)
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (2, 4)
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (2, 5)
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (3, 2)
INSERT [dbo].[user_role] ([user_id], [role_id]) VALUES (3, 4)