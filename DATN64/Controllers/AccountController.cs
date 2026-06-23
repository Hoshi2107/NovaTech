using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using System.Linq;

namespace DATN64.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var emp = MockDataService.Instance.Employees
                .FirstOrDefault(e => e.Email.Equals(email, System.StringComparison.OrdinalIgnoreCase) && e.Password == password);

            if (emp != null)
            {
                if (emp.Status != "Đang làm việc")
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đang bị khóa hoặc ngưng hoạt động.");
                    return View();
                }

                // Get role list and find matching permissions
                var userRoles = emp.Roles;
                var allPermissions = new List<string>();
                foreach (var r in userRoles)
                {
                    var rPermObj = MockDataService.Instance.RolesList.FirstOrDefault(rp => rp.RoleName == r);
                    if (rPermObj != null)
                    {
                        allPermissions.AddRange(rPermObj.Permissions);
                    }
                }

                HttpContext.Session.SetString("UserEmail", emp.Email);
                HttpContext.Session.SetString("UserName", emp.FullName);
                HttpContext.Session.SetString("UserRoles", string.Join(",", userRoles));
                HttpContext.Session.SetString("UserPermissions", string.Join(",", allPermissions.Distinct()));

                TempData["ToastMessage"] = "Đăng nhập thành công!";
                TempData["ToastType"] = "success";

                // Redirect to Portal for choosing path
                return RedirectToAction("Selection", "Portal");
            }

            // Customer check (simulated)
            if (email.Equals("customer@gmail.com", System.StringComparison.OrdinalIgnoreCase) && password == "123")
            {
                HttpContext.Session.SetString("UserEmail", "customer@gmail.com");
                HttpContext.Session.SetString("UserName", "Khách Hàng Demo");
                HttpContext.Session.SetString("UserRoles", "Khách hàng");
                HttpContext.Session.SetString("UserPermissions", "");

                TempData["ToastMessage"] = "Chào mừng bạn đến với NovaTech Store!";
                return RedirectToAction("Index", "Online");
            }

            ModelState.AddModelError("", "Thông tin đăng nhập không chính xác.");
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Online");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            ViewBag.SuccessMessage = "Một email khôi phục mật khẩu đã được gửi đến " + email;
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string oldPassword, string newPassword)
        {
            TempData["ToastMessage"] = "Đổi mật khẩu thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Selection", "Portal");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            var emp = MockDataService.Instance.Employees.FirstOrDefault(e => e.Email == email);
            if (emp == null)
            {
                // Customer profile
                ViewBag.FullName = HttpContext.Session.GetString("UserName");
                ViewBag.Email = email;
                ViewBag.Phone = "0900000000";
                ViewBag.Roles = "Khách hàng";
            }
            else
            {
                ViewBag.FullName = emp.FullName;
                ViewBag.Email = emp.Email;
                ViewBag.Phone = emp.Phone;
                ViewBag.Roles = string.Join(", ", emp.Roles);
            }

            return View();
        }
    }
}
