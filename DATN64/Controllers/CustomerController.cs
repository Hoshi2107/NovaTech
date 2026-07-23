using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        public IActionResult Index(string? keyword, string? rank, string? status, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;

            var allowedPageSizes = new[] { 10, 20, 50 };
            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 20;
            }

            keyword = (keyword ?? string.Empty).Trim();
            rank = (rank ?? string.Empty).Trim();
            status = (status ?? string.Empty).Trim();

            var query = _context.KhachHangs.AsQueryable();

            /*
             * ẨN KHÁCH VÃNG LAI KHỎI TRANG /Customer
             * Trang này chỉ dùng để quản lý khách hàng thành viên.
             * Khách vãng lai nếu có tồn tại trong database thì không hiển thị ở đây.
             */
            query = query.Where(k =>
                // Không lấy khách có trạng thái vãng lai
                (
                    k.TrangThai == null ||
                    (
                        k.TrangThai != "Khách vãng lai" &&
                        k.TrangThai != "Khách Vãng Lai" &&
                        k.TrangThai != "Vãng lai" &&
                        k.TrangThai != "Vãng Lai"
                    )
                )

                // Không lấy khách có tên là khách vãng lai
                &&
                (
                    k.HoTen == null ||
                    (
                        !EF.Functions.Like(k.HoTen, "%Khách Hàng Vãng Lai%") &&
                        !EF.Functions.Like(k.HoTen, "%Khách hàng vãng lai%") &&
                        !EF.Functions.Like(k.HoTen, "%Khách Vãng Lai%") &&
                        !EF.Functions.Like(k.HoTen, "%Khách vãng lai%") &&
                        !EF.Functions.Like(k.HoTen, "%Khach Hang Vang Lai%") &&
                        !EF.Functions.Like(k.HoTen, "%Khach hang vang lai%") &&
                        !EF.Functions.Like(k.HoTen, "%Khach Vang Lai%") &&
                        !EF.Functions.Like(k.HoTen, "%Khach vang lai%")
                    )
                )

                // Không lấy khách có email/phone dạng guest
                &&
                (
                    k.Email == null ||
                    (
                        !EF.Functions.Like(k.Email, "%guest%") &&
                        !EF.Functions.Like(k.Email, "%vanglai%") &&
                        !EF.Functions.Like(k.Email, "%khachvanglai%")
                    )
                )

                &&
                (
                    k.SoDienThoai == null ||
                    (
                        !EF.Functions.Like(k.SoDienThoai, "GUEST-%") &&
                        !EF.Functions.Like(k.SoDienThoai, "guest-%")
                    )
                )
            );

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(k =>
                    (k.HoTen != null && k.HoTen.Contains(keyword)) ||
                    (k.SoDienThoai != null && k.SoDienThoai.Contains(keyword)) ||
                    (k.Email != null && k.Email.Contains(keyword))
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(k => k.TrangThai == status);
            }

            if (rank == "Đồng")
            {
                query = query.Where(k => k.DiemTichLuy < 500);
            }
            else if (rank == "Bạc")
            {
                query = query.Where(k => k.DiemTichLuy >= 500 && k.DiemTichLuy < 1500);
            }
            else if (rank == "Vàng")
            {
                query = query.Where(k => k.DiemTichLuy >= 1500 && k.DiemTichLuy < 3000);
            }
            else if (rank == "Kim Cương")
            {
                query = query.Where(k => k.DiemTichLuy >= 3000);
            }

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var customers = query
                .OrderByDescending(k => k.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Keyword = keyword;
            ViewBag.RankFilter = rank;
            ViewBag.StatusFilter = status;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

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