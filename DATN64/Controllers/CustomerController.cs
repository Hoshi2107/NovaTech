using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using DATN64.Models;
using DATN64.Helpers;
using System;
using System.Linq;

namespace DATN64.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        [HasPermission("View_Customer")]
        public IActionResult Index()
        {
            var customers = _context.KhachHangs
                .OrderByDescending(k => k.NgayTao)
                .ToList();

            return View(customers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("View_Customer")]
        public IActionResult Create(string HoTen, string SoDienThoai, string Email, string DiaChi, string MatKhau)
        {
            HoTen = (HoTen ?? "").Trim();
            SoDienThoai = (SoDienThoai ?? "").Trim();
            Email = (Email ?? "").Trim();
            DiaChi = (DiaChi ?? "").Trim();
            MatKhau = (MatKhau ?? "").Trim();

            if (string.IsNullOrWhiteSpace(HoTen) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(MatKhau))
            {
                TempData["ToastMessage"] = "Vui lòng nhập đầy đủ họ tên, email và mật khẩu.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            var existedEmail = _context.KhachHangs.Any(k => k.Email == Email)
                || _context.NhanViens.Any(nv => nv.Email == Email);

            if (existedEmail)
            {
                TempData["ToastMessage"] = "Email này đã tồn tại trong hệ thống.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(SoDienThoai))
            {
                var existedPhone = _context.KhachHangs.Any(k => k.SoDienThoai == SoDienThoai);

                if (existedPhone)
                {
                    TempData["ToastMessage"] = "Số điện thoại này đã tồn tại.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction(nameof(Index));
                }
            }

            var khachHang = new KhachHang
            {
                HoTen = HoTen,
                SoDienThoai = SoDienThoai,
                Email = Email,
                DiaChi = DiaChi,
                DiemTichLuy = 0,
                TrangThai = "Hoạt động",
                NgayTao = DateTime.Now
            };

            var hasher = new PasswordHasher<KhachHang>();
            khachHang.MatKhau = hasher.HashPassword(khachHang, MatKhau);

            _context.KhachHangs.Add(khachHang);
            _context.SaveChanges();

            TempData["ToastMessage"] = "Tạo khách hàng thành công. Mật khẩu đã được mã hóa.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Index));
        }
    }
}