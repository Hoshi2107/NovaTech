using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using System.Linq;

namespace DATN64.Controllers
{
    public class PortalController : Controller
    {
        [HttpGet]
        public IActionResult Selection()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account");
            }

            var rolesString = HttpContext.Session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Nhân viên";
            ViewBag.RolesString = rolesString;
            
            // Allow access based on role
            ViewBag.CanAccessERP = roles.Any(r => r == "Super Admin" || r == "Admin" || r == "Quản lý cửa hàng" || r == "Nhân viên bán hàng" || r == "Nhân viên kho" || r == "Kế toán" || r == "Marketing");
            ViewBag.CanAccessPOS = roles.Any(r => r == "Super Admin" || r == "Admin" || r == "Quản lý cửa hàng" || r == "Nhân viên bán hàng");
            ViewBag.CanAccessOnline = true; // Everyone can view the retail shop

            return View();
        }
    }
}
