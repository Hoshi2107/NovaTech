using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using DATN64.Services;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace DATN64.Controllers
{
    public class PortalController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAttendanceService _attendanceService;

        public PortalController(AppDbContext context, IAttendanceService attendanceService)
        {
            _context = context;
            _attendanceService = attendanceService;
        }

        [HttpGet]
        public async Task<IActionResult> Selection()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account");
            }

            var rolesString = HttpContext.Session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Redirect customer role to retail shop directly
            if (roles.Contains("Khách hàng") && !roles.Any(r => r == "Super Admin" || r == "Admin" || r == "Quản lý cửa hàng" || r == "Nhân viên bán hàng" || r == "Nhân viên kho" || r == "Quản lý kho" || r == "Kế toán" || r == "Marketing"))
            {
                return RedirectToAction("Index", "Online");
            }

            var emp = _context.NhanViens.FirstOrDefault(e => e.Email == email);
            ChamCong activeOrTodayChamCong = null;
            if (emp != null)
            {
                // Tự động quét và xử lý các ca quên checkout ngày cũ của nhân viên này
                await _attendanceService.ProcessForgottenCheckoutAsync(emp.MaNhanVien);

                // Lấy thông tin ca làm việc đang hoạt động (Đang làm), nếu không có thì lấy ca gần nhất hôm nay
                activeOrTodayChamCong = _context.ChamCongs
                    .FirstOrDefault(c => c.MaNhanVien == emp.MaNhanVien && c.TrangThai == AttendanceService.Active)
                    ?? _context.ChamCongs
                    .Where(c => c.MaNhanVien == emp.MaNhanVien && c.NgayCham.Date == DateTime.Today)
                    .OrderByDescending(c => c.GioVao)
                    .FirstOrDefault();
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Nhân viên";
            ViewBag.RolesString = rolesString;
            ViewBag.TodayChamCong = activeOrTodayChamCong; // Truyền thông tin chấm công qua View
            
            // Allow access based on role (excluding 'Nhân viên bán hàng' from ERP access)
            ViewBag.CanAccessERP = roles.Any(r => r == "Super Admin" || r == "Admin" || r == "Quản lý cửa hàng" || r == "Nhân viên kho" || r == "Quản lý kho" || r == "Kế toán" || r == "Marketing");
            ViewBag.CanAccessPOS = roles.Any(r => r == "Super Admin" || r == "Admin" || r == "Quản lý cửa hàng" || r == "Nhân viên bán hàng");
            ViewBag.CanAccessOnline = true; // Everyone can view the retail shop

            return View();
        }

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> QuickToggleChamCong()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Account");

            var emp = _context.NhanViens.FirstOrDefault(e => e.Email == userEmail);
            if (emp == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy thông tin nhân viên!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Selection");
            }

            // Tự động quét và xử lý các ca quên checkout ngày cũ trước khi đổi ca
            await _attendanceService.ProcessForgottenCheckoutAsync(emp.MaNhanVien);

            // Tìm ca làm việc đang hoạt động ("Đang làm")
            var activeChamCong = _context.ChamCongs
                .Where(c => c.MaNhanVien == emp.MaNhanVien && c.TrangThai == AttendanceService.Active)
                .OrderByDescending(c => c.GioVao)
                .FirstOrDefault();

            if (activeChamCong != null)
            {
                // Thực hiện CHECK OUT
                activeChamCong.GioRa = DateTime.Now;
                activeChamCong.TrangThai = AttendanceService.Completed;
                if (activeChamCong.GioVao.HasValue)
                {
                    activeChamCong.TongGioLam = Math.Round((DateTime.Now - activeChamCong.GioVao.Value).TotalHours, 2);
                }
                _context.SaveChanges();

                TempData["ToastMessage"] = $"Check Out thành công lúc {DateTime.Now:HH:mm}! Tổng giờ làm: {activeChamCong.TongGioLam:F2} giờ.";
                TempData["ToastType"] = "success";
            }
            else
            {
                // Thực hiện CHECK IN
                var chamCong = new ChamCong
                {
                    MaNhanVien = emp.MaNhanVien,
                    NgayCham = DateTime.Today,
                    GioVao = DateTime.Now,
                    TrangThai = AttendanceService.Active
                };
                _context.ChamCongs.Add(chamCong);
                _context.SaveChanges();

                TempData["ToastMessage"] = $"Check In thành công lúc {DateTime.Now:HH:mm}. Chúc bạn một ngày làm việc hiệu quả!";
                TempData["ToastType"] = "success";
            }

            // Redirect back to referrer or fallback to Selection
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction("Selection");
        }
    }
}
