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
            if (page < 1)
            {
                page = 1;
            }

            var allowedPageSizes = new[] { 10, 20, 50 };

            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 20;
            }

            keyword = (keyword ?? string.Empty).Trim();
            rank = (rank ?? string.Empty).Trim();
            status = (status ?? string.Empty).Trim();

            var query = _context.KhachHangs.AsQueryable();

            query = query.Where(k =>
                (
                    k.TrangThai == null ||
                    (
                        k.TrangThai != "Khách vãng lai" &&
                        k.TrangThai != "Khách Vãng Lai" &&
                        k.TrangThai != "Vãng lai" &&
                        k.TrangThai != "Vãng Lai"
                    )
                )
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
            HoTen = NormalizeText(HoTen);
            SoDienThoai = NormalizePhone(SoDienThoai);
            Email = NormalizeEmail(Email);
            DiaChi = NormalizeText(DiaChi);
            MatKhau = (MatKhau ?? "").Trim();

            if (string.IsNullOrWhiteSpace(HoTen) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(MatKhau))
            {
                TempData["ToastMessage"] = "Vui lòng nhập đầy đủ họ tên, email và mật khẩu.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            if (!IsValidEmailBasic(Email))
            {
                TempData["ToastMessage"] = "Email không hợp lệ.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            if (CustomerEmailExists(Email))
            {
                TempData["ToastMessage"] = "Email này đã được khách hàng khác sử dụng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            if (EmployeeEmailExists(Email))
            {
                TempData["ToastMessage"] = "Email này đã được nhân viên sử dụng. Vui lòng dùng email khác.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(SoDienThoai) && CustomerPhoneExists(SoDienThoai))
            {
                TempData["ToastMessage"] = "Số điện thoại này đã được khách hàng khác sử dụng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
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

            try
            {
                _context.KhachHangs.Add(khachHang);
                _context.SaveChanges();

                TempData["ToastMessage"] = "Tạo khách hàng thành công. Mật khẩu đã được mã hóa.";
                TempData["ToastType"] = "success";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ToastMessage"] = ex.Message;
                TempData["ToastType"] = "danger";
            }
            catch (DbUpdateException)
            {
                TempData["ToastMessage"] = "Email hoặc số điện thoại đã tồn tại. Vui lòng kiểm tra lại.";
                TempData["ToastType"] = "danger";
            }
            catch
            {
                TempData["ToastMessage"] = "Có lỗi xảy ra khi tạo khách hàng. Vui lòng thử lại.";
                TempData["ToastType"] = "danger";
            }

            return RedirectToAction(nameof(Index));
        }

        [HasPermission("View_Customer")]
        public IActionResult Detail(int id)
        {
            var customer = _context.KhachHangs
                .AsNoTracking()
                .FirstOrDefault(k => k.MaKhachHang == id);

            if (customer == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy khách hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            var orders = _context.DonHangs
                .AsNoTracking()
                .Where(o => o.MaKhachHang == id)
                .OrderByDescending(o => o.NgayDat)
                .ToList();

            ViewBag.Orders = orders;
            ViewBag.TotalOrders = orders.Count;
            ViewBag.CompletedOrders = orders.Count(o =>
                (o.TrangThai ?? "").Trim() == "Hoàn thành" ||
                (o.TrangThai ?? "").Trim() == "Đã giao");

            ViewBag.TotalSpent = orders
                .Where(o =>
                    (o.TrangThai ?? "").Trim() == "Hoàn thành" ||
                    (o.TrangThai ?? "").Trim() == "Đã giao")
                .Sum(o => o.TongTien ?? 0);

            return View(customer);
        }

        [HasPermission("View_Customer")]
        public IActionResult Edit(int id)
        {
            var customer = _context.KhachHangs
                .FirstOrDefault(k => k.MaKhachHang == id);

            if (customer == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy khách hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("View_Customer")]
        public IActionResult Edit(int id, string HoTen, string SoDienThoai, string Email, string DiaChi, string TrangThai)
        {
            var customer = _context.KhachHangs
                .FirstOrDefault(k => k.MaKhachHang == id);

            if (customer == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy khách hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            HoTen = NormalizeText(HoTen);
            SoDienThoai = NormalizePhone(SoDienThoai);
            Email = NormalizeEmail(Email);
            DiaChi = NormalizeText(DiaChi);
            TrangThai = NormalizeStatus(TrangThai);

            if (string.IsNullOrWhiteSpace(HoTen) ||
                string.IsNullOrWhiteSpace(Email))
            {
                TempData["ToastMessage"] = "Vui lòng nhập họ tên và email.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Edit), new { id });
            }

            if (!IsValidEmailBasic(Email))
            {
                TempData["ToastMessage"] = "Email không hợp lệ.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Edit), new { id });
            }

            if (CustomerEmailExists(Email, id))
            {
                TempData["ToastMessage"] = "Email này đã được khách hàng khác sử dụng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Edit), new { id });
            }

            if (EmployeeEmailExists(Email))
            {
                TempData["ToastMessage"] = "Email này đã được nhân viên sử dụng. Vui lòng dùng email khác.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Edit), new { id });
            }

            if (!string.IsNullOrWhiteSpace(SoDienThoai) && CustomerPhoneExists(SoDienThoai, id))
            {
                TempData["ToastMessage"] = "Số điện thoại này đã được khách hàng khác sử dụng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Edit), new { id });
            }

            customer.HoTen = HoTen;
            customer.SoDienThoai = SoDienThoai;
            customer.Email = Email;
            customer.DiaChi = DiaChi;
            customer.TrangThai = TrangThai;

            try
            {
                _context.SaveChanges();

                TempData["ToastMessage"] = "Cập nhật thông tin khách hàng thành công.";
                TempData["ToastType"] = "success";

                return RedirectToAction(nameof(Detail), new { id = customer.MaKhachHang });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ToastMessage"] = ex.Message;
                TempData["ToastType"] = "danger";
            }
            catch (DbUpdateException)
            {
                TempData["ToastMessage"] = "Email hoặc số điện thoại đã tồn tại. Vui lòng kiểm tra lại.";
                TempData["ToastType"] = "danger";
            }
            catch
            {
                TempData["ToastMessage"] = "Có lỗi xảy ra khi cập nhật khách hàng. Vui lòng thử lại.";
                TempData["ToastType"] = "danger";
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        private bool CustomerEmailExists(string email, int? excludeCustomerId = null)
        {
            email = NormalizeEmail(email);

            return _context.KhachHangs
                .AsNoTracking()
                .Any(k =>
                    (!excludeCustomerId.HasValue || k.MaKhachHang != excludeCustomerId.Value) &&
                    k.Email != null &&
                    k.Email.ToLower() == email);
        }

        private bool CustomerPhoneExists(string phone, int? excludeCustomerId = null)
        {
            phone = NormalizePhone(phone);

            return _context.KhachHangs
                .AsNoTracking()
                .Any(k =>
                    (!excludeCustomerId.HasValue || k.MaKhachHang != excludeCustomerId.Value) &&
                    k.SoDienThoai != null &&
                    k.SoDienThoai == phone);
        }

        private bool EmployeeEmailExists(string email)
        {
            email = NormalizeEmail(email);

            return _context.NhanViens
                .AsNoTracking()
                .Any(nv =>
                    nv.Email != null &&
                    nv.Email.ToLower() == email);
        }

        private static string NormalizeEmail(string? email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? ""
                : email.Trim().ToLowerInvariant();
        }

        private static string NormalizePhone(string? phone)
        {
            return string.IsNullOrWhiteSpace(phone)
                ? ""
                : phone.Trim();
        }

        private static string NormalizeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim();
        }

        private static string NormalizeStatus(string? status)
        {
            status = (status ?? "Hoạt động").Trim();

            if (status == "Ngừng hoạt động")
            {
                return "Ngừng hoạt động";
            }

            return "Hoạt động";
        }

        private static bool IsValidEmailBasic(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return email.Contains("@") &&
                   email.Contains(".") &&
                   !email.Contains(" ");
        }
    }
}