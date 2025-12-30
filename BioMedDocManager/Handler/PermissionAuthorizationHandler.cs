using BioMedDocManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BioMedDocManager.Handler
{
    public class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
    {
        private readonly DocControlContext _db;

        public PermissionAuthorizationHandler(DocControlContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // 1) 取得登入使用者 UserId (int)
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                // 未登入或 Claim 不是 int → 視為沒權限
                return;
            }

            // 2) 取得目前執行的 ControllerName + ActionName
            string? controllerName = null;
            string? actionName = null;

            switch (context.Resource)
            {
                // MVC (AuthorizationFilterContext)
                case AuthorizationFilterContext filterContext:
                    {
                        if (filterContext.ActionDescriptor is ControllerActionDescriptor cad)
                        {
                            controllerName = cad.ControllerName;
                            actionName = cad.ActionName;
                        }
                        break;
                    }

                // Endpoint Routing (HttpContext)
                case HttpContext httpContext:
                    {
                        var endpoint = httpContext.GetEndpoint();
                        var cad = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
                        if (cad != null)
                        {
                            controllerName = cad.ControllerName;
                            actionName = cad.ActionName;
                        }
                        break;
                    }

                // 其他情況（保險）
                case Endpoint endpoint:
                    {
                        var cad = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                        if (cad != null)
                        {
                            controllerName = cad.ControllerName;
                            actionName = cad.ActionName;
                        }
                        break;
                    }
            }

            if (string.IsNullOrEmpty(controllerName) || string.IsNullOrEmpty(actionName))
            {
                // 找不到 Controller/Action → 視為沒權限
                return;
            }

            // 3) 組 ResourceKey：Controller名稱
            var resourceKey = $"{controllerName}";

            // 4) 用 RolePermission 為中心做 join，檢查是否有對應權限
            var resourceKeyLower = resourceKey.ToLower();
            var actionNameLower = actionName.ToLower();            

            var query =
                from ugm in _db.UserGroupMembers
                join ugr in _db.UserGroupRoles on ugm.UserGroupId equals ugr.UserGroupId
                join rp in _db.RolePermissions on ugr.RoleId equals rp.RoleId
                join res in _db.Resources on rp.ResourceId equals res.ResourceId
                join act in _db.AppActions on rp.AppActionId equals act.AppActionId
                where ugm.UserId == userId
                    && res.ResourceKey.ToLower() == resourceKeyLower
                    && act.AppActionCode.ToLower() == actionNameLower
                select rp;

            // Debug 用：印出 EF 轉換後的 SQL
#if DEBUG
            var sql = query.ToQueryString();
            Console.WriteLine("=== 授權查詢 SQL ===");
            Console.WriteLine(sql);
#endif

            var hasPermission = await query.AnyAsync();

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            // 沒權限就保持失敗，頁面會出現403
        }
    }
}
