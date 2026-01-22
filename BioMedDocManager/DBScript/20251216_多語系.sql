/*
CREATE TABLE [dbo].[LocalizationString] (
    [LocalizationStringId] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LocalizationString PRIMARY KEY,    
    [LocalizationStringKey] NVARCHAR(200) NOT NULL,-- 例如：Security.Password.MinLength.Label    
    [LocalizationStringCulture] NVARCHAR(20) NOT NULL,-- 例如：zh-TW / en-US（建議用 Culture Name）   
    [LocalizationStringValue] NVARCHAR(MAX) NOT NULL, -- 顯示文字    
    [LocalizationStringCategory] NVARCHAR(100) NULL,-- 分組/模組（可選）：例如 Security / Common / Menu
    [LocalizationStringIsActive] BIT NOT NULL CONSTRAINT DF_LocalizationString_IsActive DEFAULT (1),
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
ON [dbo].[LocalizationString] ([LocalizationStringCulture], [LocalizationStringKey])
INCLUDE ([LocalizationStringIsActive]);

*/
--清空資料
-- 1) 用 DELETE 依 FK 由子到父清空
DELETE FROM [dbo].[LocalizationString];

-- 2) 如果有 Identity，要重設從 1 開始
DBCC CHECKIDENT ('[dbo].[LocalizationString]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[LocalizationString]', RESEED, 0);


-- 新增資料
INSERT INTO [dbo].[LocalizationString] ([LocalizationStringKey],[LocalizationStringCulture],[LocalizationStringValue],[LocalizationStringCategory],[LocalizationStringIsActive])
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

(N'Common.Shortcut', N'zh-TW', N'快捷鍵', N'Common', 1),
(N'Common.Shortcut', N'en-US', N'Shortcut', N'Common', 1),

(N'Common.Greeting.Morning',   N'zh-TW', N'早安',   N'Common', 1),
(N'Common.Greeting.Morning',   N'en-US', N'Good morning',   N'Common', 1),

(N'Common.Greeting.Afternoon',N'zh-TW', N'午安',   N'Common', 1),
(N'Common.Greeting.Afternoon',N'en-US', N'Good afternoon', N'Common', 1),

(N'Common.Greeting.Evening',  N'zh-TW', N'晚安',   N'Common', 1),
(N'Common.Greeting.Evening',  N'en-US', N'Good evening',   N'Common', 1),

-- 操作結果
(N'Common.Success', N'zh-TW', N'操作成功', N'Common', 1),
(N'Common.Success', N'en-US', N'Operation succeeded', N'Common', 1),

(N'Common.Failed', N'zh-TW', N'操作失敗', N'Common', 1),
(N'Common.Failed', N'en-US', N'Operation failed', N'Common', 1),

(N'Common.NoChange', N'zh-TW', N'未變動', N'Common', 1),
(N'Common.NoChange', N'en-US', N'No Change', N'Common', 1),

(N'Common.NoData', N'zh-TW', N'查無資料', N'Common', 1),
(N'Common.NoData', N'en-US', N'No data found', N'Common', 1),

(N'Common.Confirm', N'zh-TW', N'請確認', N'Common', 1),
(N'Common.Confirm', N'en-US', N'Please confirm', N'Common', 1),

(N'Common.Cancelled', N'zh-TW', N'操作已取消', N'Common', 1),
(N'Common.Cancelled', N'en-US', N'Operation cancelled', N'Common', 1),

(N'Common.Message.Prefix', N'zh-TW', N'訊息', N'Common', 1),
(N'Common.Message.Prefix', N'en-US', N'Message', N'Common', 1),

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

(N'Common.Preview', N'zh-TW', N'預覽', N'Common', 1),
(N'Common.Preview', N'en-US', N'Preview', N'Common', 1),

(N'Common.PleaseSelect', N'zh-TW', N'請選擇', N'Common', 1),
(N'Common.PleaseSelect', N'en-US', N'Please select', N'Common', 1),

-- 排序按鈕
(N'Common.SortByColumn.Tooltip.Prefix', N'zh-TW', N'點選「', N'Common', 1),
(N'Common.SortByColumn.Tooltip.Prefix', N'en-US', N'Click "', N'Common', 1),

(N'Common.SortByColumn.Tooltip.Suffix', N'zh-TW', N'」欄位進行排序', N'Common', 1),
(N'Common.SortByColumn.Tooltip.Suffix', N'en-US', N'" to sort by this column', N'Common', 1),


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

(N'Common.Locked',   N'zh-TW', N'已鎖定',   N'Common', 1),
(N'Common.Locked',   N'en-US', N'Locked',    N'Common', 1),

(N'Common.Unlocked', N'zh-TW', N'未鎖定',   N'Common', 1),
(N'Common.Unlocked', N'en-US', N'Unlocked',  N'Common', 1),

(N'Common.All', N'zh-TW', N'全部', N'Common', 1),
(N'Common.All', N'en-US', N'All', N'Common', 1),

-- 稽核欄位
(N'Common.CreatedAt',N'zh-TW', N'建立日期',  N'Common', 1),
(N'Common.CreatedAt',N'en-US', N'Created at',N'Common', 1),

(N'Common.CreatedBy',N'zh-TW', N'建立人',    N'Common', 1),
(N'Common.CreatedBy',N'en-US', N'Created by',N'Common', 1),

(N'Common.UpdatedAt',N'zh-TW', N'更新日期',  N'Common', 1),
(N'Common.UpdatedAt',N'en-US', N'Updated at',N'Common', 1),

(N'Common.UpdatedBy',N'zh-TW', N'更新人',    N'Common', 1),
(N'Common.UpdatedBy',N'en-US', N'Updated by',N'Common', 1),

(N'Common.DeletedAt',N'zh-TW', N'刪除日期',  N'Common', 1),
(N'Common.DeletedAt',N'en-US', N'Deleted at',N'Common', 1),

(N'Common.DeletedBy',N'zh-TW', N'刪除人',    N'Common', 1),
(N'Common.DeletedBy',N'en-US', N'Deleted by',N'Common', 1),


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


-- ===== 身分驗證 =====
(N'AccountSettings.RegisterTotp', N'zh-TW', N'註冊 TOTP', N'AccountSettings', 1),
(N'AccountSettings.RegisterTotp', N'en-US', N'Register TOTP', N'AccountSettings', 1),

(N'Auth.LoginRequired', N'zh-TW', N'您尚未登入，請點此登入', N'Auth', 1),
(N'Auth.LoginRequired', N'en-US', N'You are not logged in. Please click here to log in.', N'Auth', 1),

(N'Auth.AccessDenied', N'zh-TW', N'您的帳號尚未開通使用權限', N'Auth', 1),
(N'Auth.AccessDenied', N'en-US', N'Your account has not been granted access yet.', N'Auth', 1),

(N'Auth.LoginInfoExpired', N'zh-TW', N'登入資訊已失效，請重新登入。', N'Common', 1),
(N'Auth.LoginInfoExpired', N'en-US', N'Login session expired. Please log in again.', N'Common', 1),

(N'Auth.UserNotFoundReLogin', N'zh-TW', N'找不到使用者資料，請重新登入。', N'Common', 1),
(N'Auth.UserNotFoundReLogin', N'en-US', N'User data not found. Please log in again.', N'Common', 1),

(N'Auth.InvalidCredentialsForgotHint', N'zh-TW', N'帳號或密碼錯誤!(若忘記密碼，請洽管理者重設密碼)', N'Auth', 1),
(N'Auth.InvalidCredentialsForgotHint', N'en-US', N'Invalid username or password. If you forgot your password, please contact the administrator.', N'Auth', 1),

(N'Auth.LogoutThanks', N'zh-TW', N'您已登出系統，謝謝您的使用', N'Auth', 1),
(N'Auth.LogoutThanks', N'en-US', N'You have been signed out. Thank you.', N'Auth', 1),

(N'Auth.LockedRetry.Prefix', N'zh-TW', N'帳號或密碼錯誤次數達', N'Auth', 1),
(N'Auth.LockedRetry.Prefix', N'en-US', N'Failed sign-in attempts reached ', N'Auth', 1),

(N'Auth.LockedRetry.Middle', N'zh-TW', N'次以上，請於', N'Auth', 1),
(N'Auth.LockedRetry.Middle', N'en-US', N'. Please try again after ', N'Auth', 1),

(N'Auth.LockedRetry.Suffix', N'zh-TW', N'分鐘後重試(或洽管理者解除鎖定)', N'Auth', 1),
(N'Auth.LockedRetry.Suffix', N'en-US', N' minutes (or contact the administrator to unlock).', N'Auth', 1),

-- ===== Menu Fallback =====
(N'Menu.Unnamed', N'zh-TW', N'(未命名選單)', N'Menu', 1),
(N'Menu.Unnamed', N'en-US', N'(Unnamed menu)', N'Menu', 1),

(N'Menu.UnnamedItem', N'zh-TW', N'(未命名)', N'Menu', 1),
(N'Menu.UnnamedItem', N'en-US', N'(Unnamed)', N'Menu', 1),


-- ===== 密碼政策 ===== 
(N'PasswordPolicy.Title', N'zh-TW', N'密碼須符合以下規則', N'PasswordPolicy', 1),
(N'PasswordPolicy.Title', N'en-US', N'Passwords must meet the following rules', N'PasswordPolicy', 1),


-- Password Policy : Min Length
(N'PasswordPolicy.MinLength.Prefix', N'zh-TW', N'密碼長度至少需', N'PasswordPolicy', 1),
(N'PasswordPolicy.MinLength.Prefix', N'en-US', N'Password length must be at least', N'PasswordPolicy', 1),

(N'PasswordPolicy.MinLength.Suffix', N'zh-TW', N'個字元。', N'PasswordPolicy', 1),
(N'PasswordPolicy.MinLength.Suffix', N'en-US', N'characters.', N'PasswordPolicy', 1),

-- Password Policy : Require Uppercase
(N'PasswordPolicy.RequireUpper.Text', N'zh-TW', N'至少包含 1 個英文大寫字母（A–Z）。', N'PasswordPolicy', 1),
(N'PasswordPolicy.RequireUpper.Text', N'en-US', N'At least 1 uppercase letter (A–Z).', N'PasswordPolicy', 1),

-- Password Policy : Require Lowercase
(N'PasswordPolicy.RequireLower.Text', N'zh-TW', N'至少包含 1 個英文小寫字母（a–z）。', N'PasswordPolicy', 1),
(N'PasswordPolicy.RequireLower.Text', N'en-US', N'At least 1 lowercase letter (a–z).', N'PasswordPolicy', 1),

-- Password Policy : Require Digit
(N'PasswordPolicy.RequireDigit.Text', N'zh-TW', N'至少包含 1 個數字（0–9）。', N'PasswordPolicy', 1),
(N'PasswordPolicy.RequireDigit.Text', N'en-US', N'At least 1 digit (0–9).', N'PasswordPolicy', 1),

-- Password Policy : Require Special (Prefix + Suffix)
(N'PasswordPolicy.RequireSpecial.Prefix', N'zh-TW', N'至少包含 1 個特殊符號（允許符號：', N'PasswordPolicy', 1),
(N'PasswordPolicy.RequireSpecial.Prefix', N'en-US', N'At least 1 special character (allowed:', N'PasswordPolicy', 1),

(N'PasswordPolicy.RequireSpecial.Suffix', N'zh-TW', N'）。', N'PasswordPolicy', 1),
(N'PasswordPolicy.RequireSpecial.Suffix', N'en-US', N').', N'PasswordPolicy', 1),

(N'PasswordPolicy.ExpiredFirstLogin', N'zh-TW', N'密碼已過期且為首次登入，請先變更密碼後再使用系統。', N'PasswordPolicy', 1),
(N'PasswordPolicy.ExpiredFirstLogin', N'en-US', N'Password expired and first login. Please change password to proceed.', N'PasswordPolicy', 1),

(N'PasswordPolicy.FirstLogin', N'zh-TW', N'首次登入必須先變更密碼，請先完成密碼變更。', N'PasswordPolicy', 1),
(N'PasswordPolicy.FirstLogin', N'en-US', N'First login requires password change. Please complete password change first.', N'PasswordPolicy', 1),

(N'PasswordPolicy.Expired', N'zh-TW', N'您的密碼已超過使用期限，請先變更密碼後再使用系統。', N'PasswordPolicy', 1),
(N'PasswordPolicy.Expired', N'en-US', N'Your password has expired. Please change it to proceed.', N'PasswordPolicy', 1),

(N'PasswordPolicy.PolicyRequired', N'zh-TW', N'目前安全策略要求您必須先變更密碼，才可繼續使用系統。', N'PasswordPolicy', 1),
(N'PasswordPolicy.PolicyRequired', N'en-US', N'Security policy requires you to change your password to proceed.', N'PasswordPolicy', 1),


-- ===== 頁面分頁 =====
(N'Pager.First', N'zh-TW', N'首頁', N'Pager', 1),
(N'Pager.First', N'en-US', N'First', N'Pager', 1),

(N'Pager.Previous', N'zh-TW', N'前一頁', N'Pager', 1),
(N'Pager.Previous', N'en-US', N'Previous', N'Pager', 1),

(N'Pager.Next', N'zh-TW', N'下一頁', N'Pager', 1),
(N'Pager.Next', N'en-US', N'Next', N'Pager', 1),

(N'Pager.Last', N'zh-TW', N'末頁', N'Pager', 1),
(N'Pager.Last', N'en-US', N'Last', N'Pager', 1),

(N'Pager.Total.Prefix', N'zh-TW', N'總共 ', N'Pager', 1),
(N'Pager.Total.Prefix', N'en-US', N'Total ', N'Pager', 1),

(N'Pager.Total.Suffix', N'zh-TW', N' 筆，', N'Pager', 1),
(N'Pager.Total.Suffix', N'en-US', N' items, ', N'Pager', 1),

(N'Pager.PageSize.Label', N'zh-TW', N'每頁顯示', N'Pager', 1),
(N'Pager.PageSize.Label', N'en-US', N'Per page', N'Pager', 1),

(N'Pager.Items', N'zh-TW', N'筆', N'Pager', 1),
(N'Pager.Items', N'en-US', N'items', N'Pager', 1),


-- ===== 資料驗證 =====
-- 字串長度驗證
(N'Validation.StringLength', N'zh-TW', N'{0} 最多 {1} 字元', N'Validation', 1),
(N'Validation.StringLength', N'en-US', N'{0} must be at most {1} characters long.', N'Validation', 1),

-- ===== 資料庫欄位文字 =====
-- 動作
(N'AppAction.AppActionId',   N'zh-TW', N'動作編號', N'AppAction', 1),
(N'AppAction.AppActionId',   N'en-US', N'Action ID', N'AppAction', 1),

(N'AppAction.AppActionCode', N'zh-TW', N'動作代碼', N'AppAction', 1),
(N'AppAction.AppActionCode', N'en-US', N'Action code', N'AppAction', 1),

(N'AppAction.AppActionDisplayName', N'zh-TW', N'顯示名稱', N'AppAction', 1),
(N'AppAction.AppActionDisplayName', N'en-US', N'Display name', N'AppAction', 1),

(N'AppAction.AppActionOrder', N'zh-TW', N'顯示順序', N'AppAction', 1),
(N'AppAction.AppActionOrder', N'en-US', N'Display order', N'AppAction', 1),

-- 部門
(N'Department.DepartmentId',       N'zh-TW', N'部門編號', N'Department', 1),
(N'Department.DepartmentId',       N'en-US', N'Department ID', N'Department', 1),

(N'Department.DepartmentCode',     N'zh-TW', N'部門代碼', N'Department', 1),
(N'Department.DepartmentCode',     N'en-US', N'Department code', N'Department', 1),

(N'Department.DepartmentName',     N'zh-TW', N'部門名稱', N'Department', 1),
(N'Department.DepartmentName',     N'en-US', N'Department name', N'Department', 1),

(N'Department.DepartmentParentId', N'zh-TW', N'上層部門', N'Department', 1),
(N'Department.DepartmentParentId', N'en-US', N'Parent department', N'Department', 1),

(N'Department.DepartmentIsActive', N'zh-TW', N'是否啟用', N'Department', 1),
(N'Department.DepartmentIsActive', N'en-US', N'Active', N'Department', 1),

-- 選單
(N'MenuItem.MenuItemId',           N'zh-TW', N'選單編號',   N'MenuItem', 1),
(N'MenuItem.MenuItemId',           N'en-US', N'Menu ID',    N'MenuItem', 1),

(N'MenuItem.MenuItemParentId',     N'zh-TW', N'上層選單',   N'MenuItem', 1),
(N'MenuItem.MenuItemParentId',     N'en-US', N'Parent menu',N'MenuItem', 1),

(N'MenuItem.MenuItemTitle',        N'zh-TW', N'選單標題',   N'MenuItem', 1),
(N'MenuItem.MenuItemTitle',        N'en-US', N'Menu title', N'MenuItem', 1),

(N'MenuItem.MenuItemIcon',         N'zh-TW', N'圖示',       N'MenuItem', 1),
(N'MenuItem.MenuItemIcon',         N'en-US', N'Icon',       N'MenuItem', 1),

(N'MenuItem.ResourceKey',          N'zh-TW', N'資源代碼',   N'MenuItem', 1),
(N'MenuItem.ResourceKey',          N'en-US', N'Resource key', N'MenuItem', 1),

(N'MenuItem.MenuItemDisplayOrder', N'zh-TW', N'顯示順序',   N'MenuItem', 1),
(N'MenuItem.MenuItemDisplayOrder', N'en-US', N'Display order', N'MenuItem', 1),

(N'MenuItem.MenuItemIsActive',     N'zh-TW', N'是否啟用',   N'MenuItem', 1),
(N'MenuItem.MenuItemIsActive',     N'en-US', N'Active',     N'MenuItem', 1),

(N'MenuItem.ResourceId',           N'zh-TW', N'連結',       N'MenuItem', 1),
(N'MenuItem.ResourceId',           N'en-US', N'Link',       N'MenuItem', 1),

-- 參數
(N'Parameter.ParameterId', N'zh-TW', N'參數編號', N'Parameter', 1),
(N'Parameter.ParameterId', N'en-US', N'Parameter ID', N'Parameter', 1),

(N'Parameter.ParameterCode', N'zh-TW', N'參數代碼', N'Parameter', 1),
(N'Parameter.ParameterCode', N'en-US', N'Parameter Code', N'Parameter', 1),

(N'Parameter.ParameterName', N'zh-TW', N'參數名稱', N'Parameter', 1),
(N'Parameter.ParameterName', N'en-US', N'Parameter Name', N'Parameter', 1),

(N'Parameter.ParameterValue', N'zh-TW', N'參數值', N'Parameter', 1),
(N'Parameter.ParameterValue', N'en-US', N'Parameter Value', N'Parameter', 1),

(N'Parameter.ParameterFormat', N'zh-TW', N'參數格式', N'Parameter', 1),
(N'Parameter.ParameterFormat', N'en-US', N'Parameter Format', N'Parameter', 1),

(N'Parameter.ParameterIsActive', N'zh-TW', N'是否啟用', N'Parameter', 1),
(N'Parameter.ParameterIsActive', N'en-US', N'Active', N'Parameter', 1),

-- 資源
(N'Resource.ResourceId', N'zh-TW', N'資源編號', N'Resource', 1),
(N'Resource.ResourceId', N'en-US', N'Resource ID', N'Resource', 1),

(N'Resource.ResourceType', N'zh-TW', N'資源類型', N'Resource', 1),
(N'Resource.ResourceType', N'en-US', N'Resource type', N'Resource', 1),

(N'Resource.ResourceKey', N'zh-TW', N'資源代碼', N'Resource', 1),
(N'Resource.ResourceKey', N'en-US', N'Resource key', N'Resource', 1),

(N'Resource.ResourceDisplayName', N'zh-TW', N'資源名稱', N'Resource', 1),
(N'Resource.ResourceDisplayName', N'en-US', N'Display name', N'Resource', 1),

(N'Resource.ResourceIsActive', N'zh-TW', N'是否啟用', N'Resource', 1),
(N'Resource.ResourceIsActive', N'en-US', N'Active', N'Resource', 1),

-- 角色
(N'Role.RoleId', N'zh-TW', N'角色編號', N'Role', 1),
(N'Role.RoleId', N'en-US', N'Role ID', N'Role', 1),

(N'Role.RoleGroup', N'zh-TW', N'角色群組', N'Role', 1),
(N'Role.RoleGroup', N'en-US', N'Role group', N'Role', 1),

(N'Role.RoleCode', N'zh-TW', N'角色代碼', N'Role', 1),
(N'Role.RoleCode', N'en-US', N'Role code', N'Role', 1),

-- 角色權限
(N'RolePermission.RoleId',     N'zh-TW', N'角色編號', N'RolePermission', 1),
(N'RolePermission.RoleId',     N'en-US', N'Role ID',  N'RolePermission', 1),

(N'RolePermission.ResourceId', N'zh-TW', N'資源編號', N'RolePermission', 1),
(N'RolePermission.ResourceId', N'en-US', N'Resource ID', N'RolePermission', 1),

(N'RolePermission.AppActionId',N'zh-TW', N'動作編號', N'RolePermission', 1),
(N'RolePermission.AppActionId',N'en-US', N'Action ID', N'RolePermission', 1),

-- 使用者
(N'User.UserId',                N'zh-TW', N'使用者編號',         N'User', 1),
(N'User.UserId',                N'en-US', N'User ID',            N'User', 1),

(N'User.UserAccount',           N'zh-TW', N'帳號',              N'User', 1),
(N'User.UserAccount',           N'en-US', N'Account',           N'User', 1),

(N'User.UserPasswordHash',      N'zh-TW', N'密碼',               N'User', 1),
(N'User.UserPasswordHash',      N'en-US', N'Password',           N'User', 1),

(N'User.UserCurrentPassword',   N'zh-TW', N'目前密碼',      N'User', 1),
(N'User.UserCurrentPassword',   N'en-US', N'Current Password',   N'User', 1),

(N'User.UserConfirmPasswordHash',   N'zh-TW', N'確認密碼',      N'User', 1),
(N'User.UserConfirmPasswordHash',   N'en-US', N'Confirm Password',   N'User', 1),

(N'User.UserNewPasswordHash',   N'zh-TW', N'新密碼',      N'User', 1),
(N'User.UserNewPasswordHash',   N'en-US', N'New Password',   N'User', 1),

(N'User.UserFullName',          N'zh-TW', N'姓名',               N'User', 1),
(N'User.UserFullName',          N'en-US', N'Full name',          N'User', 1),

(N'User.UserJobTitle',          N'zh-TW', N'職稱',               N'User', 1),
(N'User.UserJobTitle',          N'en-US', N'Job title',          N'User', 1),

(N'User.UserEmail',             N'zh-TW', N'電子郵件',           N'User', 1),
(N'User.UserEmail',             N'en-US', N'Email',              N'User', 1),

(N'User.UserPhone',             N'zh-TW', N'聯絡電話',           N'User', 1),
(N'User.UserPhone',             N'en-US', N'Phone',              N'User', 1),

(N'User.UserMobile',            N'zh-TW', N'手機',               N'User', 1),
(N'User.UserMobile',            N'en-US', N'Mobile',             N'User', 1),

(N'User.UserIsActive',          N'zh-TW', N'是否啟用',           N'User', 1),
(N'User.UserIsActive',          N'en-US', N'Active',             N'User', 1),

(N'User.UserIsLocked',          N'zh-TW', N'是否鎖定',           N'User', 1),
(N'User.UserIsLocked',          N'en-US', N'Locked',             N'User', 1),

(N'User.UserLockedUntil',       N'zh-TW', N'鎖定解除時間',       N'User', 1),
(N'User.UserLockedUntil',       N'en-US', N'Lockout end time',   N'User', 1),

(N'User.UserLoginFailedCount',  N'zh-TW', N'登入失敗次數',       N'User', 1),
(N'User.UserLoginFailedCount',  N'en-US', N'Failed login count', N'User', 1),

(N'User.UserLastLoginAt',       N'zh-TW', N'最後登入時間',       N'User', 1),
(N'User.UserLastLoginAt',       N'en-US', N'Last login time',    N'User', 1),

(N'User.UserLastLoginIp',       N'zh-TW', N'最後登入 IP',        N'User', 1),
(N'User.UserLastLoginIp',       N'en-US', N'Last login IP',      N'User', 1),

(N'User.UserPasswordChangedAt', N'zh-TW', N'密碼最後修改時間',   N'User', 1),
(N'User.UserPasswordChangedAt', N'en-US', N'Password changed at',N'User', 1),

(N'User.UserStatus',            N'zh-TW', N'帳號狀態',           N'User', 1),
(N'User.UserStatus',            N'en-US', N'Account status',     N'User', 1),

(N'User.UserRemarks',           N'zh-TW', N'備註',               N'User', 1),
(N'User.UserRemarks',           N'en-US', N'Remarks',            N'User', 1),

(N'User.UserTotpSecret',        N'zh-TW', N'綁定TOTP',           N'User', 1),
(N'User.UserTotpSecret',        N'en-US', N'TOTP binding',       N'User', 1),

(N'User.DepartmentId',          N'zh-TW', N'部門',               N'User', 1),
(N'User.DepartmentId',          N'en-US', N'Department',         N'User', 1),

(N'User.RoleCodeList',          N'zh-TW', N'強制指定系統角色',    N'User', 1),
(N'User.RoleCodeList',          N'en-US', N'Assigned system roles', N'User', 1),

(N'User.UserGroupList',         N'zh-TW', N'使用者群組清單',      N'User', 1),
(N'User.UserGroupList',         N'en-US', N'User group list',     N'User', 1),

(N'User.UserGroupRoleList',     N'zh-TW', N'使用者群組權限清單',  N'User', 1),
(N'User.UserGroupRoleList',     N'en-US', N'User group permissions', N'User', 1),

-- 使用者群組
(N'UserGroup.UserGroupId',          N'zh-TW', N'群組編號',     N'UserGroup', 1),
(N'UserGroup.UserGroupId',          N'en-US', N'Group ID',     N'UserGroup', 1),

(N'UserGroup.UserGroupCode',        N'zh-TW', N'群組名稱',     N'UserGroup', 1),
(N'UserGroup.UserGroupCode',        N'en-US', N'Group Name',   N'UserGroup', 1),

(N'UserGroup.UserGroupDescription', N'zh-TW', N'群組說明',     N'UserGroup', 1),
(N'UserGroup.UserGroupDescription', N'en-US', N'Description', N'UserGroup', 1),

-- 使用者群組成員
(N'UserGroupMember.UserGroupId', N'zh-TW', N'群組編號',   N'UserGroupMember', 1),
(N'UserGroupMember.UserGroupId', N'en-US', N'Group ID',   N'UserGroupMember', 1),

(N'UserGroupMember.UserId',      N'zh-TW', N'使用者編號', N'UserGroupMember', 1),
(N'UserGroupMember.UserId',      N'en-US', N'User ID',    N'UserGroupMember', 1),

-- 使用者群組角色
(N'UserGroupRole.UserGroupId', N'zh-TW', N'群組編號', N'UserGroupRole', 1),
(N'UserGroupRole.UserGroupId', N'en-US', N'Group ID', N'UserGroupRole', 1),

(N'UserGroupRole.RoleId',      N'zh-TW', N'角色編號', N'UserGroupRole', 1),
(N'UserGroupRole.RoleId',      N'en-US', N'Role ID',  N'UserGroupRole', 1),

-- 多語系
(N'LocalizationString.LocalizationStringKey',       N'zh-TW', N'代號',     N'LocalizationString', 1),
(N'LocalizationString.LocalizationStringKey',       N'en-US', N'Localization Key', N'LocalizationString', 1),

(N'LocalizationString.LocalizationStringCulture',   N'zh-TW', N'語系',         N'LocalizationString', 1),
(N'LocalizationString.LocalizationStringCulture',   N'en-US', N'Culture',          N'LocalizationString', 1),

(N'LocalizationString.LocalizationStringValue',     N'zh-TW', N'顯示文字',     N'LocalizationString', 1),
(N'LocalizationString.LocalizationStringValue',     N'en-US', N'Display Text',     N'LocalizationString', 1),

(N'LocalizationString.LocalizationStringCategory',  N'zh-TW', N'分類',         N'LocalizationString', 1),
(N'LocalizationString.LocalizationStringCategory',  N'en-US', N'Category',         N'LocalizationString', 1),

(N'LocalizationString.LocalizationStringIsActive',  N'zh-TW', N'是否啟用',     N'LocalizationString', 1),
(N'LocalizationString.LocalizationStringIsActive',  N'en-US', N'Is Active',        N'LocalizationString', 1),

-- ===== 各頁面文字 =====
-- 帳號管理
-- Index
(N'AccountSettings.Index.Title', N'zh-TW', N'帳號管理', N'AccountSettings', 1),
(N'AccountSettings.Index.Title', N'en-US', N'Account Management', N'AccountSettings', 1),

-- ChangePassword



-- Create
(N'AccountSettings.Create.Title', N'zh-TW', N'帳號設定-新增', N'AccountSettings', 1),
(N'AccountSettings.Create.Title', N'en-US', N'Account Settings - Create', N'AccountSettings', 1),

(N'User.UserJobTitle.Placeholder',     N'zh-TW', N'例：工程師、經理',          N'User', 1),
(N'User.UserJobTitle.Placeholder',     N'en-US', N'e.g., Engineer, Manager',   N'User', 1),

(N'User.UserPhone.Placeholder',        N'zh-TW', N'市話（選填）',              N'User', 1),
(N'User.UserPhone.Placeholder',        N'en-US', N'Phone (optional)',          N'User', 1),

(N'User.UserMobile.Placeholder',       N'zh-TW', N'手機（選填）',              N'User', 1),
(N'User.UserMobile.Placeholder',       N'en-US', N'Mobile (optional)',         N'User', 1),

(N'User.UserRemarks.Placeholder',      N'zh-TW', N'備註（選填）',              N'User', 1),
(N'User.UserRemarks.Placeholder',      N'en-US', N'Remarks (optional)',        N'User', 1),

-- Edit
(N'AccountSettings.Edit.Title', N'zh-TW', N'帳號設定-編輯', N'AccountSettings', 1),
(N'AccountSettings.Edit.Title', N'en-US', N'Account Settings - Edit', N'AccountSettings', 1),

-- ResetPassword
(N'AccountSettings.ResetPassword.Title', N'zh-TW', N'帳號設定-密碼重設', N'AccountSettings', 1),
(N'AccountSettings.ResetPassword.Title', N'en-US', N'Account Settings - Reset Password', N'AccountSettings', 1),

(N'AccountSettings.ResetPassword.Shortcut.Prefix', N'zh-TW', N'快速重設密碼為「Abcd + ', N'AccountSettings', 1),
(N'AccountSettings.ResetPassword.Shortcut.Prefix', N'en-US', N'Quickly reset password to "Abcd + ', N'AccountSettings', 1),
(N'AccountSettings.ResetPassword.Shortcut.Suffix', N'zh-TW', N'」', N'AccountSettings', 1),
(N'AccountSettings.ResetPassword.Shortcut.Suffix', N'en-US', N'"', N'AccountSettings', 1),

(N'AccountSettings.ResetPassword.Shortcut.Button', N'zh-TW', N'快速設定', N'AccountSettings', 1),
(N'AccountSettings.ResetPassword.Shortcut.Button', N'en-US', N'Quick Set', N'AccountSettings', 1),

(N'AccountSettings.ResetPassword.Hint', N'zh-TW', N'*該功能不受密碼政策限制，但建議密碼長度8個字元以上', N'AccountSettings', 1),
(N'AccountSettings.ResetPassword.Hint', N'en-US', N'*This function is not restricted by password policy, but an 8+ character password is recommended', N'AccountSettings', 1),

(N'AccountSettings.ResetPassword.ValidationFailed', N'zh-TW', N'密碼重設【失敗】，必填資料未填寫或未符合密碼政策', N'AccountSettings', 1),
(N'AccountSettings.ResetPassword.ValidationFailed', N'en-US', N'Password reset [Failed]. Missing fields or policy violation.', N'AccountSettings', 1),

--ChangePassword
(N'AccountSettings.ChangePassword.Title', N'zh-TW', N'變更密碼', N'AccountSettings', 1),
(N'AccountSettings.ChangePassword.Title', N'en-US', N'Change Password', N'AccountSettings', 1),

(N'AccountSettings.Password.WrongCurrent', N'zh-TW', N'原密碼錯誤', N'AccountSettings', 1),
(N'AccountSettings.Password.WrongCurrent', N'en-US', N'Incorrect current password', N'AccountSettings', 1),

(N'AccountSettings.Password.HistoryLimit.Prefix', N'zh-TW', N'新密碼不可與前 ', N'AccountSettings', 1),
(N'AccountSettings.Password.HistoryLimit.Prefix', N'en-US', N'New password cannot be the same as the previous ', N'AccountSettings', 1),

(N'AccountSettings.Password.HistoryLimit.Suffix', N'zh-TW', N' 次密碼相同。', N'AccountSettings', 1),
(N'AccountSettings.Password.HistoryLimit.Suffix', N'en-US', N' passwords.', N'AccountSettings', 1),

-- Details
(N'AccountSettings.Details.Title', N'zh-TW', N'帳號設定 使用者明細', N'AccountSettings', 1),
(N'AccountSettings.Details.Title', N'en-US', N'Account Settings User Details', N'AccountSettings', 1),

(N'UserDetails.Section1.Title', N'zh-TW', N'一 使用者基本資料', N'UserDetails', 1),
(N'UserDetails.Section1.Title', N'en-US', N'1 User Basic Information', N'UserDetails', 1),

(N'UserDetails.Section2.Title', N'zh-TW', N'二 使用者群組 角色與權限', N'UserDetails', 1),
(N'UserDetails.Section2.Title', N'en-US', N'2 User Groups Roles and Permissions', N'UserDetails', 1),

(N'UserDetails.EffectiveRoles', N'zh-TW', N'有效角色', N'UserDetails', 1),
(N'UserDetails.EffectiveRoles', N'en-US', N'Effective roles', N'UserDetails', 1),

(N'UserDetails.EffectivePermissions', N'zh-TW', N'有效權限', N'UserDetails', 1),
(N'UserDetails.EffectivePermissions', N'en-US', N'Effective permissions', N'UserDetails', 1),

(N'UserDetails.ActionTooltip.Prefix', N'zh-TW', N'動作', N'UserDetails', 1),
(N'UserDetails.ActionTooltip.Prefix', N'en-US', N'Action', N'UserDetails', 1),

-- 註冊TOTP
(N'Totp.Setup.Title', N'zh-TW', N'註冊 TOTP 兩步驟驗證', N'Totp', 1),
(N'Totp.Setup.Title', N'en-US', N'Register TOTP Two - Factor Authentication', N'Totp', 1),

-- 註冊TOTP-已啟用警告
(N'Totp.AlreadyEnabled.Warning', N'zh-TW', N'您的帳號已啟用 TOTP 驗證。重新註冊將會覆蓋原本的設定，請謹慎操作。', N'Totp', 1),
(N'Totp.AlreadyEnabled.Warning', N'en-US', N'Your account already has TOTP enabled. Re-registering will overwrite the existing setup. Please proceed with caution.', N'Totp', 1),

(N'Totp.ReRegister.Confirm', N'zh-TW', N'確定重新註冊', N'Totp', 1),
(N'Totp.ReRegister.Confirm', N'en-US', N'Confirm Re-register', N'Totp', 1),

-- 註冊TOTP-操作步驟說明
(N'Totp.Step.InstallApp', N'zh-TW', N'請先在手機安裝 Google Authenticator 或其他支援 TOTP 的驗證器 App。', N'Totp', 1),
(N'Totp.Step.InstallApp', N'en-US', N'Install Google Authenticator or any TOTP-compatible authenticator app on your phone.', N'Totp', 1),

(N'Totp.Step.ChromeExtension', N'zh-TW', N'Chrome 瀏覽器的外掛套件：', N'Totp', 1),
(N'Totp.Step.ChromeExtension', N'en-US', N'Chrome browser extension:', N'Totp', 1),

(N'Totp.Step.ScanOrInput', N'zh-TW', N'使用【驗證器】掃描下方的 QR Code，或手動輸入「金鑰」。', N'Totp', 1),
(N'Totp.Step.ScanOrInput', N'en-US', N'Scan the QR code below with your authenticator app, or manually enter the secret key.', N'Totp', 1),

(N'Totp.Step.InputCode', N'zh-TW', N'【驗證器】會顯示 6 位數驗證碼，請在下方欄位輸入後，按下「啟用 TOTP」。', N'Totp', 1),
(N'Totp.Step.InputCode', N'en-US', N'Your authenticator app will show a 6-digit code. Enter it below and click “Enable TOTP”.', N'Totp', 1),

-- 註冊TOTP-方式標題
(N'Totp.Method.QrCode', N'zh-TW', N'方式一：掃描 QR Code', N'Totp', 1),
(N'Totp.Method.QrCode', N'en-US', N'Method 1: Scan QR Code', N'Totp', 1),

(N'Totp.Method.Manual', N'zh-TW', N'方式二：手動輸入金鑰（若無法掃描）', N'Totp', 1),
(N'Totp.Method.Manual', N'en-US', N'Method 2: Enter Secret Key Manually (if you cannot scan)', N'Totp', 1),

-- 註冊TOTP-欄位顯示(對應ViewModel)
(N'Totp.Issuer', N'zh-TW', N'發行者', N'Totp', 1),
(N'Totp.Issuer', N'en-US', N'Issuer', N'Totp', 1),

(N'Totp.Secret', N'zh-TW', N'金鑰', N'Totp', 1),
(N'Totp.Secret', N'en-US', N'Secret', N'Totp', 1),

(N'Totp.Code', N'zh-TW', N'驗證碼', N'Totp', 1),
(N'Totp.Code', N'en-US', N'Authentication Code', N'Totp', 1),

-- 註冊TOTP-驗證碼 Placeholder
(N'Totp.Code.Placeholder', N'zh-TW', N'請輸入【驗證器】顯示的 6 位數驗證碼', N'Totp', 1),
(N'Totp.Code.Placeholder', N'en-US', N'Enter the 6-digit code from your authenticator app', N'Totp', 1),

-- 註冊TOTP-Secret、警告文字
(N'Totp.Secret.Warning', N'zh-TW', N'建議只在必要時手動輸入，並避免洩漏此金鑰。', N'Totp', 1),
(N'Totp.Secret.Warning', N'en-US', N'Only enter this manually when necessary and keep the secret secure.', N'Totp', 1),

(N'Totp.SessionExpired', N'zh-TW', N'TOTP 註冊流程已失效，請重新開始。', N'AccountSettings', 1),
(N'Totp.SessionExpired', N'en-US', N'TOTP setup session expired. Please restart.', N'AccountSettings', 1),

(N'Totp.VerifyFailed', N'zh-TW', N'驗證碼錯誤或已過期，請再次確認手機 App 顯示的數字。', N'AccountSettings', 1),
(N'Totp.VerifyFailed', N'en-US', N'Verification code incorrect or expired. Please check your authenticator app.', N'AccountSettings', 1),

(N'Totp.SetupSuccess', N'zh-TW', N'TOTP 兩步驟驗證已啟用，請妥善保管您的驗證器 App。', N'AccountSettings', 1),
(N'Totp.SetupSuccess', N'en-US', N'TOTP 2FA enabled. Please keep your authenticator app safe.', N'AccountSettings', 1),


-- 編輯使用者群組
(N'AccountSettings.EditGroup.Title', N'zh-TW', N'帳號設定-編輯使用者群組', N'AccountSettings', 1),
(N'AccountSettings.EditGroup.Title', N'en-US', N'Account Settings - Edit User Groups', N'AccountSettings', 1),

(N'AccountSettings.EditGroup.AllGroups', N'zh-TW', N'所有可選群組', N'AccountSettings', 1),
(N'AccountSettings.EditGroup.AllGroups', N'en-US', N'Available groups', N'AccountSettings', 1),

(N'AccountSettings.EditGroup.SelectedGroups', N'zh-TW', N'已指派群組', N'AccountSettings', 1),
(N'AccountSettings.EditGroup.SelectedGroups', N'en-US', N'Assigned groups', N'AccountSettings', 1),

(N'AccountSettings.EditGroup.Note', N'zh-TW', N'備註：預覽權限後，若確認無誤請務必點選「儲存」按鈕。', N'AccountSettings', 1),
(N'AccountSettings.EditGroup.Note', N'en-US', N'Note: After previewing permissions, please click “Save” to apply changes.', N'AccountSettings', 1),


-- 系統動作管理
-- Index
(N'AppAction.Index.Title', N'zh-TW', N'系統動作管理', N'AppAction', 1),
(N'AppAction.Index.Title', N'en-US', N'System Action Management', N'AppAction', 1),

(N'AppAction.AppActionCode.Placeholder', N'zh-TW', N'例如：Index / Create / Edit / Delete', N'AppAction', 1),
(N'AppAction.AppActionCode.Placeholder', N'en-US', N'e.g., Index / Create / Edit / Delete', N'AppAction', 1),

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

(N'AppAction.Delete.InUse.Prefix', N'zh-TW', N'系統動作-', N'AppAction', 1),
(N'AppAction.Delete.InUse.Prefix', N'en-US', N'App Action - ', N'AppAction', 1),
(N'AppAction.Delete.InUse.Suffix', N'zh-TW', N' 目前仍被角色權限使用，無法刪除。', N'AppAction', 1),
(N'AppAction.Delete.InUse.Suffix', N'en-US', N' is still used by role permissions and cannot be deleted.', N'AppAction', 1),

(N'AppAction.Delete.Blocked.CannotDelete', N'zh-TW', N'無法刪除', N'AppAction', 1),
(N'AppAction.Delete.Blocked.CannotDelete', N'en-US', N'cannot be deleted', N'AppAction', 1),

(N'AppAction.Delete.Blocked.Instruction', N'zh-TW', N'若要刪除，請先在「角色管理-權限設定」中，取消所有使用此動作的權限設定。', N'AppAction', 1),
(N'AppAction.Delete.Blocked.Instruction', N'en-US', N'To delete it, please remove all permissions that use this action in Role Management - Permission Settings.', N'AppAction', 1),


-- 首頁
(N'Home.Welcome.Title', N'zh-TW', N'歡迎使用', N'Home', 1),
(N'Home.Welcome.Title', N'en-US', N'Welcome', N'Home', 1),

(N'Home.Welcome.SubTitle', N'zh-TW', N'文管與電子採購系統（範例）', N'Home', 1),
(N'Home.Welcome.SubTitle', N'en-US', N'Document Control & E-Procurement System (Demo)', N'Home', 1),


-- 登入頁
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

(N'Login.Captcha.Invalid', N'zh-TW', N'圖形驗證碼錯誤，請重新輸入。', N'Captcha', 1),
(N'Login.Captcha.Invalid', N'en-US', N'Incorrect captcha. Please try again.', N'Captcha', 1),



-- 登入頁-兩階段驗證
(N'Login.TwoFactor.Title', N'zh-TW', N'兩步驟驗證', N'Login', 1),
(N'Login.TwoFactor.Title', N'en-US', N'Two-step verification', N'Login', 1),

(N'Login.TwoFactor.NoProviderEnabled', N'zh-TW', N'目前未啟用任何兩步驟驗證方式，請重新登入或洽系統管理者。', N'Login', 1),
(N'Login.TwoFactor.NoProviderEnabled', N'en-US', N'No two-step verification method is enabled. Please log in again or contact the system administrator.', N'Login', 1),

(N'Login.TwoFactor.Provider.Label', N'zh-TW', N'驗證方式', N'Login', 1),
(N'Login.TwoFactor.Provider.Label', N'en-US', N'Verification method', N'Login', 1),

(N'Login.TwoFactor.Provider.Email', N'zh-TW', N'Email 驗證碼', N'Login', 1),
(N'Login.TwoFactor.Provider.Email', N'en-US', N'Email code', N'Login', 1),

(N'Login.TwoFactor.Provider.Totp', N'zh-TW', N'TOTP 驗證碼', N'Login', 1),
(N'Login.TwoFactor.Provider.Totp', N'en-US', N'TOTP code', N'Login', 1),

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

(N'Login.TwoFactor.SessionExpiredReLogin', N'zh-TW', N'二階段驗證已失效，請重新登入。', N'Login', 1),
(N'Login.TwoFactor.SessionExpiredReLogin', N'en-US', N'Two-factor verification has expired. Please sign in again.', N'Login', 1),

(N'Login.TwoFactor.SelectProvider', N'zh-TW', N'請選擇驗證方式。', N'Login', 1),
(N'Login.TwoFactor.SelectProvider', N'en-US', N'Please select a verification method.', N'Login', 1),

(N'Login.TwoFactor.EnterCode', N'zh-TW', N'請輸入驗證碼。', N'Login', 1),
(N'Login.TwoFactor.EnterCode', N'en-US', N'Please enter the verification code.', N'Login', 1),

(N'Login.TwoFactor.InvalidProvider', N'zh-TW', N'驗證方式不正確，請重新選擇。', N'Login', 1),
(N'Login.TwoFactor.InvalidProvider', N'en-US', N'Invalid verification method. Please select again.', N'Login', 1),

(N'Login.TwoFactor.EmailNotEnabled', N'zh-TW', N'目前未啟用 Email 二階段驗證。', N'Login', 1),
(N'Login.TwoFactor.EmailNotEnabled', N'en-US', N'Email two-factor verification is not enabled.', N'Login', 1),

(N'Login.TwoFactor.TotpNotEnabled', N'zh-TW', N'目前未啟用 TOTP 二階段驗證。', N'Login', 1),
(N'Login.TwoFactor.TotpNotEnabled', N'en-US', N'TOTP two-factor verification is not enabled.', N'Login', 1),

(N'Login.TwoFactor.CodeExpiredResend', N'zh-TW', N'驗證碼已過期，請重新取得。', N'Login', 1),
(N'Login.TwoFactor.CodeExpiredResend', N'en-US', N'The code has expired. Please request a new one.', N'Login', 1),

(N'Login.TwoFactor.TooManyAttemptsReLogin', N'zh-TW', N'驗證失敗次數過多，請重新登入。', N'Login', 1),
(N'Login.TwoFactor.TooManyAttemptsReLogin', N'en-US', N'Too many failed attempts. Please sign in again.', N'Login', 1),

(N'Login.TwoFactor.CodeInvalidRetry', N'zh-TW', N'驗證碼錯誤，請重新輸入。', N'Login', 1),
(N'Login.TwoFactor.CodeInvalidRetry', N'en-US', N'Incorrect code. Please try again.', N'Login', 1),

(N'Login.TwoFactor.CodeInvalidOrExpiredRetry', N'zh-TW', N'驗證碼錯誤或已過期，請重新輸入。', N'Login', 1),
(N'Login.TwoFactor.CodeInvalidOrExpiredRetry', N'en-US', N'The code is incorrect or expired. Please try again.', N'Login', 1),

(N'Login.TwoFactor.EmailSentCheckInbox', N'zh-TW', N'已寄出 Email 驗證碼，請至信箱收信。', N'Login', 1),
(N'Login.TwoFactor.EmailSentCheckInbox', N'en-US', N'The email verification code has been sent. Please check your inbox.', N'Login', 1),


-- 選單
-- Index
(N'MenuItem.Index.Title',                 N'zh-TW', N'選單項目管理', N'MenuItem', 1),
(N'MenuItem.Index.Title',                 N'en-US', N'Menu Item Management', N'MenuItem', 1),

(N'MenuItem.MenuItemTitle.Placeholder',   N'zh-TW', N'請輸入選單標題', N'MenuItem', 1),
(N'MenuItem.MenuItemTitle.Placeholder',   N'en-US', N'Enter menu title', N'MenuItem', 1),

(N'MenuItem.ResourceKey.Placeholder',     N'zh-TW', N'例如 /Resource 或 /Home/Index', N'MenuItem', 1),
(N'MenuItem.ResourceKey.Placeholder',     N'en-US', N'e.g. /Resource or /Home/Index', N'MenuItem', 1),

-- Create
(N'MenuItem.Create.Title',                    N'zh-TW', N'選單項目管理 新增',                N'MenuItem', 1),
(N'MenuItem.Create.Title',                    N'en-US', N'Menu Item Management Create',     N'MenuItem', 1),

(N'MenuItem.MenuItemIcon.Placeholder',        N'zh-TW', N'例如 fas fa-home',                 N'MenuItem', 1),
(N'MenuItem.MenuItemIcon.Placeholder',        N'en-US', N'e.g. fas fa-home',                 N'MenuItem', 1),

(N'MenuItem.MenuItemDisplayOrder.Placeholder',N'zh-TW', N'例如 10',                           N'MenuItem', 1),
(N'MenuItem.MenuItemDisplayOrder.Placeholder',N'en-US', N'e.g. 10',                           N'MenuItem', 1),

(N'MenuItem.Icon.SearchTitle',                N'zh-TW', N'查詢 Font Awesome icon',           N'MenuItem', 1),
(N'MenuItem.Icon.SearchTitle',                N'en-US', N'Search Font Awesome icon',         N'MenuItem', 1),

(N'MenuItem.Icon.SearchLink',                 N'zh-TW', N'查詢Icon',                          N'MenuItem', 1),
(N'MenuItem.Icon.SearchLink',                 N'en-US', N'Search icon',                        N'MenuItem', 1),

-- Edit
(N'MenuItem.Edit.Title', N'zh-TW', N'選單項目管理 編輯', N'MenuItem', 1),
(N'MenuItem.Edit.Title', N'en-US', N'Menu Item Management Edit', N'MenuItem', 1),

-- Delete
(N'MenuItem.Delete.Title',                 N'zh-TW', N'選單項目管理 刪除',                 N'MenuItem', 1),
(N'MenuItem.Delete.Title',                 N'en-US', N'Menu Item Management Delete',      N'MenuItem', 1),

(N'MenuItem.Parent',                       N'zh-TW', N'上層選單',                         N'MenuItem', 1),
(N'MenuItem.Parent',                       N'en-US', N'Parent Menu',                      N'MenuItem', 1),

(N'MenuItem.Children.Status',              N'zh-TW', N'子選單狀況',                         N'MenuItem', 1),
(N'MenuItem.Children.Status',              N'en-US', N'Children Status',                   N'MenuItem', 1),

(N'MenuItem.Children.EmptyHint',           N'zh-TW', N'目前沒有任何子選單掛在此選單之下',   N'MenuItem', 1),
(N'MenuItem.Children.EmptyHint',           N'en-US', N'No child menu items under this menu', N'MenuItem', 1),

(N'MenuItem.Children.Count.Prefix',         N'zh-TW', N'此選單底下目前有',                   N'MenuItem', 1),
(N'MenuItem.Children.Count.Prefix',         N'en-US', N'This menu currently has',           N'MenuItem', 1),

(N'MenuItem.Children.Count.Suffix',         N'zh-TW', N'個子選單',                           N'MenuItem', 1),
(N'MenuItem.Children.Count.Suffix',         N'en-US', N'child menu items',                   N'MenuItem', 1),

(N'MenuItem.Delete.BlockedLine1',          N'zh-TW', N'此選單目前仍有子選單，無法刪除',     N'MenuItem', 1),
(N'MenuItem.Delete.BlockedLine1',          N'en-US', N'This menu still has children and cannot be deleted', N'MenuItem', 1),

(N'MenuItem.Delete.BlockedLine2',          N'zh-TW', N'若要刪除，請先將子選單移除或重新指定其上層選單', N'MenuItem', 1),
(N'MenuItem.Delete.BlockedLine2',          N'en-US', N'Remove or reassign the child menu items before deleting', N'MenuItem', 1),


-- Details
(N'MenuItem.Details.Title', N'zh-TW', N'選單項目管理 詳細資料', N'MenuItem', 1),
(N'MenuItem.Details.Title', N'en-US', N'Menu Item Management Details', N'MenuItem', 1),


-- 參數
-- Index
(N'Parameter.Index.Title',       N'zh-TW', N'系統參數管理',   N'Parameter',       1),
(N'Parameter.Index.Title',       N'en-US', N'System Parameters', N'Parameter',     1),

(N'Parameter.ParameterCode.Placeholder', N'zh-TW', N'例如 PWD_MIN_LENGTH', N'Parameter', 1),
(N'Parameter.ParameterCode.Placeholder', N'en-US', N'e.g. PWD_MIN_LENGTH', N'Parameter', 1),

(N'Parameter.ParameterName.Placeholder', N'zh-TW', N'例如 密碼最少長度', N'Parameter', 1),
(N'Parameter.ParameterName.Placeholder', N'en-US', N'e.g. Minimum password length', N'Parameter', 1),

(N'Parameter.ParameterFormat.Placeholder', N'zh-TW', N'text / int / html / json', N'Parameter', 1),
(N'Parameter.ParameterFormat.Placeholder', N'en-US', N'text / int / html / json', N'Parameter', 1),

-- Create
(N'Parameter.Create.Title', N'zh-TW', N'系統參數管理 新增', N'Parameter', 1),
(N'Parameter.Create.Title', N'en-US', N'Parameter Management Create', N'Parameter', 1),

(N'Parameter.ParameterValue.Placeholder', N'zh-TW', N'可輸入文字 / 數字 / HTML / JSON', N'Parameter', 1),
(N'Parameter.ParameterValue.Placeholder', N'en-US', N'Enter text / number / HTML / JSON', N'Parameter', 1),

(N'Parameter.ParameterCode.Duplicate', N'zh-TW', N'參數代碼已存在，請更換。', N'Parameter', 1),
(N'Parameter.ParameterCode.Duplicate', N'en-US', N'Parameter code already exists. Please choose another one.', N'Parameter', 1),

-- Edit
(N'Parameter.Edit.Title', N'zh-TW', N'系統參數管理 編輯', N'Parameter', 1),
(N'Parameter.Edit.Title', N'en-US', N'Parameter Management Edit', N'Parameter', 1),

-- Delete
(N'Parameter.Delete.Title', N'zh-TW', N'系統參數管理 刪除', N'Parameter', 1),
(N'Parameter.Delete.Title', N'en-US', N'Parameter Management Delete', N'Parameter', 1),

-- Details
(N'Parameter.Details.Title', N'zh-TW', N'系統參數管理-詳細資料', N'Parameter', 1),
(N'Parameter.Details.Title', N'en-US', N'Parameter Details',     N'Parameter', 1),


-- 系統資源管理
-- Index
(N'Resource.Index.Title', N'zh-TW', N'系統資源管理', N'Resource', 1),
(N'Resource.Index.Title', N'en-US', N'System Resource Management', N'Resource', 1),

(N'Resource.ResourceType.Placeholder', N'zh-TW', N'例如：PAGE / API', N'Resource', 1),
(N'Resource.ResourceType.Placeholder', N'en-US', N'e.g., PAGE / API', N'Resource', 1),

(N'Resource.ResourceKey.Placeholder', N'zh-TW', N'請輸入資源代碼', N'Resource', 1),
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

(N'Resource.Delete.Blocked.Prefix', N'zh-TW', N'此資源目前仍被角色權限使用，', N'Resource', 1),
(N'Resource.Delete.Blocked.Prefix', N'en-US', N'This resource is still used by role permissions, ', N'Resource', 1),

(N'Resource.Delete.Blocked.CannotDelete', N'zh-TW', N'無法刪除', N'Resource', 1),
(N'Resource.Delete.Blocked.CannotDelete', N'en-US', N'cannot be deleted', N'Resource', 1),

(N'Resource.Delete.Blocked.Instruction', N'zh-TW', N'若要刪除，請先在「角色管理-權限設定」中取消此資源相關的權限設定。', N'Resource', 1),
(N'Resource.Delete.Blocked.Instruction', N'en-US', N'To delete it, please remove the related permission settings under "Role Management - Permission Settings" first.', N'Resource', 1),

-- 角色權限使用狀況(Delete、Details共用)
(N'Resource.RolePermissionUsage.Title', N'zh-TW', N'角色權限使用狀況', N'Resource', 1),
(N'Resource.RolePermissionUsage.Title', N'en-US', N'Role permission usage', N'Resource', 1),

(N'Resource.RolePermissionUsage.None', N'zh-TW', N'目前尚未有任何角色權限使用此資源。', N'Resource', 1),
(N'Resource.RolePermissionUsage.None', N'en-US', N'No role permissions are currently using this resource.', N'Resource', 1),

(N'Resource.RolePermissionUsage.Count.Prefix', N'zh-TW', N'此資源目前被 ', N'Resource', 1),
(N'Resource.RolePermissionUsage.Count.Prefix', N'en-US', N'This resource is currently assigned to ', N'Resource', 1),

(N'Resource.RolePermissionUsage.Count.Suffix', N'zh-TW', N' 個角色設定權限。', N'Resource', 1),
(N'Resource.RolePermissionUsage.Count.Suffix', N'en-US', N' role(s).', N'Resource', 1),

(N'Resource.RolePermissionUsage.RoleGroupListTitle', N'zh-TW', N'使用此資源的角色／群組：', N'Resource', 1),
(N'Resource.RolePermissionUsage.RoleGroupListTitle', N'en-US', N'Roles / groups using this resource', N'Resource', 1),

(N'Resource.UnassignedGroup', N'zh-TW', N'未指定群組', N'Resource', 1),
(N'Resource.UnassignedGroup', N'en-US', N'No group assigned', N'Resource', 1),

(N'Resource.RolePermissionUsage.HasPermissionButNoGroup', N'zh-TW', N'目前已由部分角色設定權限，但尚未指定任何使用者群組。', N'Resource', 1),
(N'Resource.RolePermissionUsage.HasPermissionButNoGroup', N'en-US', N'Permissions are configured for some roles, but no user groups have been assigned yet.', N'Resource', 1),

-- Details
(N'Resource.Details.Title', N'zh-TW', N'系統資源管理 - 詳細資料', N'Resource', 1),
(N'Resource.Details.Title', N'en-US', N'System Resource Management - Details', N'Resource', 1),

-- 角色權限關聯(Delete、Details共用)
(N'AppAction.RolePermissionUsage.Title', N'zh-TW', N'角色權限關聯', N'AppAction', 1),
(N'AppAction.RolePermissionUsage.Title', N'en-US', N'Role permission association', N'AppAction', 1),

(N'AppAction.RolePermissionUsage.None', N'zh-TW', N'目前尚未有任何角色使用此動作。', N'AppAction', 1),
(N'AppAction.RolePermissionUsage.None', N'en-US', N'No roles are currently using this action.', N'AppAction', 1),

(N'AppAction.RolePermissionUsage.Count.Prefix', N'zh-TW', N'此動作目前被 ', N'AppAction', 1),
(N'AppAction.RolePermissionUsage.Count.Prefix', N'en-US', N'This action is currently used by ', N'AppAction', 1),

(N'AppAction.RolePermissionUsage.Count.Suffix', N'zh-TW', N' 組角色使用。', N'AppAction', 1),
(N'AppAction.RolePermissionUsage.Count.Suffix', N'en-US', N' role-permission pair(s).', N'AppAction', 1),

(N'AppAction.RolePermissionUsage.NoDetail', N'zh-TW', N'已有角色權限使用此動作，但無法取得對應的詳細資料。', N'AppAction', 1),
(N'AppAction.RolePermissionUsage.NoDetail', N'en-US', N'Permissions exist for this action, but related details could not be retrieved.', N'AppAction', 1),

-- Details
(N'AppAction.Details.Title', N'zh-TW', N'系統動作管理 - 詳細資料', N'AppAction', 1),
(N'AppAction.Details.Title', N'en-US', N'System Action Management - Details', N'AppAction', 1),


-- 系統角色管理
-- Index
(N'Role.Index.Title',            N'zh-TW', N'角色管理',       N'Role',            1),
(N'Role.Index.Title',            N'en-US', N'Role Management',N'Role',            1),

(N'Role.RoleGroup.Placeholder', N'zh-TW', N'請輸入角色群組', N'Role', 1),
(N'Role.RoleGroup.Placeholder', N'en-US', N'Please enter role group', N'Role', 1),

(N'Role.RoleCode.Placeholder', N'zh-TW', N'請輸入角色代碼', N'Role', 1),
(N'Role.RoleCode.Placeholder', N'en-US', N'Please enter role code', N'Role', 1),

(N'Role.EditPermission', N'zh-TW', N'編輯【權限】', N'UserGroup', 1),
(N'Role.EditPermission', N'en-US', N'Edit Permissions', N'UserGroup', 1),

-- Create
(N'Role.Create.Title', N'zh-TW', N'角色管理-新增', N'Role', 1),
(N'Role.Create.Title', N'en-US', N'Role Management - Create', N'Role', 1),

-- Edit
(N'Role.Edit.Title', N'zh-TW', N'角色管理-編輯', N'Role', 1),
(N'Role.Edit.Title', N'en-US', N'Role Management - Edit', N'Role', 1),

-- Delete
(N'Role.Delete.Title', N'zh-TW', N'刪除角色', N'Role', 1),
(N'Role.Delete.Title', N'en-US', N'Delete Role', N'Role', 1),

-- Delete-使用者明細 (User)
(N'Role.Delete.UserUsageCount.Prefix', N'zh-TW', N'使用者明細：', N'Role', 1),
(N'Role.Delete.UserUsageCount.Prefix', N'en-US', N'User Details: ', N'Role', 1),

(N'Role.Delete.UserUsageCount.Suffix', N'zh-TW', N' 筆', N'Role', 1),
(N'Role.Delete.UserUsageCount.Suffix', N'en-US', N' items', N'Role', 1),

-- Delete-群組明細 (Group)
(N'Role.Delete.GroupUsageCount.Prefix', N'zh-TW', N'使用者群組明細：', N'Role', 1),
(N'Role.Delete.GroupUsageCount.Prefix', N'en-US', N'User Group Details: ', N'Role', 1),

(N'Role.Delete.GroupUsageCount.Suffix', N'zh-TW', N' 筆', N'Role', 1),
(N'Role.Delete.GroupUsageCount.Suffix', N'en-US', N' items', N'Role', 1),

-- Delete-警告訊息 (controller純文字)
(N'Role.Delete.UsedByUserOrGroup.Prefix', N'zh-TW', N'', N'角色', 1),
(N'Role.Delete.UsedByUserOrGroup.Suffix', N'zh-TW', N' 目前仍被使用者或群組使用，無法刪除。', N'Role', 1),

(N'Role.Delete.UsedByUserOrGroup.Prefix', N'en-US', N'Role ', N'Role', 1),
(N'Role.Delete.UsedByUserOrGroup.Suffix', N'en-US', N' is still used by users or groups and cannot be deleted.', N'Role', 1),

-- Delete-警告訊息 (包含 HTML 格式)
(N'Role.Delete.CannotDeleteMsg', N'zh-TW', N'此角色目前被上述使用者或群組使用，故<strong>無法刪除</strong>。<br />若要刪除，請先解除上述的使用者或群組角色設定。', N'Role', 1),
(N'Role.Delete.CannotDeleteMsg', N'en-US', N'This role is currently used by the above users or groups, so it <strong>cannot be deleted</strong>.<br />Please remove the user or group role settings first.', N'Role', 1),

(N'Role.Delete.PermissionWarning', N'zh-TW', N'（角色相關的權限將一併刪除）', N'Role', 1),
(N'Role.Delete.PermissionWarning', N'en-US', N'(Related role permissions will also be deleted)', N'Role', 1),

-- Details
(N'Role.Details.Title', N'zh-TW', N'角色管理-詳細資料', N'Role', 1),
(N'Role.Details.Title', N'en-US', N'Role Management - Details', N'Role', 1),

-- 權限(Delete、Details共用)
(N'Role.NoPermissions', N'zh-TW', N'(沒有任何啟用中的資源權限)', N'Role', 1),
(N'Role.NoPermissions', N'en-US', N'(No active resource permissions)', N'Role', 1),

-- 角色權限管理
-- Index(在資源中顯示用，其實沒有Index)
(N'RolePermission.Index.Title', N'zh-TW', N'角色管理-權限設定', N'RolePermission',   1),
(N'RolePermission.Index.Title', N'en-US', N'Role Permission Management', N'RolePermission',   1),

-- Edit
(N'RolePermission.Edit.Title', N'zh-TW', N'角色管理-權限設定', N'Role', 1),
(N'RolePermission.Edit.Title', N'en-US', N'Role Management - Permission Settings', N'Role', 1),

(N'Role.EditPermission.PermissionSetting', N'zh-TW', N'權限設定', N'Role', 1),
(N'Role.EditPermission.PermissionSetting', N'en-US', N'Permission Settings', N'Role', 1),

-- 提示訊息
(N'Role.EditPermission.NoResources', N'zh-TW', N'目前尚無任何啟用中的資源可設定。', N'Role', 1),
(N'Role.EditPermission.NoResources', N'en-US', N'No active resources available for configuration.', N'Role', 1),

(N'Role.EditPermission.NoActions', N'zh-TW', N'目前尚無任何動作(AppAction)可設定。', N'Role', 1),
(N'Role.EditPermission.NoActions', N'en-US', N'No actions (AppAction) available for configuration.', N'Role', 1),

(N'Role.EditPermission.Note', N'zh-TW', N'說明：勾選代表此角色對應資源擁有該動作權限；取消勾選將刪除原有的權限設定。', N'Role', 1),
(N'Role.EditPermission.Note', N'en-US', N'Note: Checked means the role has permission for the action on the resource; unchecking will remove the existing permission.', N'Role', 1),


-- 使用者群組
-- Index
(N'UserGroup.Index.Title',       N'zh-TW', N'使用者群組管理', N'UserGroup',       1),
(N'UserGroup.Index.Title',       N'en-US', N'User Group Management', N'UserGroup',   1),

(N'UserGroup.UserGroupCode.Placeholder', N'zh-TW', N'請輸入群組名稱', N'UserGroup', 1),
(N'UserGroup.UserGroupCode.Placeholder', N'en-US', N'Please enter group code', N'UserGroup', 1),

(N'UserGroup.UserGroupDescription.Placeholder', N'zh-TW', N'請輸入群組說明', N'UserGroup', 1),
(N'UserGroup.UserGroupDescription.Placeholder', N'en-US', N'Please enter description', N'UserGroup', 1),

(N'UserGroup.EditRole', N'zh-TW', N'編輯【角色】', N'UserGroup', 1),
(N'UserGroup.EditRole', N'en-US', N'Edit Roles', N'UserGroup', 1),


-- Create
(N'UserGroup.Create.Title', N'zh-TW', N'使用者群組管理-新增', N'UserGroup', 1),
(N'UserGroup.Create.Title', N'en-US', N'User Group Management - Create', N'UserGroup', 1),

-- Edit
(N'UserGroup.Edit.Title',   N'zh-TW', N'使用者群組管理-編輯', N'UserGroup', 1),
(N'UserGroup.Edit.Title',   N'en-US', N'User Group Management - Edit',   N'UserGroup', 1),

-- Delete
(N'UserGroup.Delete.Title', N'zh-TW', N'使用者群組管理-刪除', N'UserGroup', 1),
(N'UserGroup.Delete.Title', N'en-US', N'User Group Management - Delete', N'UserGroup', 1),

-- Details
(N'UserGroup.Details.Title', N'zh-TW', N'使用者群組管理-詳細資料', N'Role', 1),
(N'UserGroup.Details.Title', N'en-US', N'User Group Management - Details', N'Role', 1),


-- 使用者群組角色
-- Index(資源顯示用，其實沒有index)
(N'UserGroupRole.Index.Title',       N'zh-TW', N'使用者群組角色管理', N'UserGroup',       1),
(N'UserGroupRole.Index.Title',       N'en-US', N'User Group Role Management', N'UserGroup',   1),

-- Edit
(N'UserGroupRole.Edit.Title', N'zh-TW', N'使用者群組角色-編輯', N'UserGroupRole', 1),
(N'UserGroupRole.Edit.Title', N'en-US', N'User Group Role Management - Edit', N'UserGroupRole', 1),


-- 多語系
-- Index
(N'LocalizationString.Index.Title', N'zh-TW', N'多語系設定', N'LocalizationString', 1),
(N'LocalizationString.Index.Title', N'en-US', N'Localization Settings', N'LocalizationString', 1),

(N'LocalizationString.LocalizationStringKey.Placeholder', N'zh-TW', N'請輸入代號', N'LocalizationString', 1),
(N'LocalizationString.LocalizationStringKey.Placeholder', N'en-US', N'Enter localization key', N'LocalizationString', 1),

(N'LocalizationString.LocalizationStringCulture.Placeholder', N'zh-TW', N'例如：zh-TW / en-US', N'LocalizationString', 1),
(N'LocalizationString.LocalizationStringCulture.Placeholder', N'en-US', N'e.g. zh-TW / en-US', N'LocalizationString', 1),

(N'LocalizationString.LocalizationStringValue.Placeholder', N'zh-TW', N'請輸入顯示文字', N'LocalizationString', 1),
(N'LocalizationString.LocalizationStringValue.Placeholder', N'en-US', N'Enter display text', N'LocalizationString', 1),

(N'LocalizationString.LocalizationStringCategory.Placeholder', N'zh-TW', N'例如：Security / Menu', N'LocalizationString', 1),
(N'LocalizationString.LocalizationStringCategory.Placeholder', N'en-US', N'e.g. Security / Menu', N'LocalizationString', 1),

-- Create
(N'LocalizationString.Create.Title', N'zh-TW', N'新增多語系文字', N'LocalizationString', 1),
(N'LocalizationString.Create.Title', N'en-US', N'Create Localization String', N'LocalizationString', 1),

-- Edit
(N'LocalizationString.Edit.Title', N'zh-TW', N'編輯多語系文字', N'LocalizationString', 1),
(N'LocalizationString.Edit.Title', N'en-US', N'Edit Localization String', N'LocalizationString', 1),

-- Delete
(N'LocalizationString.Delete.Title', N'zh-TW', N'刪除多語系文字', N'LocalizationString', 1),
(N'LocalizationString.Delete.Title', N'en-US', N'Delete Localization String', N'LocalizationString', 1),

-- Details
(N'LocalizationString.Details.Title', N'zh-TW', N'多語系文字明細', N'LocalizationString', 1),
(N'LocalizationString.Details.Title', N'en-US', N'Localization String Details', N'LocalizationString', 1),

-- ===== 驗證 / 系統訊息 =====
(N'LocalizationString.KeyCulture.Duplicate', N'zh-TW', N'相同語系下，字串鍵值不可重複', N'LocalizationString', 1),
(N'LocalizationString.KeyCulture.Duplicate', N'en-US', N'Duplicate key under the same culture is not allowed', N'LocalizationString', 1),








-- 客製化錯誤頁面
(N'Error.RedirectCountdown.Prefix', N'zh-TW', N'將在 ', N'Error', 1),
(N'Error.RedirectCountdown.Prefix', N'en-US', N'Redirecting in ', N'Error', 1),

(N'Error.RedirectCountdown.Suffix', N'zh-TW', N' 秒後自動返回首頁...', N'Error', 1),
(N'Error.RedirectCountdown.Suffix', N'en-US', N' seconds. You will be redirected to the home page...', N'Error', 1),


-- 額外資料：各表的Value內容
(N'Department.Admin',  N'zh-TW', N'行政部', N'Department', 1),
(N'Department.Admin',  N'en-US', N'Admin', N'Department', 1),

(N'Department.IT',  N'zh-TW', N'資訊部', N'Department', 1),
(N'Department.IT',  N'en-US', N'IT', N'Department', 1),


-- 角色群組
-- System
(N'Role.SYSTEM', N'zh-TW', N'系統', N'Role', 1),
(N'Role.SYSTEM', N'en-US', N'System', N'Role', 1),

-- Purchase
(N'Role.PROCUREMENT', N'zh-TW', N'採購', N'Role', 1),
(N'Role.PROCUREMENT', N'en-US', N'Purchase', N'Role', 1),

-- Document
(N'Role.DOCUMENT', N'zh-TW', N'文管', N'Role', 1),
(N'Role.DOCUMENT', N'en-US', N'Document', N'Role', 1),

-- 角色代碼
-- SYSTEM_ADMIN
(N'Role.SYSTEM_ADMIN', N'zh-TW', N'系統管理者', N'Role', 1),
(N'Role.SYSTEM_ADMIN', N'en-US', N'System Administrator', N'Role', 1),

-- REQUESTER
(N'Role.REQUESTER', N'zh-TW', N'請購人', N'Role', 1),
(N'Role.REQUESTER', N'en-US', N'Requester', N'Role', 1),

-- PURCHASER
(N'Role.PURCHASER', N'zh-TW', N'採購人', N'Role', 1),
(N'Role.PURCHASER', N'en-US', N'Purchaser', N'Role', 1),

-- EVALUATOR
(N'Role.EVALUATOR', N'zh-TW', N'評核人', N'Role', 1),
(N'Role.EVALUATOR', N'en-US', N'Evaluator', N'Role', 1),

-- CONSUMER
(N'Role.CONSUMER', N'zh-TW', N'領用人', N'Role', 1),
(N'Role.CONSUMER', N'en-US', N'Consumer', N'Role', 1),

-- OWNER
(N'Role.OWNER', N'zh-TW', N'負責人', N'Role', 1),
(N'Role.OWNER', N'en-US', N'Owner', N'Role', 1),


-- 系統動作
-- Index
(N'AppAction.Index',   N'zh-TW', N'首頁/列表', N'AppAction', 1),
(N'AppAction.Index',   N'en-US', N'Index/List', N'AppAction', 1),

-- Create
(N'AppAction.Create',  N'zh-TW', N'新增', N'AppAction', 1),
(N'AppAction.Create',  N'en-US', N'Create', N'AppAction', 1),

-- Edit
(N'AppAction.Edit',    N'zh-TW', N'編輯', N'AppAction', 1),
(N'AppAction.Edit',    N'en-US', N'Edit', N'AppAction', 1),

-- Delete
(N'AppAction.Delete',  N'zh-TW', N'刪除', N'AppAction', 1),
(N'AppAction.Delete',  N'en-US', N'Delete', N'AppAction', 1),

-- Details
(N'AppAction.Details', N'zh-TW', N'檢視明細', N'AppAction', 1),
(N'AppAction.Details', N'en-US', N'Details', N'AppAction', 1),

-- Export
(N'AppAction.Export',  N'zh-TW', N'匯出', N'AppAction', 1),
(N'AppAction.Export',  N'en-US', N'Export', N'AppAction', 1),

-- Import
(N'AppAction.Import',  N'zh-TW', N'匯入', N'AppAction', 1),
(N'AppAction.Import',  N'en-US', N'Import', N'AppAction', 1),

-- EditGroup
(N'AppAction.EditGroup', N'zh-TW', N'編輯群組', N'AppAction', 1),
(N'AppAction.EditGroup', N'en-US', N'Edit Group', N'AppAction', 1),

-- PreviewPermissions
(N'AppAction.PreviewPermissions', N'zh-TW', N'預覽使用者群組角色權限', N'AppAction', 1),
(N'AppAction.PreviewPermissions', N'en-US', N'Preview Group Role Permissions', N'AppAction', 1),

-- ChangePassword
(N'AppAction.ChangePassword', N'zh-TW', N'變更密碼', N'AppAction', 1),
(N'AppAction.ChangePassword', N'en-US', N'Change Password', N'AppAction', 1),

-- ResetPassword
(N'AppAction.ResetPassword', N'zh-TW', N'重設密碼', N'AppAction', 1),
(N'AppAction.ResetPassword', N'en-US', N'Reset Password', N'AppAction', 1),

-- RegisterTotp
(N'AppAction.RegisterTotp', N'zh-TW', N'註冊 TOTP', N'AppAction', 1),
(N'AppAction.RegisterTotp', N'en-US', N'Register TOTP', N'AppAction', 1),

-- TotpQrCode
(N'AppAction.TotpQrCode', N'zh-TW', N'TOTP QR Code', N'AppAction', 1),
(N'AppAction.TotpQrCode', N'en-US', N'TOTP QR Code', N'AppAction', 1);



-- insert 資源
INSERT INTO Resource (ResourceType, ResourceKey, ResourceIsActive, CreatedAt)
VALUES
('PAGE', 'LocalizationString', 1, GETDATE());

-- ==== ResourceId ====
DECLARE @Res_LocalizationString INT;

SELECT @Res_LocalizationString = ResourceId
FROM Resource
WHERE ResourceKey = 'LocalizationString';

-- ==== AppActionId ====
DECLARE
    @Act_Index      INT,
    @Act_Details    INT,
    @Act_Create     INT,
    @Act_Edit       INT,
    @Act_Delete     INT;

SELECT @Act_Index   = AppActionId FROM AppAction WHERE AppActionCode = 'Index';
SELECT @Act_Details = AppActionId FROM AppAction WHERE AppActionCode = 'Details';
SELECT @Act_Create  = AppActionId FROM AppAction WHERE AppActionCode = 'Create';
SELECT @Act_Edit    = AppActionId FROM AppAction WHERE AppActionCode = 'Edit';
SELECT @Act_Delete  = AppActionId FROM AppAction WHERE AppActionCode = 'Delete';

/* =========================
   RolePermission
   多語系設定：Index/Details/Create/Edit/Delete
   固定 RoleId = 1 (管理者)
========================= */
INSERT INTO RolePermission (RoleId, ResourceId, AppActionId)
SELECT 1, @Res_LocalizationString, v.ActId
FROM (VALUES
    (@Act_Index),
    (@Act_Details),
    (@Act_Create),
    (@Act_Edit),
    (@Act_Delete)
) v(ActId)
WHERE v.ActId IS NOT NULL
  AND @Res_LocalizationString IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM RolePermission rp
      WHERE rp.RoleId = 1
        AND rp.ResourceId = @Res_LocalizationString
        AND rp.AppActionId = v.ActId
  );


/* =========================
   MenuItem
   多語系設定：LocalizationString
   固定 MenuItemParentId = 1 (系統管理)
========================= */

DECLARE @Menu_SystemRoot INT = 1;
IF NOT EXISTS (
    SELECT 1
    FROM MenuItem
    WHERE MenuItemParentId = @Menu_SystemRoot
      AND ResourceId = @Res_LocalizationString
)
BEGIN
    INSERT INTO MenuItem
    (MenuItemParentId, MenuItemIcon, MenuItemDisplayOrder, MenuItemIsActive, ResourceId, CreatedAt, CreatedBy)
    VALUES
    (@Menu_SystemRoot, 'fa-solid fa-language', 5, 1, @Res_LocalizationString, GETDATE(), NULL);
END