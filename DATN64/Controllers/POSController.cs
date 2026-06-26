using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using DATN64.Models;
using System.Linq;
using System.Collections.Generic;
using System;

namespace DATN64.Controllers
{
    public class POSController : Controller
    {
        private readonly AppDbContext _context;

        public POSController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var rolesString = HttpContext.Session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!roles.Contains("Super Admin") && !roles.Contains("Admin") && !roles.Contains("Quản lý cửa hàng") && !roles.Contains("Nhân viên bán hàng"))
            {
                return RedirectToAction("Selection", "Portal");
            }

            // Seed default vouchers for testing if none exist
            if (!_context.Vouchers.Any())
            {
                _context.Vouchers.AddRange(new List<Voucher>
                {
                    new Voucher { MaCode = "NOVATECH100K", GiaTri = 100000, SoLuong = 99, NgayBatDau = DateTime.Now.AddDays(-1), NgayKetThuc = DateTime.Now.AddDays(30) },
                    new Voucher { MaCode = "KM50K", GiaTri = 50000, SoLuong = 50, NgayBatDau = DateTime.Now.AddDays(-1), NgayKetThuc = DateTime.Now.AddDays(30) }
                });
                _context.SaveChanges();
            }

            var products = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .Where(p => p.TrangThai == "Đang bán").ToList();
            var customers = _context.KhachHangs.ToList();

            // Lấy nhân viên hiện tại
            var seller = !string.IsNullOrEmpty(userEmail)
                ? _context.NhanViens.FirstOrDefault(e => e.Email == userEmail)
                : null;

            // Kiểm tra trạng thái chấm công hôm nay
            ChamCong? todayChamCong = null;
            if (seller != null)
            {
                todayChamCong = _context.ChamCongs
                    .Where(c => c.MaNhanVien == seller.MaNhanVien
                             && c.NgayCham.Date == DateTime.Today
                             && c.TrangThai == "Đang làm")
                    .OrderByDescending(c => c.GioVao)
                    .FirstOrDefault();
            }

            ViewBag.Customers = customers;
            ViewBag.SellerName = HttpContext.Session.GetString("UserName") ?? "Nhân viên";
            ViewBag.CurrentSeller = seller;
            ViewBag.TodayChamCong = todayChamCong;

            return View(products);
        }

        [HttpGet]
        public IActionResult GetProducts()
        {
            var products = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .Where(p => p.TrangThai == "Đang bán")
                .Select(p => new {
                    id = p.MaSanPham,
                    name = p.TenSanPham,
                    price = p.GiaBan,
                    stock = p.SoLuongTon,
                    image = p.HinhAnh ?? "https://images.unsplash.com/photo-1531297484001-80022131f5a1?q=80&w=300",
                    category = p.DanhMuc != null ? p.DanhMuc.TenDanhMuc : "Khác",
                    brand = p.ThuongHieu != null ? p.ThuongHieu.TenThuongHieu : "Khác",
                    sku = "SP-" + p.MaSanPham.ToString("D4")
                }).ToList();
            return Json(products);
        }

        [HttpGet]
        public IActionResult GetCustomers()
        {
            var customers = _context.KhachHangs.Select(c => new {
                id = c.MaKhachHang,
                name = c.HoTen,
                phone = c.SoDienThoai ?? ""
            }).ToList();
            return Json(customers);
        }

        // ── CHECK IN ─────────────────────────────────────────────────
        [HttpPost]
        public IActionResult CheckIn()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Account");

            var seller = _context.NhanViens.FirstOrDefault(e => e.Email == userEmail);
            if (seller == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy thông tin nhân viên!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            // Kiểm tra đã check in hôm nay chưa (tránh tạo bản ghi trùng)
            bool daCheckIn = _context.ChamCongs.Any(c =>
                c.MaNhanVien == seller.MaNhanVien
                && c.NgayCham.Date == DateTime.Today
                && c.TrangThai == "Đang làm");

            if (daCheckIn)
            {
                TempData["ToastMessage"] = "Bạn đã Check In rồi! Vui lòng Check Out trước khi bắt đầu ca mới.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index");
            }

            var chamCong = new ChamCong
            {
                MaNhanVien = seller.MaNhanVien,
                NgayCham = DateTime.Today,
                GioVao = DateTime.Now,
                TrangThai = "Đang làm"
            };

            _context.ChamCongs.Add(chamCong);
            _context.SaveChanges();

            TempData["ToastMessage"] = $"Check In thành công lúc {DateTime.Now:HH:mm}. Chúc bạn làm việc hiệu quả!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        // ── CHECK OUT ────────────────────────────────────────────────
        [HttpPost]
        public IActionResult CheckOut()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Account");

            var seller = _context.NhanViens.FirstOrDefault(e => e.Email == userEmail);
            if (seller == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy thông tin nhân viên!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            var chamCong = _context.ChamCongs
                .Where(c => c.MaNhanVien == seller.MaNhanVien
                         && c.NgayCham.Date == DateTime.Today
                         && c.TrangThai == "Đang làm")
                .OrderByDescending(c => c.GioVao)
                .FirstOrDefault();

            if (chamCong == null)
            {
                TempData["ToastMessage"] = "Bạn chưa Check In hôm nay!";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index");
            }

            chamCong.GioRa = DateTime.Now;
            chamCong.TrangThai = "Hoàn thành";

            if (chamCong.GioVao.HasValue)
            {
                chamCong.TongGioLam = Math.Round((chamCong.GioRa.Value - chamCong.GioVao.Value).TotalHours, 2);
            }

            _context.SaveChanges();

            TempData["ToastMessage"] = $"Check Out thành công! Tổng giờ làm hôm nay: {chamCong.TongGioLam:F1} giờ.";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ApplyVoucher(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã voucher!" });
            }

            var voucher = _context.Vouchers.FirstOrDefault(v => v.MaCode == code);
            if (voucher == null)
            {
                return Json(new { success = false, message = "Mã voucher không tồn tại!" });
            }

            if (voucher.SoLuong.HasValue && voucher.SoLuong <= 0)
            {
                return Json(new { success = false, message = "Voucher này đã hết lượt sử dụng!" });
            }

            if (voucher.NgayBatDau.HasValue && voucher.NgayBatDau > DateTime.Now)
            {
                return Json(new { success = false, message = "Voucher chưa đến thời gian áp dụng!" });
            }

            if (voucher.NgayKetThuc.HasValue && voucher.NgayKetThuc < DateTime.Now)
            {
                return Json(new { success = false, message = "Voucher đã hết hạn!" });
            }

            return Json(new { 
                success = true, 
                discount = voucher.GiaTri ?? 0, 
                message = $"Áp dụng mã {code} thành công!" 
            });
        }

        [HttpPost]
        public IActionResult CreateOrderPOS(string customerName, string customerPhone, string paymentMethod, List<int> productIds, List<int> quantities, string? voucherCode)
        {
            if (productIds == null || productIds.Count == 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { message = "Vui lòng chọn ít nhất 1 sản phẩm!" });
                }
                TempData["ToastMessage"] = "Vui lòng chọn ít nhất 1 sản phẩm!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            // Find or create customer
            var customer = _context.KhachHangs.FirstOrDefault(c => c.SoDienThoai == customerPhone);
            if (customer == null && !string.IsNullOrEmpty(customerPhone))
            {
                customer = new KhachHang 
                { 
                    HoTen = string.IsNullOrEmpty(customerName) ? "Khách Hàng Vãng Lai" : customerName,
                    SoDienThoai = customerPhone,
                    DiemTichLuy = 0
                };
                _context.KhachHangs.Add(customer);
                _context.SaveChanges();
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");
            var seller = !string.IsNullOrEmpty(userEmail)
                ? _context.NhanViens.FirstOrDefault(e => e.Email == userEmail)
                : null;

            var newOrder = new DonHang
            {
                MaKhachHang = customer?.MaKhachHang,
                MaNhanVien = seller?.MaNhanVien,
                NgayDat = DateTime.Now,
                TrangThai = "Hoàn thành",
                TongTien = 0, // Will calculate below
                GhiChu = "Đơn mua tại quầy POS",
                PhuongThucThanhToan = paymentMethod,
                ChiTietDonHangs = new List<ChiTietDonHang>()
            };

            decimal total = 0;

            for (int i = 0; i < productIds.Count; i++)
            {
                var prod = _context.SanPhams.FirstOrDefault(p => p.MaSanPham == productIds[i]);
                if (prod != null)
                {
                    int qty = quantities[i];
                    prod.SoLuongTon -= qty; // Cập nhật kho
                    
                    var itemTotal = qty * prod.GiaBan;
                    total += itemTotal;

                    newOrder.ChiTietDonHangs.Add(new ChiTietDonHang
                    {
                        MaSanPham = prod.MaSanPham,
                        SoLuong = qty,
                        DonGia = prod.GiaBan
                    });
                }
            }

            // Apply Voucher
            Voucher? appliedVoucher = null;
            if (!string.IsNullOrEmpty(voucherCode))
            {
                appliedVoucher = _context.Vouchers.FirstOrDefault(v => v.MaCode == voucherCode);
                if (appliedVoucher != null && 
                    (!appliedVoucher.SoLuong.HasValue || appliedVoucher.SoLuong > 0) &&
                    (!appliedVoucher.NgayBatDau.HasValue || appliedVoucher.NgayBatDau <= DateTime.Now) &&
                    (!appliedVoucher.NgayKetThuc.HasValue || appliedVoucher.NgayKetThuc >= DateTime.Now))
                {
                    decimal discount = appliedVoucher.GiaTri ?? 0;
                    total = Math.Max(0, total - discount);

                    if (appliedVoucher.SoLuong.HasValue && appliedVoucher.SoLuong > 0)
                    {
                        appliedVoucher.SoLuong--;
                    }
                }
            }

            newOrder.TongTien = total;
            _context.DonHangs.Add(newOrder);

            // Gửi thông báo
            _context.SystemNotifications.Add(new SystemNotification
            {
                Title = "Đơn POS mới",
                Message = $"Đơn hàng trị giá {newOrder.TongTien:N0} đ vừa được thanh toán tại quầy.",
                Type = "Đơn mới",
                Timestamp = DateTime.Now
            });

            _context.SaveChanges(); // Save to generate newOrder.MaDonHang

            if (appliedVoucher != null)
            {
                var dhVoucher = new DonHang_Voucher
                {
                    MaDonHang = newOrder.MaDonHang,
                    MaVoucher = appliedVoucher.MaVoucher
                };
                _context.DonHang_Vouchers.Add(dhVoucher);
                _context.SaveChanges();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = true, 
                    message = "Đã thanh toán đơn hàng thành công!", 
                    orderCode = "POS-" + newOrder.MaDonHang 
                });
            }

            TempData["ToastMessage"] = $"Đã thanh toán đơn hàng thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}
