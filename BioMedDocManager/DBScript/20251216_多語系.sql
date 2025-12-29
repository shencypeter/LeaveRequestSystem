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

/*
清空資料
-- 1) 用 DELETE 依 FK 由子到父清空
DELETE FROM [dbo].[LocalizationString];

-- 2) 如果有 Identity，要重設從 1 開始
DBCC CHECKIDENT ('[dbo].[LocalizationString]', RESEED, 0);
*/


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


-- 操作結果
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

-- Delete 刪除確認
(N'Common.ConfirmDelete', N'zh-TW', N'確認刪除該筆資料嗎？', N'Common', 1),
(N'Common.ConfirmDelete', N'en-US', N'Are you sure you want to delete this record?', N'Common', 1),

-- CRUD / 表單
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

(N'Common.Verify', N'zh-TW', N'驗證', N'Common', 1),
(N'Common.Verify', N'en-US', N'Verify', N'Common', 1),

(N'Common.Actions', N'zh-TW', N'功能', N'Common', 1),
(N'Common.Actions', N'en-US', N'Actions', N'Common', 1),

(N'Common.PleaseSelect', N'zh-TW', N'請選擇', N'Common', 1),
(N'Common.PleaseSelect', N'en-US', N'Please select', N'Common', 1),

-- 排序按鈕
(N'Common.SortByColumn.TooltipPrefix', N'zh-TW', N'點選「', N'Common', 1),
(N'Common.SortByColumn.TooltipPrefix', N'en-US', N'Click "', N'Common', 1),

(N'Common.SortByColumn.TooltipSuffix', N'zh-TW', N'」欄位進行排序', N'Common', 1),
(N'Common.SortByColumn.TooltipSuffix', N'en-US', N'" to sort by this column', N'Common', 1),


-- 狀態
(N'Common.None', N'zh-TW', N'無', N'Common', 1),
(N'Common.None', N'en-US', N'N/A', N'Common', 1),

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


-- 部分視圖：Navbar / Menu / Banner / Breadcrumbs
(N'Common.Guest', N'zh-TW', N'訪客', N'Common', 1),
(N'Common.Guest', N'en-US', N'Guest', N'Common', 1),

(N'Common.Login', N'zh-TW', N'登入', N'Common', 1),
(N'Common.Login', N'en-US', N'Log in', N'Common', 1),

(N'Common.Logout', N'zh-TW', N'登出', N'Common', 1),
(N'Common.Logout', N'en-US', N'Log out', N'Common', 1),

(N'Common.ChangePassword', N'zh-TW', N'更改密碼', N'Common', 1),
(N'Common.ChangePassword', N'en-US', N'Change password', N'Common', 1),

(N'Common.ContactAdminIfAnyQuestion', N'zh-TW', N'若有疑問請聯絡系統管理員', N'Common', 1),
(N'Common.ContactAdminIfAnyQuestion', N'en-US', N'If you have any questions, please contact the system administrator.', N'Common', 1),

(N'Common.Home', N'zh-TW', N'首頁', N'Common', 1),
(N'Common.Home', N'en-US', N'Home', N'Common', 1),


-- Permission / Navigation
(N'Common.NoPermissionContactAdmin', N'zh-TW', N'您的帳號尚未開通使用權限，請聯絡系統管理員', N'Common', 1),
(N'Common.NoPermissionContactAdmin', N'en-US', N'Your account has not been granted access yet. Please contact the system administrator.', N'Common', 1),

(N'Common.BackToUpper', N'zh-TW', N'返回上層', N'Common', 1),
(N'Common.BackToUpper', N'en-US', N'Back', N'Common', 1),


-- ===== 驗證 =====
(N'AccountSettings.RegisterTotp', N'zh-TW', N'註冊 TOTP', N'AccountSettings', 1),
(N'AccountSettings.RegisterTotp', N'en-US', N'Register TOTP', N'AccountSettings', 1),

(N'Auth.LoginRequired', N'zh-TW', N'您尚未登入，請點此登入', N'Auth', 1),
(N'Auth.LoginRequired', N'en-US', N'You are not logged in. Please click here to log in.', N'Auth', 1),

(N'Auth.AccessDenied', N'zh-TW', N'您的帳號尚未開通使用權限', N'Auth', 1),
(N'Auth.AccessDenied', N'en-US', N'Your account has not been granted access yet.', N'Auth', 1),


-- ===== Menu Fallback =====
(N'Menu.Unnamed', N'zh-TW', N'(未命名選單)', N'Menu', 1),
(N'Menu.Unnamed', N'en-US', N'(Unnamed menu)', N'Menu', 1),

(N'Menu.UnnamedItem', N'zh-TW', N'(未命名)', N'Menu', 1),
(N'Menu.UnnamedItem', N'en-US', N'(Unnamed)', N'Menu', 1),


-- ===== 密碼政策 ===== 
(N'Parameter.SEC_PASSWORD_MIN_LENGTH.Label', N'zh-TW', N'密碼最少長度', N'Security', 1),
(N'Parameter.SEC_PASSWORD_MIN_LENGTH.Label', N'en-US', N'Minimum password length', N'Security', 1),

(N'Parameter.SEC_PASSWORD_EXPIRE_DAYS.Label', N'zh-TW', N'密碼過期天數', N'Security', 1),
(N'Parameter.SEC_PASSWORD_EXPIRE_DAYS.Label', N'en-US', N'Password expiry (days)', N'Security', 1),

(N'Parameter.SEC_2FA_TOTP_ENABLED.Label', N'zh-TW', N'啟用 2FA（TOTP）', N'Security',1),
(N'Parameter.SEC_2FA_TOTP_ENABLED.Label', N'en-US', N'Enable 2FA (TOTP)', N'Security', 1),

-- ===== 頁面分頁 =====
(N'Pager.First', N'zh-TW', N'首頁', N'Pager', 1),
(N'Pager.First', N'en-US', N'First', N'Pager', 1),

(N'Pager.Previous', N'zh-TW', N'前一頁', N'Pager', 1),
(N'Pager.Previous', N'en-US', N'Previous', N'Pager', 1),

(N'Pager.Next', N'zh-TW', N'下一頁', N'Pager', 1),
(N'Pager.Next', N'en-US', N'Next', N'Pager', 1),

(N'Pager.Last', N'zh-TW', N'末頁', N'Pager', 1),
(N'Pager.Last', N'en-US', N'Last', N'Pager', 1),

(N'Pager.TotalPrefix', N'zh-TW', N'總共 ', N'Pager', 1),
(N'Pager.TotalPrefix', N'en-US', N'Total ', N'Pager', 1),

(N'Pager.TotalSuffix', N'zh-TW', N' 筆，', N'Pager', 1),
(N'Pager.TotalSuffix', N'en-US', N' items, ', N'Pager', 1),

(N'Pager.PageSize.Label', N'zh-TW', N'每頁顯示', N'Pager', 1),
(N'Pager.PageSize.Label', N'en-US', N'Per page', N'Pager', 1),

(N'Pager.Items', N'zh-TW', N'筆', N'Pager', 1),
(N'Pager.Items', N'en-US', N'items', N'Pager', 1),


-- ===== 各頁面文字 =====
-- ===== 客製化錯誤頁面 =====
(N'Error.RedirectCountdown.Prefix', N'zh-TW', N'將在 ', N'Error', 1),
(N'Error.RedirectCountdown.Prefix', N'en-US', N'Redirecting in ', N'Error', 1),

(N'Error.RedirectCountdown.Suffix', N'zh-TW', N' 秒後自動返回首頁...', N'Error', 1),
(N'Error.RedirectCountdown.Suffix', N'en-US', N' seconds. You will be redirected to the home page...', N'Error', 1),


-- ===== 首頁 =====
(N'Home.Welcome.Title', N'zh-TW', N'歡迎使用', N'Home', 1),
(N'Home.Welcome.Title', N'en-US', N'Welcome', N'Home', 1),

(N'Home.Welcome.SubTitle', N'zh-TW', N'文管與電子採購系統（範例）', N'Home', 1),
(N'Home.Welcome.SubTitle', N'en-US', N'Document Control & E-Procurement System (Demo)', N'Home', 1),

-- ===== 登入頁 =====
(N'Login.SessionExpired', N'zh-TW', N'系統偵測到您的登入已過期，請重新登入。', N'Login', 1),
(N'Login.SessionExpired', N'en-US', N'Your session has expired. Please log in again.', N'Login', 1),

(N'Login.Account', N'zh-TW', N'帳號', N'Login', 1),
(N'Login.Account', N'en-US', N'Account', N'Login', 1),

(N'Login.Account.Placeholder', N'zh-TW', N'請輸入登入帳號', N'Login', 1),
(N'Login.Account.Placeholder', N'en-US', N'Enter your account', N'Login', 1),

(N'Login.Password', N'zh-TW', N'密碼', N'Login', 1),
(N'Login.Password', N'en-US', N'Password', N'Login', 1),

(N'Login.Password.Placeholder', N'zh-TW', N'請輸入密碼', N'Login', 1),
(N'Login.Password.Placeholder', N'en-US', N'Enter your password', N'Login', 1),

(N'Login.Captcha', N'zh-TW', N'驗證碼', N'Login', 1),
(N'Login.Captcha', N'en-US', N'Captcha', N'Login', 1),

(N'Login.Captcha.Placeholder', N'zh-TW', N'點擊圖片更換驗證碼', N'Login', 1),
(N'Login.Captcha.Placeholder', N'en-US', N'Click the image to refresh the captcha', N'Login', 1),

(N'Login.Captcha.RefreshTitle', N'zh-TW', N'點擊重新產生', N'Login', 1),
(N'Login.Captcha.RefreshTitle', N'en-US', N'Click to regenerate', N'Login', 1),


-- ===== 兩階段驗證 =====
(N'Login.TwoFactor.Title', N'zh-TW', N'兩步驟驗證', N'Login', 1),
(N'Login.TwoFactor.Title', N'en-US', N'Two-step verification', N'Login', 1),

(N'Login.TwoFactor.NoProviderEnabled', N'zh-TW', N'目前未啟用任何兩步驟驗證方式，請重新登入或洽系統管理者。', N'Login', 1),
(N'Login.TwoFactor.NoProviderEnabled', N'en-US', N'No two-step verification method is enabled. Please log in again or contact the system administrator.', N'Login', 1),

(N'Login.TwoFactor.Provider.Label', N'zh-TW', N'驗證方式', N'Login', 1),
(N'Login.TwoFactor.Provider.Label', N'en-US', N'Verification method', N'Login', 1),

(N'Login.TwoFactor.Provider.Email', N'zh-TW', N'Email 驗證碼', N'Login', 1),
(N'Login.TwoFactor.Provider.Email', N'en-US', N'Email code', N'Login', 1),

(N'Login.TwoFactor.Provider.Totp', N'zh-TW', N'Authenticator 驗證碼', N'Login', 1),
(N'Login.TwoFactor.Provider.Totp', N'en-US', N'Authenticator code', N'Login', 1),

(N'Login.TwoFactor.Email.Hint', N'zh-TW', N'系統會寄送 6 碼驗證碼至您的註冊 Email，請在有效時間內輸入。', N'Login', 1),
(N'Login.TwoFactor.Email.Hint', N'en-US', N'A 6-digit code will be sent to your registered email. Please enter it within the valid time.', N'Login', 1),

(N'Login.TwoFactor.Totp.Hint', N'zh-TW', N'請打開手機的 Authenticator App（如 Google Authenticator），輸入目前顯示的 6 碼動態驗證碼。', N'Login', 1),
(N'Login.TwoFactor.Totp.Hint', N'en-US', N'Open your authenticator app (e.g., Google Authenticator) and enter the current 6-digit code shown.', N'Login', 1),

(N'Login.TwoFactor.Email.SendOrResend', N'zh-TW', N'寄送 / 重寄 Email 驗證碼', N'Login', 1),
(N'Login.TwoFactor.Email.SendOrResend', N'en-US', N'Send / resend email code', N'Login', 1),

(N'Login.TwoFactor.Code.Label', N'zh-TW', N'驗證碼', N'Login', 1),
(N'Login.TwoFactor.Code.Label', N'en-US', N'Code', N'Login', 1),

(N'Login.TwoFactor.Code.Placeholder', N'zh-TW', N'請輸入 6 位數驗證碼', N'Login', 1),
(N'Login.TwoFactor.Code.Placeholder', N'en-US', N'Enter the 6-digit code', N'Login', 1),


-- ===== 系統資源管理 =====
-- Index
(N'Resource.Index.Title', N'zh-TW', N'系統資源管理', N'Resource', 1),
(N'Resource.Index.Title', N'en-US', N'System Resource Management', N'Resource', 1),

(N'Resource.Query.ResourceType.Label', N'zh-TW', N'資源類型', N'Resource', 1),
(N'Resource.Query.ResourceType.Label', N'en-US', N'Resource type:', N'Resource', 1),

(N'Resource.Query.ResourceKey.Label', N'zh-TW', N'資源代號', N'Resource', 1),
(N'Resource.Query.ResourceKey.Label', N'en-US', N'Resource key:', N'Resource', 1),

(N'Resource.Query.ResourceDisplayName.Label', N'zh-TW', N'顯示名稱', N'Resource', 1),
(N'Resource.Query.ResourceDisplayName.Label', N'en-US', N'Display name:', N'Resource', 1),

(N'Resource.Query.ResourceIsActive.Label', N'zh-TW', N'是否啟用', N'Resource', 1),
(N'Resource.Query.ResourceIsActive.Label', N'en-US', N'Active:', N'Resource', 1),

(N'Resource.ResourceType.Placeholder', N'zh-TW', N'例如：PAGE / API', N'Resource', 1),
(N'Resource.ResourceType.Placeholder', N'en-US', N'e.g., PAGE / API', N'Resource', 1),

(N'Resource.ResourceKey.Placeholder', N'zh-TW', N'請輸入資源代號', N'Resource', 1),
(N'Resource.ResourceKey.Placeholder', N'en-US', N'Enter resource key', N'Resource', 1),

(N'Resource.ResourceDisplayName.Placeholder', N'zh-TW', N'請輸入顯示名稱', N'Resource', 1),
(N'Resource.ResourceDisplayName.Placeholder', N'en-US', N'Enter display name', N'Resource', 1),

-- Create
(N'Resource.Create.Title', N'zh-TW', N'系統資源管理 - 新增', N'Resource', 1),
(N'Resource.Create.Title', N'en-US', N'System Resource Management - Create', N'Resource', 1),

-- Edit
(N'Resource.Edit.Title', N'zh-TW', N'系統資源管理 - 編輯', N'Resource', 1),
(N'Resource.Edit.Title', N'en-US', N'System Resource Management - Edit', N'Resource', 1),

-- Delete
(N'Resource.Delete.Title', N'zh-TW', N'系統資源管理 - 刪除', N'Resource', 1),
(N'Resource.Delete.Title', N'en-US', N'System Resource Management - Delete', N'Resource', 1),

-- 角色權限使用狀況
(N'Resource.RolePermissionUsage.Title', N'zh-TW', N'角色權限使用狀況', N'Resource', 1),
(N'Resource.RolePermissionUsage.Title', N'en-US', N'Role permission usage', N'Resource', 1),

(N'Resource.RolePermissionUsage.None', N'zh-TW', N'目前尚未有任何角色權限使用此資源。', N'Resource', 1),
(N'Resource.RolePermissionUsage.None', N'en-US', N'No role permissions are currently using this resource.', N'Resource', 1),

(N'Resource.RolePermissionUsage.CountPrefix', N'zh-TW', N'此資源目前被 ', N'Resource', 1),
(N'Resource.RolePermissionUsage.CountPrefix', N'en-US', N'This resource is currently assigned to ', N'Resource', 1),

(N'Resource.RolePermissionUsage.CountSuffix', N'zh-TW', N' 個角色設定權限。', N'Resource', 1),
(N'Resource.RolePermissionUsage.CountSuffix', N'en-US', N' role(s).', N'Resource', 1),

(N'Resource.RolePermissionUsage.RoleGroupListTitle', N'zh-TW', N'使用此資源的角色／群組：', N'Resource', 1),
(N'Resource.RolePermissionUsage.RoleGroupListTitle', N'en-US', N'Roles / groups using this resource:', N'Resource', 1),

(N'Resource.UnassignedGroup', N'zh-TW', N'未指定群組', N'Resource', 1),
(N'Resource.UnassignedGroup', N'en-US', N'No group assigned', N'Resource', 1),

(N'Resource.RolePermissionUsage.HasPermissionButNoGroup', N'zh-TW', N'目前已由部分角色設定權限，但尚未指定任何使用者群組。', N'Resource', 1),
(N'Resource.RolePermissionUsage.HasPermissionButNoGroup', N'en-US', N'Permissions are configured for some roles, but no user groups have been assigned yet.', N'Resource', 1),

-- Delete 資源目前仍被角色權限使用
(N'Resource.Delete.Blocked.Prefix', N'zh-TW', N'此資源目前仍被角色權限使用，', N'Resource', 1),
(N'Resource.Delete.Blocked.Prefix', N'en-US', N'This resource is still used by role permissions, ', N'Resource', 1),

(N'Resource.Delete.Blocked.CannotDelete', N'zh-TW', N'無法刪除', N'Resource', 1),
(N'Resource.Delete.Blocked.CannotDelete', N'en-US', N'cannot be deleted', N'Resource', 1),

(N'Resource.Delete.Blocked.Instruction', N'zh-TW', N'若要刪除，請先在「角色管理-權限設定」中取消此資源相關的權限設定。', N'Resource', 1),
(N'Resource.Delete.Blocked.Instruction', N'en-US', N'To delete it, please remove the related permission settings under "Role Management - Permission Settings" first.', N'Resource', 1),

-- Details
(N'Resource.Details.Title', N'zh-TW', N'系統資源管理 - 詳細資料', N'Resource', 1),
(N'Resource.Details.Title', N'en-US', N'System Resource Management - Details', N'Resource', 1),

-- ===== 系統動作管理 =====

-- Index
(N'AppAction.Index.Title', N'zh-TW', N'系統動作管理', N'AppAction', 1),
(N'AppAction.Index.Title', N'en-US', N'System Action Management', N'AppAction', 1),

(N'AppAction.AppActionName.Label', N'zh-TW', N'動作名稱：', N'AppAction', 1),
(N'AppAction.AppActionName.Label', N'en-US', N'Action name:', N'AppAction', 1),

(N'AppAction.AppActionDisplayName.Label', N'zh-TW', N'顯示名稱：', N'AppAction', 1),
(N'AppAction.AppActionDisplayName.Label', N'en-US', N'Display name:', N'AppAction', 1),

(N'AppAction.AppActionName.Placeholder', N'zh-TW', N'例如：Index / Create / Edit / Delete', N'AppAction', 1),
(N'AppAction.AppActionName.Placeholder', N'en-US', N'e.g., Index / Create / Edit / Delete', N'AppAction', 1),

(N'AppAction.AppActionDisplayName.Placeholder', N'zh-TW', N'請輸入顯示名稱', N'AppAction', 1),
(N'AppAction.AppActionDisplayName.Placeholder', N'en-US', N'Enter display name', N'AppAction', 1),

-- Create
(N'AppAction.Create.Title', N'zh-TW', N'系統動作管理 - 新增', N'AppAction', 1),
(N'AppAction.Create.Title', N'en-US', N'System Action Management - Create', N'AppAction', 1),

-- Edit
(N'AppAction.Edit.Title', N'zh-TW', N'系統動作管理 - 編輯', N'AppAction', 1),
(N'AppAction.Edit.Title', N'en-US', N'System Action Management - Edit', N'AppAction', 1),

-- Delete
(N'AppAction.Delete.Title', N'zh-TW', N'系統動作管理 - 刪除', N'AppAction', 1),
(N'AppAction.Delete.Title', N'en-US', N'System Action Management - Delete', N'AppAction', 1),

(N'AppAction.RolePermissionUsage.Title', N'zh-TW', N'角色權限關聯', N'AppAction', 1),
(N'AppAction.RolePermissionUsage.Title', N'en-US', N'Role permission association', N'AppAction', 1),

(N'AppAction.RolePermissionUsage.None', N'zh-TW', N'目前尚未有任何角色使用此動作。', N'AppAction', 1),
(N'AppAction.RolePermissionUsage.None', N'en-US', N'No roles are currently using this action.', N'AppAction', 1),

(N'AppAction.Delete.Blocked.CannotDelete', N'zh-TW', N'無法刪除', N'AppAction', 1),
(N'AppAction.Delete.Blocked.CannotDelete', N'en-US', N'cannot be deleted', N'AppAction', 1),

(N'AppAction.Delete.Blocked.Instruction', N'zh-TW', N'若要刪除，請先在「角色管理-權限設定」中，取消所有使用此動作的權限設定。', N'AppAction', 1),
(N'AppAction.Delete.Blocked.Instruction', N'en-US', N'To delete it, please remove all permissions that use this action in Role Management - Permission Settings.', N'AppAction', 1),

--  Delete 動作目前仍被角色使用
(N'AppAction.RolePermissionUsage.CountPrefix', N'zh-TW', N'此動作目前被 ', N'AppAction', 1),
(N'AppAction.RolePermissionUsage.CountPrefix', N'en-US', N'This action is currently used by ', N'AppAction', 1),

(N'AppAction.RolePermissionUsage.CountSuffix', N'zh-TW', N' 組角色使用。', N'AppAction', 1),
(N'AppAction.RolePermissionUsage.CountSuffix', N'en-US', N' role-permission pair(s).', N'AppAction', 1),

(N'AppAction.RolePermissionUsage.NoDetail', N'zh-TW', N'已有角色權限使用此動作，但無法取得對應的詳細資料。', N'AppAction', 1),
(N'AppAction.RolePermissionUsage.NoDetail', N'en-US', N'Permissions exist for this action, but related details could not be retrieved.', N'AppAction', 1),

-- Details
(N'AppAction.Details.Title', N'zh-TW', N'系統動作管理 - 詳細資料', N'AppAction', 1),
(N'AppAction.Details.Title', N'en-US', N'System Action Management - Details', N'AppAction', 1),



-- ===== 系統角色管理 =====
(N'Role.RoleGroup', N'zh-TW', N'角色群組', N'Role', 1),
(N'Role.RoleGroup', N'en-US', N'Role group', N'Role', 1),

(N'Role.RoleName', N'zh-TW', N'角色名稱', N'Role', 1),
(N'Role.RoleName', N'en-US', N'Role name', N'Role', 1),








;







;
