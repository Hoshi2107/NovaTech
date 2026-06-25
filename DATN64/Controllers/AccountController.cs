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
            var emp = _context.NhanViens.FirstOrDefault(e => e.Email != null && e.Email.Equals(email));

            if (emp != null)
            {
                if (emp.TrangThai != "Hoạt động")
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động.");
                    return View();
                }

                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<NhanVien>();
                var verificationResult = hasher.VerifyHashedPassword(emp, emp.MatKhau, password);
                if (verificationResult == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("", "Thông tin đăng nhập không chính xác hoặc tài khoản không tồn tại.");
                    return View();
                }

                // Retrieve roles dynamically from NhanVienRole junction table
                var roles = (from nr in _context.NhanVienRoles
                             join r in _context.Roles on nr.RoleId equals r.Id
                             where nr.MaNhanVien == emp.MaNhanVien
                             select r.Name).ToList();

                var rolesStr = string.Join(",", roles);

                // Retrieve distinct permissions associated with these roles
                var permissions = _context.RolePermissions
                    .Where(rp => roles.Contains(rp.RoleName))
                    .Select(rp => rp.PermissionName)
                    .Distinct()
                    .ToList();

                var permissionsStr = string.Join(",", permissions);

                HttpContext.Session.SetString("UserEmail", emp.Email ?? "");
                HttpContext.Session.SetString("UserName", emp.HoTen);
                HttpContext.Session.SetString("UserRoles", rolesStr);
                HttpContext.Session.SetString("UserPermissions", permissionsStr);

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
