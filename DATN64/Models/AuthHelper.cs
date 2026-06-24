using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using DATN64.Models;

namespace DATN64.Helpers
{
    public class HasPermissionAttribute : TypeFilterAttribute
    {
        public HasPermissionAttribute(string permissionName) : base(typeof(PermissionFilter))
        {
            Arguments = new object[] { permissionName };
        }
    }

    public class PermissionFilter : IAuthorizationFilter
    {
        private readonly string _permissionName;

        public PermissionFilter(string permissionName)
        {
            _permissionName = permissionName;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var email = session.GetString("UserEmail");

            if (string.IsNullOrEmpty(email))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var rolesString = session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Super Admin & Admin bypass
            if (roles.Contains("Super Admin") || roles.Contains("Admin"))
            {
                return;
            }

            var permissionsString = session.GetString("UserPermissions") ?? "";
            var userPermissions = permissionsString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!userPermissions.Contains(_permissionName))
            {
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }

    public static class AuthHelper
    {
        public static bool HasPermission(HttpContext context, string permissionName)
        {
            var session = context.Session;
            var rolesString = session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            
            if (roles.Contains("Super Admin") || roles.Contains("Admin")) return true;

            var permissionsString = session.GetString("UserPermissions") ?? "";
            var permissions = permissionsString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            return permissions.Contains(permissionName);
        }

        public static bool HasRole(HttpContext context, string roleName)
        {
            var session = context.Session;
            var rolesString = session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            return roles.Contains(roleName);
        }
    }
}
