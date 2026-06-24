using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DATN64.Controllers
{
    public class OnlineController : Controller
    {
        private readonly AppDbContext _context;

        public OnlineController(AppDbContext context)
        {
            _context = context;
        }

        public class CartItem
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = "";
            public string Image { get; set; } = "";
            public decimal Price { get; set; }
            public decimal OriginalPrice { get; set; }
            public int Quantity { get; set; }
            public bool IsDiscounted { get; set; }
            public decimal Total => Price * Quantity;
        }

        public IActionResult Index()
        {
            var products = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .Where(p => p.TrangThai == "Đang bán")
                .ToList();
            return View(products);
        }

        public IActionResult ProductsList(string category)
        {
            var query = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .Where(p => p.TrangThai == "Đang bán");

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.DanhMuc != null && p.DanhMuc.TenDanhMuc == category);
            }

            return View(query.ToList());
        }

        public IActionResult Detail(int id)
        {
            var p = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .FirstOrDefault(prod => prod.MaSanPham == id);
                
            if (p == null) return NotFound();
            return View(p);
        }

        // ----------------- CART MANAGEMENT -----------------
        private List<CartItem> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson)) return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        public IActionResult Cart()
        {
            var cart = GetCartFromSession();
            ViewBag.Vouchers = _context.Vouchers.Where(v => v.NgayKetThuc > DateTime.Now).ToList();
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int id, int qty = 1)
        {
            var p = _context.SanPhams.FirstOrDefault(prod => prod.MaSanPham == id);
            if (p == null) return NotFound();

            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.ProductId == id);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = p.MaSanPham,
                    Name = p.TenSanPham,
                    Image = p.HinhAnh ?? "",
                    Price = p.GiaBan,
                    OriginalPrice = p.GiaBan,
                    Quantity = qty,
                    IsDiscounted = false
                });
            }
            else
            {
                item.Quantity += qty;
            }

            SaveCartToSession(cart);
            return Json(new { success = true, cartCount = cart.Sum(i => i.Quantity) });
        }

        [HttpPost]
        public IActionResult UpdateCart(int id, int qty)
        {
            if (qty <= 0) return RemoveFromCart(id);

            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                item.Quantity = qty;
                SaveCartToSession(cart);
            }

            return Json(new { success = true, cartCount = cart.Sum(i => i.Quantity), itemTotal = item?.Total.ToString("N0") });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartToSession(cart);
            }

            return RedirectToAction("Cart");
        }

        [HttpPost]
        public IActionResult ApplyVoucher(string code, decimal currentSubtotal)
        {
            var voucher = _context.Vouchers.FirstOrDefault(v => 
                v.MaCode != null && v.MaCode.Equals(code, StringComparison.OrdinalIgnoreCase) && 
                (v.NgayKetThuc == null || v.NgayKetThuc > DateTime.Now) && 
                (v.NgayBatDau == null || v.NgayBatDau <= DateTime.Now));

            if (voucher == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn!" });
            }

            var cart = GetCartFromSession();
            decimal subtotal = cart.Sum(i => i.Total);

            decimal eligibleSubtotal = subtotal;

            decimal discount = voucher.GiaTri ?? 0;
            discount = Math.Min(discount, eligibleSubtotal);

            return Json(new { 
                success = true, 
                discount = discount, 
                message = $"Áp dụng mã thành công! Đã giảm {discount.ToString("N0")} đ" 
            });
        }

        [HttpPost]
        public IActionResult Checkout(string customerName, string customerPhone, string customerAddress, string paymentMethod, string voucherCode, decimal discountVal)
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0)
            {
                TempData["ToastMessage"] = "Giỏ hàng rỗng!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Cart");
            }

            var customer = _context.KhachHangs.FirstOrDefault(k => k.SoDienThoai == customerPhone);
            if (customer == null)
            {
                customer = new KhachHang
                {
                    HoTen = customerName,
                    SoDienThoai = customerPhone,
                    DiaChi = customerAddress,
                    DiemTichLuy = 0
                };
                _context.KhachHangs.Add(customer);
                _context.SaveChanges();
            }

            decimal subtotal = cart.Sum(i => i.Total);
            decimal total = Math.Max(0, subtotal - discountVal);

            var order = new DonHang
            {
                MaKhachHang = customer.MaKhachHang,
                NgayDat = DateTime.Now,
                TongTien = total,
                TrangThai = "Đơn mới",
                PhuongThucThanhToan = paymentMethod,
                GhiChu = $"Địa chỉ giao hàng: {customerAddress}. Voucher sử dụng: {voucherCode}. Giảm giá: {discountVal:N0}đ"
            };

            _context.DonHangs.Add(order);
            _context.SaveChanges();

            foreach (var item in cart)
            {
                var detail = new ChiTietDonHang
                {
                    MaDonHang = order.MaDonHang,
                    MaSanPham = item.ProductId,
                    SoLuong = item.Quantity,
                    DonGia = item.Price
                };
                _context.ChiTietDonHangs.Add(detail);
            }

            if (!string.IsNullOrEmpty(voucherCode))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.MaCode == voucherCode);
                if (voucher != null)
                {
                    var orderVoucher = new DonHang_Voucher
                    {
                        MaDonHang = order.MaDonHang,
                        MaVoucher = voucher.MaVoucher
                    };
                    _context.DonHang_Vouchers.Add(orderVoucher);
                    
                    if (voucher.SoLuong.HasValue && voucher.SoLuong.Value > 0)
                    {
                        voucher.SoLuong--;
                    }
                }
            }

            var notification = new SystemNotification
            {
                Title = "Đơn hàng Website mới",
                Message = $"Khách hàng {customerName} vừa đặt đơn hàng #{order.MaDonHang} trị giá {total.ToString("N0")} đ.",
                Type = "Đơn mới",
                Timestamp = DateTime.Now,
                IsRead = false
            };
            _context.SystemNotifications.Add(notification);

            _context.SaveChanges();

            HttpContext.Session.Remove("Cart");

            TempData["ToastMessage"] = $"Đặt hàng thành công! Mã đơn hàng của bạn: #{order.MaDonHang}";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}
