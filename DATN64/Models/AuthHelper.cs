using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using DATN64.Models;

namespace DATN64.Helpers
{
    public static class PermissionConstants
    {
        public static readonly Dictionary<string, List<string>> Groups = new Dictionary<string, List<string>>
        {
            { "Sản phẩm", new List<string> { "View_Product", "Create_Product", "Edit_Product", "Delete_Product" } },
            { "Đơn hàng", new List<string> { "View_Order", "Create_Order", "Edit_Order", "Delete_Order", "Approve_Order" } },
            { "Khách hàng", new List<string> { "View_Customer", "Create_Customer", "Edit_Customer", "Delete_Customer" } },
            { "Kho bãi", new List<string> { "View_Inventory", "Create_Inventory", "Edit_Inventory", "Import_Inventory", "Export_Inventory" } },
            { "Nhân sự", new List<string> { "View_Employee", "Create_Employee", "Edit_Employee", "Delete_Employee", "Assign_Role" } },
            { "Hệ thống", new List<string> { "View_Report", "Export_Report", "View_TikTok", "Sync_TikTok", "View_Promotion", "View_Setting", "Edit_Setting", "View_Accounting" } }
        };

        public static List<string> All => Groups.Values.SelectMany(x => x).ToList();
    }

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
            var httpContext = context.HttpContext;
            var session = httpContext.Session;
            var email = session.GetString("UserEmail");

            if (string.IsNullOrEmpty(email))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (!AuthHelper.HasPermission(httpContext, _permissionName))
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
            var email = session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return false;

            const string cacheKey = "UserPermissionsCache";
            List<string> permissions;

            if (context.Items.TryGetValue(cacheKey, out var cachedObj) && cachedObj is List<string> cachedPerms)
            {
                permissions = cachedPerms;
            }
            else
            {
                var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
                var emp = dbContext.NhanViens.FirstOrDefault(e => e.Email == email);
                if (emp == null || emp.TrangThai == "Bị khóa") return false;

                var roles = (from nr in dbContext.NhanVienRoles
                             join r in dbContext.Roles on nr.RoleId equals r.Id
                             where nr.MaNhanVien == emp.MaNhanVien
                             select r.Name).ToList();

                if (roles.Contains("Super Admin"))
                {
                    permissions = new List<string> { "Super_Admin_Bypass" };
                }
                else
                {
                    permissions = dbContext.RolePermissions
                        .Where(rp => roles.Contains(rp.RoleName))
                        .Select(rp => rp.PermissionName)
                        .Distinct()
                        .ToList();
                }

                context.Items[cacheKey] = permissions;
            }

            if (permissions.Contains("Super_Admin_Bypass")) return true;
            return permissions.Contains(permissionName);
        }

        public static bool HasRole(HttpContext context, string roleName)
        {
            var session = context.Session;
            var email = session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return false;

            const string cacheKey = "UserRolesCache";
            List<string> roles;

            if (context.Items.TryGetValue(cacheKey, out var cachedObj) && cachedObj is List<string> cachedRoles)
            {
                roles = cachedRoles;
            }
            else
            {
                var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
                var emp = dbContext.NhanViens.FirstOrDefault(e => e.Email == email);
                if (emp == null || emp.TrangThai == "Bị khóa") return false;

                roles = (from nr in dbContext.NhanVienRoles
                             join r in dbContext.Roles on nr.RoleId equals r.Id
                             where nr.MaNhanVien == emp.MaNhanVien
                             select r.Name).ToList();

                context.Items[cacheKey] = roles;
            }

            return roles.Contains(roleName);
        }
    }

    public static class ImageHelper
    {
        public static string AppendVersion(string? url, string version = "2")
        {
            if (string.IsNullOrEmpty(url)) return "";
            if (url.Contains("?"))
            {
                return $"{url}&v={version}";
            }
            return $"{url}?v={version}";
        }
    }
}
