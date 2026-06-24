using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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

            var products = _context.SanPhams.Where(p => p.TrangThai == "Đang bán").ToList();
            var customers = _context.KhachHangs.ToList();
            
            ViewBag.Customers = customers;
            ViewBag.SellerName = HttpContext.Session.GetString("UserName") ?? "Nhân viên";

            return View(products);
        }

        [HttpPost]
        public IActionResult CreateOrderPOS(string customerName, string customerPhone, string paymentMethod, List<int> productIds, List<int> quantities)
        {
            if (productIds == null || productIds.Count == 0)
            {
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

            var newOrder = new DonHang
            {
                MaKhachHang = customer?.MaKhachHang,
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

            _context.SaveChanges();

            TempData["ToastMessage"] = $"Đã thanh toán đơn hàng thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}
