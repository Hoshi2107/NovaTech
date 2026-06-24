using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using System.Linq;

namespace DATN64.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var emp = _context.NhanViens.FirstOrDefault(e => e.Email.Equals(email));

            if (emp != null)
            {
                var userRoles = string.IsNullOrEmpty(emp.VaiTro) ? new string[0] : emp.VaiTro.Split(',');
                
                HttpContext.Session.SetString("UserEmail", emp.Email);
                HttpContext.Session.SetString("UserName", emp.HoTen);
                HttpContext.Session.SetString("UserRoles", emp.VaiTro ?? "");
                HttpContext.Session.SetString("UserPermissions", "All"); // Simplified

                TempData["ToastMessage"] = "Đăng nhập thành công!";
                TempData["ToastType"] = "success";

                return RedirectToAction("Selection", "Portal");
            }

            // Customer check (simulated)
            if (email.Equals("customer@gmail.com", System.StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.SetString("UserEmail", "customer@gmail.com");
                HttpContext.Session.SetString("UserName", "Khách Hàng Demo");
                HttpContext.Session.SetString("UserRoles", "Khách hàng");
                HttpContext.Session.SetString("UserPermissions", "");

                TempData["ToastMessage"] = "Chào mừng bạn đến với NovaTech Store!";
                return RedirectToAction("Index", "Online");
            }

            ModelState.AddModelError("", "Thông tin đăng nhập không chính xác hoặc tài khoản không tồn tại.");
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

            var emp = _context.NhanViens.FirstOrDefault(e => e.Email == email);
            if (emp == null)
            {
                ViewBag.FullName = HttpContext.Session.GetString("UserName");
                ViewBag.Email = email;
                ViewBag.Phone = "0900000000";
                ViewBag.Roles = "Khách hàng";
            }
            else
            {
                ViewBag.FullName = emp.HoTen;
                ViewBag.Email = emp.Email;
                ViewBag.Phone = emp.SoDienThoai;
                ViewBag.Roles = emp.VaiTro;
            }

            return View();
        }
    }
}
