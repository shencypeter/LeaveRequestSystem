using BioMedDocManager.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Controllers
{
    /// <summary>
    /// 帳號設定
    /// </summary>
    /// <param name="context">資料庫查詢物件</param>
    /// <param name="hostingEnvironment">網站環境變數</param>
    [Authorize(Roles = AdminRoleStrings.系統管理者)]
    public class AccountSettingsController(DocControlContext context, IWebHostEnvironment hostingEnvironment) : BaseController(context, hostingEnvironment)
    {

        /// <summary>
        /// 預設排序依據
        /// </summary>
        public const string InitSort = "username";

        /// <summary>
        /// 查詢畫面的表頭DB與中文對照 (因為沒有結果也要顯示)
        /// </summary>
        public Dictionary<string, string> TableHeaders = new()
        {
            // 系統用欄位
            { "RowNum", "#" },

            // 使用者相關
            { "username", "帳號" },
            { "department_name", "部門" },
            { "job_title", "職稱" },
            { "full_name", "使用者名稱" },
            { "is_active", "是否啟用" },
            { "created_at", "註冊時間" },
            { "RoleNameList", "系統角色" },
            { "status", "狀態" },
        };

        /// <summary>
        /// 顯示帳號設定頁面
        /// </summary>
        /// <param name="PageSize">單頁顯示筆數</param>
        /// <param name="PageNumber">第幾頁</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int? PageSize, [FromQuery] int? PageNumber)
        {
            // Retrieve query model from session or create a default one
            var queryModel = GetSessionQueryModel<AccountModel>();

            // 如果query string有帶入page參數，才使用；否則保留Session中的值
            if (PageSize.HasValue)
            {
                queryModel.PageSize = PageSize.Value;
            }
            if (PageNumber.HasValue)
            {
                queryModel.PageNumber = PageNumber.Value;
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(queryModel);

            // 儲存查詢model到session中
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            // 載入角色List
            ViewData["AllRoles"] = await GetRoles();

            return await LoadPage(queryModel);
        }

        /// <summary>
        /// 文件管制查詢頁面送出查詢
        /// </summary>
        /// <param name="queryModel">查詢model</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(AccountModel queryModel)
        {
            // 儲存查詢model到session中
            QueryableExtensions.SetSessionQueryModel(HttpContext, queryModel);

            // 轉跳到GET方法頁面，顯示查詢內容
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示新增頁
        /// </summary>
        /// <returns></returns>
        [Route("[controller]/Create/")]
        public async Task<IActionResult> Create()
        {
            var accountModel = new CreateUser
            {
                CreatedAt = DateTime.Now,
            };

            // 載入角色List
            ViewData["AllRoles"] = await GetRoles();

            return View(accountModel);

        }

        /// <summary>
        /// 更新個人資料
        /// </summary>
        /// <param name="user">資料</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("[controller]/Create")]
        public async Task<IActionResult> Create(CreateUser PostedUser)
        {
            if (PostedUser == null)
            {
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(PostedUser);

            try
            {

                ModelState.Remove("RoleName");//不用驗證
                ModelState.Remove("RoleNameList");//不用驗證

                if (!ModelState.IsValid)
                {
                    return View(PostedUser);
                }

                var newUser = ToUserEntity(PostedUser);

                await context.Users.AddAsync(newUser);
                await context.SaveChangesAsync(); // 儲存後 newUser.Id 才有值

                // 角色

                // 抓選被checkbox的角色名稱
                var roleEntities = context.Roles
                    .AsEnumerable()
                    .Where(r => PostedUser.RoleName.Contains(r.RoleName))
                    .ToList();
                if (roleEntities.Count > 0)
                {
                    // 加入新角色
                    foreach (var role in roleEntities)
                    {
                        newUser.UserRoles.Add(new UserRole
                        {
                            UserId = newUser.Id,
                            RoleId = role.Id
                        });
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["_JSShowAlert"] = "帳號設定-" + PostedUser.FullName + "資料新增【失敗】!";
            }

            TempData["_JSShowSuccess"] = "帳號設定-" + PostedUser.FullName + "資料新增成功!";

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示編輯頁
        /// </summary>
        /// <param name="UserName">工號</param>
        /// <returns></returns>
        [Route("[controller]/Edit/{UserName}")]
        public async Task<IActionResult> Edit([FromRoute] string UserName)
        {
            if (string.IsNullOrEmpty(UserName))
            {
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(UserName);

            var user = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(s => s.UserName == UserName);

            if (user == null)
            {
                return NotFound();
            }

            var accountModel = new AccountModel
            {
                UserName = user.UserName,
                FullName = user.FullName,
                JobTitle = user.JobTitle,
                DepartmentName = user.DepartmentName,
                Email = user.Email,
                Phone = user.Phone,
                Mobile = user.Mobile,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                IsLocked = user.IsLocked,
                Status = user.Status,
                Remarks = user.Remarks,
                RoleName = user.UserRoles?
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role.RoleName)
                    .ToList() ?? new List<string>(),
                RoleNameList = string.Join("、",
                        user.UserRoles?
                            .Where(ur => ur.Role != null)
                            .Select(ur => ur.Role.RoleGroup + "-" + ur.Role.RoleName)
                        ?? Enumerable.Empty<string>())
            };


            // 載入角色List
            ViewData["AllRoles"] = await GetRoles();

            return View(accountModel);
        }

        /// <summary>
        /// 更新個人資料
        /// </summary>
        /// <param name="id">工號</param>
        /// <param name="user">資料</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("[controller]/Edit/{UserName}")]
        public async Task<IActionResult> Edit([FromRoute] string UserName, AccountModel user)
        {
            if (UserName != user.UserName)
            {
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(UserName);
            QueryableExtensions.TrimStringProperties(user);


            var DBuser = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role) // 確保 Role 有載入
                .FirstOrDefaultAsync(s => s.UserName == user.UserName);

            if (DBuser == null)
            {
                return NotFound();
            }

            try
            {
                // 確認有選擇新角色
                if (user.RoleName != null)
                {

                    DBuser.IsActive = user.IsActive.HasValue ? user.IsActive.Value : false;// 是否啟用
                    DBuser.FullName = user.FullName;// 使用者姓名


                    // 角色
                    //是否為管理者
                    var isAdmin = User?.IsInRole("系統管理者") ?? false;
                    var isEditingSelfAdmin = (DBuser.UserName == GetLoginUserId()) && isAdmin;

                    // 1) 記憶體過濾：把前端勾選的角色名稱正規化並轉為集合
                    var selectedNames = (user.RoleName ?? Enumerable.Empty<string>())
                        .Select(n => (n ?? string.Empty).Trim())
                        .Where(n => n.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    var roleLookup = await context.Roles
                        .Select(r => new { r.Id, r.RoleName })
                        .ToListAsync();

                    var selectedSet = new HashSet<string>(selectedNames, StringComparer.OrdinalIgnoreCase);

                    var desiredRoleIds = roleLookup
                        .Where(r => selectedSet.Contains(r.RoleName))
                        .Select(r => r.Id)
                        .ToList();

                    // 2) 目前資料庫中這位使用者的角色 Id
                    var currentRoleIds = DBuser.UserRoles
                        .Select(ur => ur.RoleId)
                        .ToList();

                    // 3) 若是系統管理者在編修自己帳號：保留「系統」群組的角色，不受此次異動影響
                    var preservedSystemRoleIds = isEditingSelfAdmin
                        ? DBuser.UserRoles
                            .Where(ur => ur.Role?.RoleGroup == "系統")
                            .Select(ur => ur.RoleId)
                            .ToHashSet()
                        : new HashSet<int>();

                    // 4) 計算需要移除與需要新增的角色
                    //    - 要移除：目前有、但（不在此次目標集合）且（也不是要保留的系統角色）
                    var toRemoveRoleIds = currentRoleIds
                        .Where(id => !desiredRoleIds.Contains(id) && !preservedSystemRoleIds.Contains(id))
                        .ToList();

                    //    - 要新增：目標集合有，但目前沒有（即可避免新增重覆）
                    var toAddRoleIds = desiredRoleIds
                        .Where(id => !currentRoleIds.Contains(id))
                        .ToList();

                    // 5) 真的移除
                    if (toRemoveRoleIds.Count > 0)
                    {
                        var removeEntities = DBuser.UserRoles
                            .Where(ur => toRemoveRoleIds.Contains(ur.RoleId))
                            .ToList();

                        context.UserRoles.RemoveRange(removeEntities);
                    }

                    // 6) 真的新增
                    foreach (var roleId in toAddRoleIds)
                    {
                        DBuser.UserRoles.Add(new UserRole
                        {
                            UserId = DBuser.Id,
                            RoleId = roleId
                        });
                    }

                    // 7) 其他欄位更新
                    DBuser.IsActive = user.IsActive ?? false;
                    DBuser.FullName = user.FullName;

                    await context.SaveChangesAsync();

                }
                else
                {
                    // 未選擇任何角色
                    TempData["_JSShowAlert"] = "帳號設定-" + DBuser.FullName + "資料更新【失敗】，未選擇任何角色!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["_JSShowAlert"] = "帳號設定-" + DBuser.FullName + "資料更新【失敗】!";
                return RedirectToAction(nameof(Index));
            }

            TempData["_JSShowSuccess"] = "帳號設定-" + DBuser.FullName + "資料更新成功!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 顯示密碼重設頁
        /// </summary>
        /// <param name="UserName">工號</param>
        /// <returns></returns>
        [Route("[controller]/ResetPassword/{UserName}")]
        public async Task<IActionResult> ResetPassword([FromRoute] string UserName)
        {
            if (string.IsNullOrEmpty(UserName))
            {
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(UserName);

            var user = await context.Users
                .FirstOrDefaultAsync(s => s.UserName == UserName);

            if (user == null)
            {
                return NotFound();
            }

            // 產生變更密碼模型
            var model = new ChangePasswordModel
            {
                UserName = user.UserName,
                FullName = user.FullName,
            };

            return View(model);

        }

        /// <summary>
        /// 更新密碼
        /// </summary>
        /// <param name="id">工號</param>
        /// <param name="user">資料</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("[controller]/ResetPassword/{UserName}")]
        public async Task<IActionResult> ResetPassword([FromRoute] string UserName, ChangePasswordModel PostedUser)
        {
            if (UserName != PostedUser.UserName)
            {
                return NotFound();
            }

            // 過濾文字
            QueryableExtensions.TrimStringProperties(UserName);
            QueryableExtensions.TrimStringProperties(PostedUser);

            var User = await context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(s => s.UserName == PostedUser.UserName);

            if (User == null)
            {
                return NotFound();
            }

            try
            {
                // 這是管理者重設，不用知道原本使用者密碼
                ModelState.Remove("CurrentPassword");// 不用驗證 原密碼

                if (!ModelState.IsValid)
                {
                    return View(PostedUser);
                }

                // 將新密碼寫入資料庫
                User.Password = HashPassword(User, PostedUser.NewPassword);

                await context.SaveChangesAsync();

            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["_JSShowAlert"] = "帳號設定-" + User.FullName + "密碼重設【失敗】!";
            }

            TempData["_JSShowSuccess"] = "帳號設定-" + User.FullName + "密碼重設完成!";

            return RedirectToAction(nameof(Index));

        }

        /// <summary>
        /// 載入資料與回傳畫面
        /// </summary>
        private async Task<IActionResult> LoadPage(AccountModel queryModel)
        {
            ViewData["pageNumber"] = queryModel.PageNumber.ToString();

            BuildQueryAccountSettings(queryModel, out DynamicParameters parameters, out string sqlDef);

            FilterOrderBy(queryModel, TableHeaders, InitSort);

            // 使用Dapper對查詢進行分頁(Paginate)
            var (items, totalCount) = await context.BySqlGetPagedWithCountAsync<dynamic>(
                sqlDef,
                orderByPart: $" ORDER BY  {queryModel.OrderBy} {queryModel.SortDir}",
                queryModel.PageNumber,
                queryModel.PageSize,
                parameters
            );

            // 即使無資料，也要確認標題存在
            List<Dictionary<string, object>> result = items?.Select(item =>
                (item as IDictionary<string, object>)?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            ).ToList() ?? new List<Dictionary<string, object>>();

            // Pass data to ViewData
            ViewData["totalCount"] = totalCount;

            ViewData["tableHeaders"] = TableHeaders;

            return View(result);
        }


        /// <summary>
        /// 查詢SQL
        /// </summary>
        /// <param name="queryModel"></param>
        /// <param name="parameters"></param>
        /// <param name="sqlQuery"></param>
        private static void BuildQueryAccountSettings(AccountModel queryModel, out DynamicParameters parameters, out string sqlQuery)
        {
            sqlQuery = @"
        SELECT 
            u.id,
            u.username,
            u.full_name,
            u.job_title,
            u.department_name,
            u.email,
            u.phone,
            u.mobile,
            CASE WHEN u.is_active = 1 THEN N'啟用' ELSE N'停用' END AS is_active,
            u.is_locked,
            u.login_failed_count,
            u.last_login_at,
            u.last_login_ip,
            u.password_changed_at,
            u.status,
            u.remarks,
            u.created_at,
            u.created_by,
            u.updated_at,
            u.updated_by,
            u.deleted_at,
            u.deleted_by,
            r.RoleNameList
        FROM [user] u
        LEFT JOIN (
            SELECT 
                ur.user_id,
                STRING_AGG(r.role_group + N'-' + r.role_name, N'、') AS RoleNameList
            FROM user_role ur
            INNER JOIN role r ON ur.role_id = r.id
            GROUP BY ur.user_id
        ) r ON r.user_id = u.id
        WHERE 1 = 1
    ";

            var whereClauses = new List<string>();
            parameters = new DynamicParameters();

            // 工號(帳號)
            if (!string.IsNullOrEmpty(queryModel.UserName))
            {
                whereClauses.Add("u.username LIKE @UserName");
                parameters.Add("UserName", $"%{queryModel.UserName.Trim()}%");
            }

            // 姓名
            if (!string.IsNullOrEmpty(queryModel.FullName))
            {
                whereClauses.Add("u.full_name LIKE @FullName");
                parameters.Add("FullName", $"%{queryModel.FullName.Trim()}%");
            }

            // 是否啟用
            if (!string.IsNullOrEmpty(queryModel.IsActive.ToString()))
            {
                whereClauses.Add("u.is_active = @IsActive");
                parameters.Add("IsActive", queryModel.IsActive);
            }

            // 系統角色（用子查詢改寫後，若要用角色做條件可追加 EXISTS）
            if (queryModel.RoleName != null && queryModel.RoleName.Any())
            {
                whereClauses.Add(@"
            EXISTS (
                SELECT 1
                FROM user_role ur2
                INNER JOIN role r2 ON ur2.role_id = r2.id
                WHERE ur2.user_id = u.id
                  AND r2.role_name IN @RoleName
            )
        ");
                parameters.Add("RoleName", queryModel.RoleName);
            }

            if (whereClauses.Any())
            {
                sqlQuery += " AND " + string.Join(" AND ", whereClauses);
            }

            // 不要 GROUP BY（因為已用子查詢聚合角色，主查詢可直接排序/分頁）
        }


        /// <summary>
        /// 查詢SQL
        /// </summary>
        /// <param name="queryModel"></param>
        /// <param name="parameters"></param>
        /// <param name="sqlQuery"></param>
        private static void BuildQueryAccountSettings0(AccountModel queryModel, out DynamicParameters parameters, out string sqlQuery)
        {

            sqlQuery = @"
                SELECT 
                    u.id,
                    u.username,
                    u.full_name,
                    CASE WHEN u.is_active =1 THEN '啟用' ELSE '停用' END AS is_active,
                    u.created_at,
                    STRING_AGG(r.role_group+'-'+r.role_name, '、') AS RoleNameList 
                FROM [user] u
                LEFT JOIN user_role ur ON u.id = ur.user_id
                LEFT JOIN role r ON ur.role_id = r.id
                Where 1=1
                ";

            var whereClauses = new List<string>();
            parameters = new DynamicParameters();

            // 工號
            if (!string.IsNullOrEmpty(queryModel.UserName))
            {
                whereClauses.Add("u.username LIKE @UserName");
                parameters.Add("UserName", $"%{queryModel.UserName.Trim()}%");
            }

            // 姓名
            if (!string.IsNullOrEmpty(queryModel.FullName))
            {
                whereClauses.Add("u.full_name LIKE @FullName");
                parameters.Add("FullName", $"%{queryModel.FullName.Trim()}%");
            }

            // 是否啟用
            if (!string.IsNullOrEmpty(queryModel.IsActive.ToString()))
            {
                whereClauses.Add("u.is_active = @IsActive");
                parameters.Add("IsActive", queryModel.IsActive);
            }

            // 系統角色
            if (queryModel.RoleName != null && queryModel.RoleName.Any())
            {
                whereClauses.Add("r.role_name IN @RoleName");
                parameters.Add("RoleName", queryModel.RoleName);
            }


            // Add whereClauses to the SQL query if they exist
            if (whereClauses.Any())
            {
                sqlQuery += " AND " + string.Join(" AND ", whereClauses);
            }

            sqlQuery += " GROUP BY u.id, u.username, u.full_name, u.is_active, u.created_at";

        }







    }
}
