using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
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

            ViewBag.LeftCarouselProducts = products
                .Where(p => p.SoLuongTon > 0)
                .OrderByDescending(p => p.GiaBan)
                .Take(4)
                .ToList();
            ViewBag.RightCarouselProducts = products
                .Where(p => p.SoLuongTon > 0)
                .OrderByDescending(p => p.GiaBan)
                .Skip(4)
                .Take(4)
                .ToList();
            ViewBag.HeroBannerProducts = products
                .Where(p => !string.IsNullOrEmpty(p.HinhAnh))
                .Take(5)
                .ToList();

            return View(products);
        }

        public IActionResult ProductsList(string category, string brand)
        {
            var query = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .Where(p => p.TrangThai == "Đang bán");

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.DanhMuc != null && p.DanhMuc.TenDanhMuc == category);
            }

            if (!string.IsNullOrEmpty(brand))
            {
                query = query.Where(p => p.ThuongHieu != null && p.ThuongHieu.TenThuongHieu == brand);
            }

            ViewBag.SelectedBrand = brand;
            return View(query.ToList());
        }

        public IActionResult Detail(int id)
        {
            var p = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .FirstOrDefault(prod => prod.MaSanPham == id);
                
            if (p == null) return NotFound();

            var favorites = GetFavoritesFromSession();
            ViewBag.IsFavorite = favorites.Contains(id);
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

        private List<int> GetFavoritesFromSession()
        {
            var favoritesJson = HttpContext.Session.GetString("Favorites");
            if (string.IsNullOrEmpty(favoritesJson)) return new List<int>();
            return JsonSerializer.Deserialize<List<int>>(favoritesJson) ?? new List<int>();
        }

        private void SaveFavoritesToSession(List<int> favorites)
        {
            HttpContext.Session.SetString("Favorites", JsonSerializer.Serialize(favorites));
        }

        private int? GetCurrentCustomerId()
        {
            if (int.TryParse(HttpContext.Session.GetString("CustomerId"), out var customerId))
            {
                return customerId;
            }

            var roles = HttpContext.Session.GetString("UserRoles") ?? string.Empty;
            if (roles.Contains("Khách hàng"))
            {
                var email = HttpContext.Session.GetString("UserEmail");
                if (!string.IsNullOrEmpty(email))
                {
                    var lowerEmail = email.ToLower();
                    var customer = _context.KhachHangs.FirstOrDefault(k => k.Email != null && k.Email.ToLower() == lowerEmail);
                    if (customer != null)
                    {
                        HttpContext.Session.SetString("CustomerId", customer.MaKhachHang.ToString());
                        return customer.MaKhachHang;
                    }
                }
            }

            return null;
        }

        private bool IsCustomerLoggedIn()
        {
            return GetCurrentCustomerId() != null;
        }

        private void SetCurrentCustomerSession(KhachHang customer)
        {
            HttpContext.Session.SetString("CustomerId", customer.MaKhachHang.ToString());
            if (!string.IsNullOrEmpty(customer.Email))
            {
                HttpContext.Session.SetString("UserEmail", customer.Email);
            }
            if (!string.IsNullOrEmpty(customer.SoDienThoai))
            {
                HttpContext.Session.SetString("CustomerPhone", customer.SoDienThoai);
            }
        }

        public IActionResult Cart()
        {
            var cart = GetCartFromSession();
            ViewBag.Vouchers = _context.Vouchers.Where(v => v.NgayKetThuc > DateTime.Now).ToList();
            ViewBag.CanCheckout = IsCustomerLoggedIn();
            return View(cart);
        }

        public IActionResult Support(string tab = "warranty", string query = "")
        {
            var model = new SupportViewModel
            {
                ActiveTab = tab ?? "warranty"
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                if (tab == "repair")
                {
                    model.RepairRequest.Query = query;
                }
                else
                {
                    model.WarrantyCheck.Query = query;
                }

                var orderQuery = _context.DonHangs
                    .Include(d => d.KhachHang)
                    .Include(d => d.ChiTietDonHangs!)
                        .ThenInclude(ct => ct.SanPham)
                    .AsQueryable();

                if (int.TryParse(query, out var orderId))
                {
                    orderQuery = orderQuery.Where(d => d.MaDonHang == orderId);
                }
                else
                {
                    orderQuery = orderQuery.Where(d => d.KhachHang != null && d.KhachHang.SoDienThoai != null && d.KhachHang.SoDienThoai == query);
                }

                var order = orderQuery.OrderByDescending(d => d.NgayDat).FirstOrDefault();

                if (order != null)
                {
                    var customerName = order.KhachHang?.HoTen ?? "Khách hàng";
                    var customerPhone = order.KhachHang?.SoDienThoai ?? "Không có";
                    var orderItems = order.ChiTietDonHangs?.ToList() ?? new List<ChiTietDonHang>();

                    if (tab == "repair")
                    {
                        model.RepairRequest.HasAttempted = true;
                        model.RepairRequest.Success = true;
                        model.RepairRequest.Message = "Yêu cầu sửa máy đã được ghi nhận. Chúng tôi sẽ liên hệ lại trong thời gian sớm nhất.";
                        model.RepairRequest.OrderId = order.MaDonHang;
                        model.RepairRequest.CustomerName = customerName;
                        model.RepairRequest.CustomerPhone = customerPhone;
                    }
                    else
                    {
                        model.WarrantyCheck.HasResult = true;
                        model.WarrantyCheck.Found = true;
                        model.WarrantyCheck.OrderId = order.MaDonHang;
                        model.WarrantyCheck.CustomerName = customerName;
                        model.WarrantyCheck.CustomerPhone = customerPhone;
                        model.WarrantyCheck.OrderDate = order.NgayDat;
                        model.WarrantyCheck.TotalAmount = order.TongTien;
                        model.WarrantyCheck.Status = order.TrangThai ?? "Chờ xử lý";
                        model.WarrantyCheck.OrderItems = orderItems;
                    }
                }
                else
                {
                    if (tab == "repair")
                    {
                        model.RepairRequest.HasAttempted = true;
                        model.RepairRequest.Success = false;
                        model.RepairRequest.Message = "Không tìm thấy đơn hàng với mã hoặc số điện thoại này. Vui lòng kiểm tra lại.";
                    }
                    else
                    {
                        model.WarrantyCheck.HasResult = true;
                        model.WarrantyCheck.Found = false;
                        model.WarrantyCheck.Message = "Không tìm thấy đơn hàng với mã hoặc số điện thoại này.";
                    }
                }
            }

            return View("Support", model);
        }

        [HttpPost]
        public IActionResult Support(string tab, string query, string issue)
        {
            var model = new SupportViewModel
            {
                ActiveTab = tab ?? "repair",
                RepairRequest = new RepairRequestModel
                {
                    Query = query ?? string.Empty,
                    Issue = issue ?? string.Empty,
                    HasAttempted = true
                }
            };

            if (string.IsNullOrWhiteSpace(query))
            {
                model.RepairRequest.Success = false;
                model.RepairRequest.Message = "Vui lòng nhập mã đơn hàng hoặc số điện thoại để gửi yêu cầu sửa.";
                return View("Support", model);
            }

            var orderQuery = _context.DonHangs
                .Include(d => d.KhachHang)
                .AsQueryable();

            if (int.TryParse(query, out var orderId))
            {
                orderQuery = orderQuery.Where(d => d.MaDonHang == orderId);
            }
            else
            {
                orderQuery = orderQuery.Where(d => d.KhachHang != null && d.KhachHang.SoDienThoai != null && d.KhachHang.SoDienThoai == query);
            }

            var order = orderQuery.OrderByDescending(d => d.NgayDat).FirstOrDefault();
            if (order == null)
            {
                model.RepairRequest.Success = false;
                model.RepairRequest.Message = "Không tìm thấy đơn hàng với mã hoặc số điện thoại này.";
                return View("Support", model);
            }

            model.RepairRequest.Success = true;
            model.RepairRequest.Message = "Yêu cầu sửa chữa của bạn đã được gửi. Nhân viên kỹ thuật sẽ liên hệ lại sớm.";
            model.RepairRequest.OrderId = order.MaDonHang;
            model.RepairRequest.CustomerName = order.KhachHang?.HoTen ?? "Khách hàng";
            model.RepairRequest.CustomerPhone = order.KhachHang?.SoDienThoai ?? string.Empty;

            return View("Support", model);
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
            var customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                TempData["ToastMessage"] = "Bạn cần đăng nhập bằng tài khoản khách hàng để đặt hàng.";
                TempData["ToastType"] = "info";
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCartFromSession();
            if (cart.Count == 0)
            {
                TempData["ToastMessage"] = "Giỏ hàng rỗng!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Cart");
            }

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerPhone) || string.IsNullOrWhiteSpace(customerAddress))
            {
                TempData["ToastMessage"] = "Vui lòng điền đầy đủ họ tên, số điện thoại và địa chỉ giao hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Cart");
            }

            if (!Regex.IsMatch(customerPhone, "^(0|\\+84)\\d{9,10}$"))
            {
                TempData["ToastMessage"] = "Số điện thoại không hợp lệ. Vui lòng nhập số bắt đầu bằng 0 hoặc +84 và đủ 10-12 chữ số.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Cart");
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");
            var seller = !string.IsNullOrEmpty(userEmail)
                ? _context.NhanViens.FirstOrDefault(e => e.Email == userEmail)
                : null;

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
            else
            {
                if (string.IsNullOrWhiteSpace(customer.DiaChi) && !string.IsNullOrWhiteSpace(customerAddress))
                {
                    customer.DiaChi = customerAddress;
                }
                if (string.IsNullOrWhiteSpace(customer.HoTen) && !string.IsNullOrWhiteSpace(customerName))
                {
                    customer.HoTen = customerName;
                }
                _context.SaveChanges();
            }

            SetCurrentCustomerSession(customer);

            decimal subtotal = cart.Sum(i => i.Total);
            decimal total = Math.Max(0, subtotal - discountVal);

            var order = new DonHang
            {
                MaKhachHang = customer.MaKhachHang,
                MaNhanVien = seller?.MaNhanVien,
                NgayDat = DateTime.Now,
                TongTien = total,
                TrangThai = "Chờ duyệt",
                PhuongThucThanhToan = paymentMethod,
                GhiChu = $"Địa chỉ giao hàng: {customerAddress}. Voucher sử dụng: {voucherCode}. Giảm giá: {discountVal:N0}đ"
            };

            _context.DonHangs.Add(order);
            _context.SaveChanges();

            foreach (var item in cart)
            {
                var product = _context.SanPhams.FirstOrDefault(p => p.MaSanPham == item.ProductId);
                if (product != null)
                {
                    product.SoLuongTon = Math.Max(0, product.SoLuongTon - item.Quantity);
                }

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

        public IActionResult Favorites()
        {
            var favoriteIds = GetFavoritesFromSession();
            var favorites = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .Where(p => favoriteIds.Contains(p.MaSanPham) && p.TrangThai == "Đang bán")
                .ToList();

            return View(favorites);
        }

        [HttpPost]
        public IActionResult AddToFavorites(int id)
        {
            var favoriteIds = GetFavoritesFromSession();
            if (!favoriteIds.Contains(id))
            {
                favoriteIds.Add(id);
                SaveFavoritesToSession(favoriteIds);
            }

            return Json(new { success = true, count = favoriteIds.Count });
        }

        [HttpPost]
        public IActionResult RemoveFromFavorites(int id)
        {
            var favoriteIds = GetFavoritesFromSession();
            if (favoriteIds.Remove(id))
            {
                SaveFavoritesToSession(favoriteIds);
            }

            return Json(new { success = true, count = favoriteIds.Count });
        }

        public IActionResult OrderHistory()
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                TempData["ToastMessage"] = "Bạn cần đăng nhập để xem lịch sử đơn hàng.";
                TempData["ToastType"] = "info";
                return RedirectToAction("Login", "Account");
            }

            var orders = _context.DonHangs
                .Include(o => o.ChiTietDonHangs)
                    .ThenInclude(c => c.SanPham)
                .Where(o => o.MaKhachHang == customerId)
                .OrderByDescending(o => o.NgayDat)
                .ToList();

            return View(orders);
        }

        public IActionResult OrderDetail(int id)
        {
            var order = _context.DonHangs
                .Include(o => o.KhachHang)
                .Include(o => o.ChiTietDonHangs)
                    .ThenInclude(c => c.SanPham)
                .FirstOrDefault(o => o.MaDonHang == id);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}
