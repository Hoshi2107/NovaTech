using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DATN64.Controllers
{
    public class OnlineController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GeminiService _geminiService;
        private readonly OpenAIService _openAIService;

        public OnlineController(AppDbContext context, GeminiService geminiService, OpenAIService openAIService)
        {
            _context = context;
            _geminiService = geminiService;
            _openAIService = openAIService;
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

            var activeProducts = products.Where(p => p.SoLuongTon > 0).ToList();

            ViewBag.LeftCarouselProducts = activeProducts
                .OrderByDescending(p => p.GiaBan)
                .Take(4)
                .ToList();

            ViewBag.RightCarouselProducts = activeProducts
                .OrderBy(p => p.GiaBan)
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

            string series = "";

            if (p.TenSanPham.Contains("iPhone 15", StringComparison.OrdinalIgnoreCase))
            {
                series = "iPhone 15";
            }
            else if (p.TenSanPham.Contains("Samsung Galaxy S24", StringComparison.OrdinalIgnoreCase))
            {
                series = "Samsung Galaxy S24";
            }

            if (!string.IsNullOrEmpty(series))
            {
                ViewBag.RelatedVariants = _context.SanPhams
                    .Where(prod => prod.TenSanPham.Contains(series) &&
                                   (prod.TrangThai == "Đang bán" || prod.TrangThai == "Biến thể"))
                    .ToList();
            }
            else
            {
                ViewBag.RelatedVariants = new List<SanPham> { p };
            }

            return View(p);
        }

        private string GetLoginName()
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (!string.IsNullOrWhiteSpace(userName))
            {
                return userName.Trim();
            }

            return "Khách hàng";
        }

        private string GetLoginEmail()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (!string.IsNullOrWhiteSpace(userEmail) && userEmail.Contains("@"))
            {
                return userEmail.Trim();
            }

            return "";
        }

        // ----------------- CART MANAGEMENT -----------------

        private List<CartItem> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");

            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        private List<int> GetFavoritesFromSession()
        {
            var customerId = GetCurrentCustomerId();

            if (customerId != null)
            {
                var isLoaded = HttpContext.Session.GetString("FavoritesLoaded");

                if (string.IsNullOrEmpty(isLoaded))
                {
                    var dbFavorites = _context.YeuThichs
                        .Where(y => y.MaKhachHang == customerId.Value)
                        .Select(y => y.MaSanPham)
                        .ToList();

                    var anonFavoritesJson = HttpContext.Session.GetString("Favorites");

                    if (!string.IsNullOrEmpty(anonFavoritesJson))
                    {
                        var sessionFavorites = JsonSerializer.Deserialize<List<int>>(anonFavoritesJson) ?? new List<int>();
                        var merged = dbFavorites.Union(sessionFavorites).ToList();

                        SaveFavoritesToSession(merged);
                        HttpContext.Session.SetString("FavoritesLoaded", "true");

                        return merged;
                    }

                    HttpContext.Session.SetString("Favorites", JsonSerializer.Serialize(dbFavorites));
                    HttpContext.Session.SetString("FavoritesLoaded", "true");

                    return dbFavorites;
                }
            }

            var favoritesJson = HttpContext.Session.GetString("Favorites");

            if (string.IsNullOrEmpty(favoritesJson))
            {
                return new List<int>();
            }

            return JsonSerializer.Deserialize<List<int>>(favoritesJson) ?? new List<int>();
        }

        private void SaveFavoritesToSession(List<int> favorites)
        {
            HttpContext.Session.SetString("Favorites", JsonSerializer.Serialize(favorites));

            var customerId = GetCurrentCustomerId();

            if (customerId != null)
            {
                var existing = _context.YeuThichs
                    .Where(y => y.MaKhachHang == customerId.Value)
                    .ToList();

                var toRemove = existing.Where(e => !favorites.Contains(e.MaSanPham)).ToList();

                if (toRemove.Any())
                {
                    _context.YeuThichs.RemoveRange(toRemove);
                }

                var existingProdIds = existing.Select(e => e.MaSanPham).ToList();

                var toAdd = favorites
                    .Where(id => !existingProdIds.Contains(id))
                    .Select(id => new YeuThich
                    {
                        MaKhachHang = customerId.Value,
                        MaSanPham = id,
                        NgayTao = DateTime.Now
                    })
                    .ToList();

                if (toAdd.Any())
                {
                    _context.YeuThichs.AddRange(toAdd);
                }

                _context.SaveChanges();
            }
        }

        private int? GetCurrentCustomerId()
        {
            var loginName = GetLoginName();
            var loginEmail = GetLoginEmail();

            KhachHang? customer = null;

            if (int.TryParse(HttpContext.Session.GetString("CustomerId"), out var customerId))
            {
                customer = _context.KhachHangs
                    .FirstOrDefault(k => k.MaKhachHang == customerId);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(loginEmail))
            {
                var lowerEmail = loginEmail.ToLower();

                customer = _context.KhachHangs
                    .FirstOrDefault(k => k.Email != null && k.Email.ToLower() == lowerEmail);
            }

            if (customer == null)
            {
                if (string.IsNullOrWhiteSpace(loginEmail))
                {
                    return null;
                }

                customer = new KhachHang
                {
                    HoTen = loginName,
                    Email = loginEmail,
                    SoDienThoai = "0900000000",
                    DiaChi = "Chưa cập nhật",
                    DiemTichLuy = 0,
                    TrangThai = "Hoạt động",
                    NgayTao = DateTime.Now
                };

                _context.KhachHangs.Add(customer);
                _context.SaveChanges();
            }
            else
            {
                var changed = false;

                if (!string.IsNullOrWhiteSpace(loginName) && customer.HoTen != loginName)
                {
                    customer.HoTen = loginName;
                    changed = true;
                }

                if (!string.IsNullOrWhiteSpace(loginEmail) && customer.Email != loginEmail)
                {
                    customer.Email = loginEmail;
                    changed = true;
                }

                if (changed)
                {
                    _context.SaveChanges();
                }
            }

            HttpContext.Session.SetString("CustomerId", customer.MaKhachHang.ToString());

            return customer.MaKhachHang;
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

            var customerId = GetCurrentCustomerId();
            KhachHang? customer = null;

            if (customerId != null)
            {
                customer = _context.KhachHangs
                    .FirstOrDefault(k => k.MaKhachHang == customerId.Value);
            }

            ViewBag.Vouchers = _context.Vouchers
                .Where(v => v.NgayKetThuc > DateTime.Now
                         && (v.SoLuong == null || v.SoLuong > 0)
                         && (v.NgayBatDau == null || v.NgayBatDau <= DateTime.Now))
                .OrderBy(v => v.GiaTri)
                .ToList();

            ViewBag.CanCheckout = customerId != null;

            ViewBag.CustomerName = GetLoginName();
            ViewBag.CustomerEmail = GetLoginEmail();
            ViewBag.CustomerPhone = customer?.SoDienThoai ?? "";
            ViewBag.CustomerAddress = customer?.DiaChi ?? "";

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

                var cleanQuery = query.Trim();

                if (cleanQuery.StartsWith("#"))
                {
                    cleanQuery = cleanQuery.Substring(1);
                }

                if (int.TryParse(cleanQuery, out var orderId) && cleanQuery.Length < 7)
                {
                    orderQuery = orderQuery.Where(d => d.MaDonHang == orderId);
                }
                else
                {
                    orderQuery = orderQuery.Where(d =>
                        d.KhachHang != null &&
                        d.KhachHang.SoDienThoai != null &&
                        d.KhachHang.SoDienThoai == cleanQuery);
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
        public IActionResult Support(string tab, string query, string issue, string deviceType, string preferredTime, string priority)
        {
            var model = new SupportViewModel
            {
                ActiveTab = tab ?? "repair",
                RepairRequest = new RepairRequestModel
                {
                    Query = query ?? string.Empty,
                    Issue = issue ?? string.Empty,
                    DeviceType = deviceType ?? string.Empty,
                    PreferredTime = preferredTime ?? string.Empty,
                    Priority = priority ?? string.Empty,
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

            var cleanQuery = query.Trim();

            if (cleanQuery.StartsWith("#"))
            {
                cleanQuery = cleanQuery.Substring(1);
            }

            if (int.TryParse(cleanQuery, out var orderId) && cleanQuery.Length < 7)
            {
                orderQuery = orderQuery.Where(d => d.MaDonHang == orderId);
            }
            else
            {
                orderQuery = orderQuery.Where(d =>
                    d.KhachHang != null &&
                    d.KhachHang.SoDienThoai != null &&
                    d.KhachHang.SoDienThoai == cleanQuery);
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

            if (p.SoLuongTon <= 0)
            {
                return Json(new { success = false, message = "Sản phẩm này đã hết hàng!" });
            }

            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.ProductId == id);
            int currentQtyInCart = item?.Quantity ?? 0;

            if (currentQtyInCart + qty > p.SoLuongTon)
            {
                return Json(new
                {
                    success = false,
                    message = $"Số lượng yêu cầu vượt quá tồn kho khả dụng ({p.SoLuongTon} sản phẩm)!"
                });
            }

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

            var p = _context.SanPhams.FirstOrDefault(prod => prod.MaSanPham == id);

            if (p == null) return NotFound();

            if (qty > p.SoLuongTon)
            {
                return Json(new
                {
                    success = false,
                    message = $"Số lượng yêu cầu vượt quá tồn kho khả dụng ({p.SoLuongTon} sản phẩm)!"
                });
            }

            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.ProductId == id);

            if (item != null)
            {
                item.Quantity = qty;
                SaveCartToSession(cart);
            }

            return Json(new
            {
                success = true,
                cartCount = cart.Sum(i => i.Quantity),
                itemTotal = item?.Total.ToString("N0")
            });
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
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá!" });
            }

            var voucher = _context.Vouchers.FirstOrDefault(v =>
                v.MaCode != null && v.MaCode.ToLower() == code.Trim().ToLower());

            if (voucher == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại!" });
            }

            if (voucher.SoLuong.HasValue && voucher.SoLuong <= 0)
            {
                return Json(new { success = false, message = "Mã giảm giá này đã hết lượt sử dụng!" });
            }

            if (voucher.NgayBatDau.HasValue && voucher.NgayBatDau > DateTime.Now)
            {
                return Json(new { success = false, message = "Mã giảm giá chưa đến thời gian áp dụng!" });
            }

            if (voucher.NgayKetThuc.HasValue && voucher.NgayKetThuc < DateTime.Now)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn!" });
            }

            var cart = GetCartFromSession();

            decimal eligibleSubtotal = cart
                .Where(i => !i.IsDiscounted)
                .Sum(i => i.Total);

            decimal voucherValue = voucher.GiaTri ?? 0;

            if (eligibleSubtotal <= 0)
            {
                return Json(new { success = false, message = "Mã giảm giá không áp dụng cho sản phẩm Flash Sale!" });
            }

            decimal discount = Math.Min(voucherValue, eligibleSubtotal);

            return Json(new
            {
                success = true,
                discount = discount,
                message = $"Áp dụng mã <strong>{voucher.MaCode}</strong> thành công! Giảm <strong>{discount.ToString("N0")}đ</strong>"
            });
        }

        [HttpPost]
        public IActionResult Checkout(
            string customerName,
            string customerPhone,
            string customerAddress,
            string paymentMethod,
            string voucherCode,
            decimal discountVal)
        {
            var customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                TempData["ToastMessage"] = "Bạn cần đăng nhập bằng tài khoản khách hàng để đặt hàng.";
                TempData["ToastType"] = "info";
                return RedirectToAction("Login", "Account");
            }

            var loginName = GetLoginName();
            var loginEmail = GetLoginEmail();

            customerName = loginName;

            var cart = GetCartFromSession();

            if (cart.Count == 0)
            {
                TempData["ToastMessage"] = "Giỏ hàng rỗng!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Cart");
            }

            if (string.IsNullOrWhiteSpace(customerPhone) || string.IsNullOrWhiteSpace(customerAddress))
            {
                TempData["ToastMessage"] = "Vui lòng điền đầy đủ số điện thoại và địa chỉ giao hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Cart");
            }

            if (!Regex.IsMatch(customerPhone, "^(0|\\+84)\\d{9,10}$"))
            {
                TempData["ToastMessage"] = "Số điện thoại không hợp lệ. Vui lòng nhập số bắt đầu bằng 0 hoặc +84 và đủ 10-12 chữ số.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Cart");
            }

            var sessionEmail = HttpContext.Session.GetString("UserEmail");

            var seller = !string.IsNullOrEmpty(sessionEmail)
                ? _context.NhanViens.FirstOrDefault(e => e.Email == sessionEmail)
                : null;

            var customer = _context.KhachHangs
                .FirstOrDefault(k => k.MaKhachHang == customerId.Value);

            if (customer == null)
            {
                customer = new KhachHang
                {
                    HoTen = loginName,
                    Email = loginEmail,
                    SoDienThoai = customerPhone,
                    DiaChi = customerAddress,
                    DiemTichLuy = 0,
                    TrangThai = "Hoạt động",
                    NgayTao = DateTime.Now
                };

                _context.KhachHangs.Add(customer);
                _context.SaveChanges();

                HttpContext.Session.SetString("CustomerId", customer.MaKhachHang.ToString());
            }
            else
            {
                customer.HoTen = loginName;

                if (!string.IsNullOrWhiteSpace(loginEmail))
                {
                    customer.Email = loginEmail;
                }

                customer.SoDienThoai = customerPhone;
                customer.DiaChi = customerAddress;

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
                var product = _context.SanPhams
                    .FirstOrDefault(p => p.MaSanPham == item.ProductId);

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

            if (paymentMethod == "Chuyển khoản")
            {
                return RedirectToAction("OrderPaymentQR", new { id = order.MaDonHang });
            }

            return RedirectToAction("Index");
        }

        public IActionResult OrderPaymentQR(int id)
        {
            var order = _context.DonHangs
                .Include(o => o.KhachHang)
                .Include(o => o.ChiTietDonHangs)
                .ThenInclude(c => c.SanPham)
                .FirstOrDefault(o => o.MaDonHang == id);

            if (order == null) return NotFound();

            string bankId = "vietinbank";
            string accountNo = "108602210708";
            string accountName = "CONG TY TNHH NOVATECH";
            string amount = ((long)order.TongTien).ToString();
            string addInfo = Uri.EscapeDataString($"NovaTech thanh toan don hang {order.MaDonHang}");
            string formattedAccountName = Uri.EscapeDataString(accountName);

            string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={amount}&addInfo={addInfo}&accountName={formattedAccountName}";

            ViewBag.QRUrl = qrUrl;
            ViewBag.AccountNo = accountNo;
            ViewBag.AccountName = accountName;
            ViewBag.BankName = "Ngân hàng TMCP Công Thương Việt Nam (VietinBank)";

            return View(order);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int id, string[] reasons, string otherReason)
        {
            var customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                TempData["ToastMessage"] = "Bạn cần đăng nhập để thực hiện thao tác này.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "Account");
            }

            var order = _context.DonHangs
                .Include(o => o.ChiTietDonHangs)
                .ThenInclude(c => c.SanPham)
                .FirstOrDefault(o => o.MaDonHang == id && o.MaKhachHang == customerId);

            if (order == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy đơn hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("OrderHistory");
            }

            var status = order.TrangThai?.Trim();

            if (status != "Chờ duyệt" &&
                status != "Đơn mới" &&
                status != "Chờ thanh toán" &&
                !string.IsNullOrEmpty(status))
            {
                TempData["ToastMessage"] = "Không thể hủy đơn hàng ở trạng thái hiện tại.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("OrderDetail", new { id = id });
            }

            var selectedReasons = new List<string>();

            if (reasons != null && reasons.Length > 0)
            {
                selectedReasons.AddRange(reasons);
            }

            if (!string.IsNullOrWhiteSpace(otherReason))
            {
                selectedReasons.Add(otherReason);
            }

            string cancelReasonText = string.Join("; ", selectedReasons);

            if (string.IsNullOrWhiteSpace(cancelReasonText))
            {
                cancelReasonText = "Không có lý do cụ thể.";
            }

            order.TrangThai = "Đã hủy";
            order.GhiChu = (string.IsNullOrEmpty(order.GhiChu) ? "" : order.GhiChu + "\n")
                           + $"Lý do hủy: {cancelReasonText} (Hủy lúc {DateTime.Now:dd/MM/yyyy HH:mm})";

            if (order.ChiTietDonHangs != null)
            {
                foreach (var item in order.ChiTietDonHangs)
                {
                    if (item.SanPham != null)
                    {
                        item.SanPham.SoLuongTon = item.SanPham.SoLuongTon + item.SoLuong;
                    }
                }
            }

            var customerName = _context.KhachHangs
                .FirstOrDefault(k => k.MaKhachHang == customerId)?.HoTen ?? "Khách hàng";

            var notification = new SystemNotification
            {
                Title = "Đơn hàng đã bị hủy",
                Message = $"Khách hàng {customerName} đã hủy đơn hàng #{order.MaDonHang}. Lý do: {cancelReasonText}",
                Type = "Đơn mới",
                Timestamp = DateTime.Now,
                IsRead = false
            };

            _context.SystemNotifications.Add(notification);
            _context.SaveChanges();

            TempData["ToastMessage"] = "Hủy đơn hàng thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("OrderDetail", new { id = id });
        }

        // ==================== CUSTOMER PROFILE ====================

        public IActionResult Profile()
        {
            var customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                TempData["ToastMessage"] = "Bạn cần đăng nhập để xem trang cá nhân.";
                TempData["ToastType"] = "info";
                return RedirectToAction("Login", "Account");
            }

            var customer = _context.KhachHangs
                .Include(k => k.DonHangs)
                .ThenInclude(d => d.ChiTietDonHangs)
                .ThenInclude(c => c.SanPham)
                .FirstOrDefault(k => k.MaKhachHang == customerId);

            if (customer == null) return NotFound();

            var model = new ProfileViewModel
            {
                MaKhachHang = customer.MaKhachHang,
                HoTen = customer.HoTen,
                SoDienThoai = customer.SoDienThoai,
                Email = customer.Email,
                DiaChi = customer.DiaChi,
                DiemTichLuy = customer.DiemTichLuy,
                TrangThai = customer.TrangThai,
                NgayTao = customer.NgayTao,
                DonHangs = customer.DonHangs
                    .OrderByDescending(d => d.NgayDat)
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Profile(ProfileViewModel model, string activeTab = "info")
        {
            var customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = _context.KhachHangs.FirstOrDefault(k => k.MaKhachHang == customerId);

            if (customer == null) return NotFound();

            if (activeTab == "info")
            {
                if (string.IsNullOrWhiteSpace(model.HoTen))
                {
                    TempData["ToastMessage"] = "Họ tên không được để trống.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("Profile");
                }

                if (!string.IsNullOrWhiteSpace(model.Email) &&
                    model.Email.ToLower() != (customer.Email ?? "").ToLower())
                {
                    var emailTaken = _context.KhachHangs
                        .Any(k => k.Email != null &&
                                  k.Email.ToLower() == model.Email.ToLower() &&
                                  k.MaKhachHang != customerId);

                    if (emailTaken)
                    {
                        TempData["ToastMessage"] = "Email này đã được sử dụng bởi tài khoản khác.";
                        TempData["ToastType"] = "danger";
                        return RedirectToAction("Profile");
                    }
                }

                customer.HoTen = model.HoTen.Trim();
                customer.SoDienThoai = model.SoDienThoai?.Trim();
                customer.DiaChi = model.DiaChi?.Trim();

                if (!string.IsNullOrWhiteSpace(model.Email))
                {
                    customer.Email = model.Email.Trim();
                    HttpContext.Session.SetString("UserEmail", customer.Email);
                }

                HttpContext.Session.SetString("UserName", customer.HoTen);

                if (!string.IsNullOrWhiteSpace(customer.SoDienThoai))
                {
                    HttpContext.Session.SetString("CustomerPhone", customer.SoDienThoai);
                }

                _context.SaveChanges();

                TempData["ToastMessage"] = "Cập nhật thông tin thành công!";
                TempData["ToastType"] = "success";
            }
            else if (activeTab == "password")
            {
                if (string.IsNullOrWhiteSpace(model.MatKhauCu) ||
                    string.IsNullOrWhiteSpace(model.MatKhauMoi) ||
                    string.IsNullOrWhiteSpace(model.XacNhanMatKhau))
                {
                    TempData["ToastMessage"] = "Vui lòng điền đầy đủ thông tin đổi mật khẩu.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("Profile");
                }

                if (customer.MatKhau != model.MatKhauCu)
                {
                    TempData["ToastMessage"] = "Mật khẩu cũ không đúng.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("Profile");
                }

                if (model.MatKhauMoi != model.XacNhanMatKhau)
                {
                    TempData["ToastMessage"] = "Mật khẩu mới và xác nhận không khớp.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("Profile");
                }

                if (model.MatKhauMoi!.Length < 6)
                {
                    TempData["ToastMessage"] = "Mật khẩu mới phải ít nhất 6 ký tự.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("Profile");
                }

                customer.MatKhau = model.MatKhauMoi;
                _context.SaveChanges();

                TempData["ToastMessage"] = "Đổi mật khẩu thành công! Lần đăng nhập sau hãy dùng mật khẩu mới.";
                TempData["ToastType"] = "success";
            }

            return RedirectToAction("Profile");
        }

        // ==================== AI CONSULTANT ====================

        [HttpPost]
        public async Task<IActionResult> ChatConsult([FromBody] OnlineChatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Message))
            {
                return BadRequest(new { error = "Tin nhắn trống" });
            }

            var customerQuestion = req.Message.Trim();

            if (IsClearlyUnsupportedOrOffTopic(customerQuestion))
            {
                return Ok(new
                {
                    response = "Dạ hiện tại NovaTech chỉ hỗ trợ tư vấn các sản phẩm công nghệ như laptop, điện thoại, linh kiện, phụ kiện, thiết bị điện tử và khuyến mãi liên quan. Sản phẩm anh/chị hỏi hiện không thuộc ngành hàng NovaTech đang kinh doanh nên em chưa thể gợi ý sản phẩm phù hợp ạ.",
                    products = Array.Empty<object>(),
                    vouchers = Array.Empty<object>(),
                    needsHumanSupport = false
                });
            }

            var shouldSuggestProducts = HasProductConsultIntent(customerQuestion);

            if (!shouldSuggestProducts)
            {
                return Ok(new
                {
                    response = "Dạ em là trợ lý AI tư vấn mua sắm công nghệ của NovaTech. Em có thể hỗ trợ anh/chị chọn laptop, điện thoại, phụ kiện, linh kiện, thiết bị điện tử hoặc voucher khuyến mãi. Anh/chị cho em biết nhu cầu mua sản phẩm công nghệ để em tư vấn phù hợp hơn ạ.",
                    products = Array.Empty<object>(),
                    vouchers = Array.Empty<object>(),
                    needsHumanSupport = false
                });
            }

            var allProducts = await _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .Where(p => p.TrangThai == "Đang bán" && p.SoLuongTon > 0)
                .OrderByDescending(p => p.SoLuongTon)
                .Select(p => new AiProductContext
                {
                    Id = p.MaSanPham,
                    Name = p.TenSanPham,
                    Price = p.GiaBan,
                    Description = p.MoTa ?? "",
                    Category = p.DanhMuc != null ? p.DanhMuc.TenDanhMuc : "Khác",
                    Brand = p.ThuongHieu != null ? p.ThuongHieu.TenThuongHieu : "Khác",
                    Stock = p.SoLuongTon,
                    Image = p.HinhAnh ?? ""
                })
                .Take(100)
                .ToListAsync();

            var candidateProducts = ApplyProductHardFilters(customerQuestion, allProducts);

            if (!candidateProducts.Any())
            {
                return Ok(new
                {
                    response = BuildNoCandidateMessage(customerQuestion),
                    products = Array.Empty<object>(),
                    vouchers = Array.Empty<object>(),
                    needsHumanSupport = false
                });
            }

            var now = DateTime.Now;

            var activeVouchers = await _context.Vouchers
                .Where(v => v.MaCode != null
                         && (v.NgayBatDau == null || v.NgayBatDau <= now)
                         && (v.NgayKetThuc == null || v.NgayKetThuc > now)
                         && (v.SoLuong == null || v.SoLuong > 0))
                .OrderByDescending(v => v.GiaTri ?? 0)
                .Select(v => new AiVoucherContext
                {
                    Code = v.MaCode ?? "",
                    Value = v.GiaTri ?? 0,
                    Quantity = v.SoLuong,
                    EndDate = v.NgayKetThuc
                })
                .Take(10)
                .ToListAsync();

            string productContext = candidateProducts.Any()
                ? string.Join("\n", candidateProducts.Select(p =>
                    $"- ID:{p.Id} | {p.Name} | Giá:{p.Price:N0}đ | Danh mục:{p.Category} | Thương hiệu:{p.Brand} | Tồn:{p.Stock} | Mô tả:{LimitText(p.Description, 160)}"))
                : "Không có sản phẩm nào phù hợp với ngân sách hoặc điều kiện khách yêu cầu.";

            string voucherContext = activeVouchers.Any()
                ? string.Join("\n", activeVouchers.Select(v =>
                    $"- Mã:{v.Code} | Giảm:{v.Value:N0}đ | Số lượng còn:{(v.Quantity.HasValue ? v.Quantity.Value.ToString() : "Không giới hạn")} | Hạn:{(v.EndDate.HasValue ? v.EndDate.Value.ToString("dd/MM/yyyy") : "Không giới hạn")}"))
                : "Hiện chưa có voucher/khuyến mãi đang hiệu lực.";

            string systemInstruction = $@"Bạn là AI tư vấn bán hàng và chăm sóc khách hàng của NovaTech - cửa hàng công nghệ.

Nhiệm vụ:
- Tư vấn trực tiếp cho khách hàng bằng tiếng Việt lịch sự, dễ hiểu, có kính ngữ 'dạ', 'anh/chị'.
- Phân tích nhu cầu của khách: ngân sách, mục đích sử dụng, thương hiệu mong muốn, cấu hình, khuyến mãi.
- Chỉ được gợi ý sản phẩm có trong DANH SÁCH SẢN PHẨM THỰC TẾ.
- Nếu khách hỏi sản phẩm không thuộc ngành hàng công nghệ hoặc NovaTech không kinh doanh, hãy lịch sự từ chối tư vấn sản phẩm đó, recommendedProductIds = [], voucherCodes = [].
- Chỉ gợi ý sản phẩm khi câu hỏi liên quan đến laptop, điện thoại, linh kiện, phụ kiện, thiết bị điện tử hoặc nhu cầu mua hàng công nghệ.
- Nếu khách có ngân sách rõ ràng, tuyệt đối không gợi ý sản phẩm vượt ngân sách đã lọc trong danh sách.
- Nếu có voucher phù hợp, gợi ý mã voucher trong DANH SÁCH VOUCHER.
- Không tự bịa giá, không bịa khuyến mãi, không bịa sản phẩm ngoài dữ liệu.

BẮT BUỘC trả về JSON object hợp lệ, không markdown code fence, đúng cấu trúc:
{{
  ""message"": ""Câu trả lời tư vấn cho khách. Có thể dùng **in đậm** và gạch đầu dòng markdown."",
  ""recommendedProductIds"": [1, 2, 3],
  ""voucherCodes"": [""NOVA10""],
  ""needsHumanSupport"": false
}}

Quy tắc JSON:
- recommendedProductIds tối đa 3 ID sản phẩm, chỉ lấy ID có trong danh sách.
- voucherCodes tối đa 2 mã, chỉ lấy mã có trong danh sách.
- Nếu không có sản phẩm phù hợp thì recommendedProductIds = [].
- Nếu không có voucher phù hợp thì voucherCodes = [].
- needsHumanSupport = true nếu khách cần bảo hành, khiếu nại, đổi trả phức tạp hoặc yêu cầu gặp nhân viên.

=== DANH SÁCH SẢN PHẨM THỰC TẾ ĐÃ ĐƯỢC LỌC THEO NHU CẦU/KHOẢNG GIÁ ===
{productContext}

=== DANH SÁCH VOUCHER/KHUYẾN MÃI ĐANG HIỆU LỰC ===
{voucherContext}";

            var sessionKey = "ConsultChatHistory";
            var historyJson = HttpContext.Session.GetString(sessionKey);
            var history = new List<ConsultMessage>();

            if (!string.IsNullOrEmpty(historyJson))
            {
                try
                {
                    history = JsonSerializer.Deserialize<List<ConsultMessage>>(historyJson) ?? new List<ConsultMessage>();
                }
                catch
                {
                    history = new List<ConsultMessage>();
                }
            }

            if (history.Count > 8)
            {
                history = history.Skip(history.Count - 8).ToList();
            }

            var promptBuilder = new System.Text.StringBuilder();

            if (history.Any())
            {
                promptBuilder.AppendLine("Lịch sử hội thoại gần nhất:");

                foreach (var msg in history)
                {
                    promptBuilder.AppendLine($"{(msg.Sender == "user" ? "Khách hàng" : "AI")}: {msg.Text}");
                }

                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine($"Khách hàng hiện tại hỏi: {customerQuestion}");

            string rawReply = string.Empty;

            if (_openAIService.IsConfigured)
            {
                rawReply = await _openAIService.GenerateResponseAsync(systemInstruction, promptBuilder.ToString());
            }

            if (string.IsNullOrWhiteSpace(rawReply) ||
                rawReply.StartsWith("Lỗi API OpenAI") ||
                rawReply.StartsWith("Lỗi ngoại lệ khi gọi OpenAI"))
            {
                rawReply = await _geminiService.GenerateResponseAsync(systemInstruction, promptBuilder.ToString());
            }

            var aiResponse = ParseAiConsultJson(rawReply);
            var fallbackIds = PickFallbackProductIds(customerQuestion, candidateProducts);

            if (aiResponse == null ||
                string.IsNullOrWhiteSpace(aiResponse.Message) ||
                aiResponse.Message.StartsWith("Lỗi:"))
            {
                aiResponse = new AiConsultStructuredResponse
                {
                    Message = BuildFallbackConsultMessage(customerQuestion, candidateProducts, fallbackIds, activeVouchers),
                    RecommendedProductIds = fallbackIds.Take(3).ToList(),
                    VoucherCodes = fallbackIds.Any()
                        ? activeVouchers.Take(1).Select(v => v.Code).ToList()
                        : new List<string>(),
                    NeedsHumanSupport = false
                };
            }

            var validProductIds = candidateProducts.Select(p => p.Id).ToHashSet();

            var recommendedIds = (aiResponse.RecommendedProductIds ?? new List<int>())
                .Where(id => validProductIds.Contains(id))
                .Distinct()
                .Take(3)
                .ToList();

            if (!recommendedIds.Any())
            {
                recommendedIds = fallbackIds.Take(3).ToList();
            }

            var productCards = candidateProducts
                .Where(p => recommendedIds.Contains(p.Id))
                .OrderBy(p => recommendedIds.IndexOf(p.Id))
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    priceText = p.Price.ToString("N0") + "đ",
                    category = p.Category,
                    brand = p.Brand,
                    stock = p.Stock,
                    imageUrl = NormalizeProductImage(p.Image),
                    detailUrl = Url.Action("Detail", "Online", new { id = p.Id }) ?? $"/Online/Detail/{p.Id}"
                })
                .ToList();

            var voucherCodes = recommendedIds.Any()
                ? (aiResponse.VoucherCodes ?? new List<string>())
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Select(code => code.Trim().ToUpper())
                    .Distinct()
                    .Take(2)
                    .ToList()
                : new List<string>();

            if (!voucherCodes.Any() && activeVouchers.Any() && recommendedIds.Any())
            {
                voucherCodes = activeVouchers.Take(1).Select(v => v.Code.ToUpper()).ToList();
            }

            var voucherCards = activeVouchers
                .Where(v => voucherCodes.Contains(v.Code.ToUpper()))
                .Select(v => new
                {
                    code = v.Code,
                    value = v.Value,
                    valueText = v.Value.ToString("N0") + "đ",
                    quantityText = v.Quantity.HasValue ? v.Quantity.Value.ToString() : "Không giới hạn",
                    endDateText = v.EndDate.HasValue ? v.EndDate.Value.ToString("dd/MM/yyyy") : "Không giới hạn"
                })
                .ToList();

            history.Add(new ConsultMessage { Sender = "user", Text = customerQuestion });
            history.Add(new ConsultMessage { Sender = "model", Text = aiResponse.Message ?? "" });

            HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(history));

            return Ok(new
            {
                response = aiResponse.Message,
                products = productCards,
                vouchers = voucherCards,
                needsHumanSupport = aiResponse.NeedsHumanSupport
            });
        }

        [HttpPost]
        public IActionResult ClearConsultChatHistory()
        {
            HttpContext.Session.Remove("ConsultChatHistory");
            return Ok(new { success = true });
        }

        private static AiConsultStructuredResponse? ParseAiConsultJson(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;

            try
            {
                var cleanJson = rawJson.Trim();

                if (cleanJson.StartsWith("```"))
                {
                    cleanJson = Regex.Replace(cleanJson, @"```[a-zA-Z]*\n?", "")
                        .Replace("```", "")
                        .Trim();
                }

                var start = cleanJson.IndexOf('{');
                var end = cleanJson.LastIndexOf('}');

                if (start >= 0 && end > start)
                {
                    cleanJson = cleanJson.Substring(start, end - start + 1);
                }

                return JsonSerializer.Deserialize<AiConsultStructuredResponse>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private static List<AiProductContext> ApplyProductHardFilters(string question, List<AiProductContext> products)
        {
            var q = NormalizeSearchText(question);
            var result = products.ToList();

            if (ContainsAny(q, "laptop", "may tinh xach tay", "notebook", "macbook"))
            {
                result = result
                    .Where(p => ProductContainsAny(p, "laptop", "may tinh xach tay", "notebook", "macbook"))
                    .ToList();
            }
            else if (ContainsAny(q, "iphone"))
            {
                result = result
                    .Where(p => ProductContainsAny(p, "iphone"))
                    .ToList();
            }
            else if (ContainsAny(q, "ipad", "tablet", "may tinh bang"))
            {
                result = result
                    .Where(p => ProductContainsAny(p, "ipad", "tablet", "may tinh bang"))
                    .ToList();
            }
            else if (ContainsAny(q, "dien thoai", "smartphone"))
            {
                result = result
                    .Where(p => ProductContainsAny(p, "iphone", "samsung", "oppo", "xiaomi", "dien thoai", "smartphone", "galaxy"))
                    .ToList();
            }
            else if (ContainsAny(q, "samsung"))
            {
                result = result
                    .Where(p => ProductContainsAny(p, "samsung", "galaxy"))
                    .ToList();
            }
            else if (ContainsAny(q, "oppo"))
            {
                result = result
                    .Where(p => ProductContainsAny(p, "oppo"))
                    .ToList();
            }
            else if (ContainsAny(q, "xiaomi"))
            {
                result = result
                    .Where(p => ProductContainsAny(p, "xiaomi"))
                    .ToList();
            }
            else if (ContainsAny(q, "chuot", "ban phim", "tai nghe", "loa", "sac", "cap", "cu sac", "pin du phong", "phu kien"))
            {
                result = result
                    .Where(p => ProductContainsAny(p, "chuot", "ban phim", "tai nghe", "loa", "sac", "cap", "cu sac", "pin du phong", "phu kien"))
                    .ToList();
            }
            else if (ContainsAny(q, "man hinh", "monitor"))
            {
                result = result
                    .Where(p => ProductContainsAny(p, "man hinh", "monitor"))
                    .ToList();
            }
            else if (ContainsAny(q, "camera"))
            {
                result = result
                    .Where(p => ProductContainsAny(p, "camera"))
                    .ToList();
            }

            var budgetLimit = TryExtractBudget(q);

            if (budgetLimit.HasValue)
            {
                var maxAllowedPrice = HasStrictBudgetIntent(q)
                    ? budgetLimit.Value
                    : budgetLimit.Value * 1.15m;

                result = result
                    .Where(p => p.Price <= maxAllowedPrice)
                    .OrderByDescending(p => p.Price)
                    .ToList();
            }

            return result;
        }

        private static List<int> PickFallbackProductIds(string query, List<AiProductContext> products)
        {
            if (!products.Any()) return new List<int>();

            var lowerQuery = NormalizeSearchText(query);

            var tokens = Regex.Matches(lowerQuery, @"[\p{L}\p{N}]+")
                .Select(m => m.Value)
                .Where(t => t.Length >= 2)
                .Where(t => !StopWords.Contains(t))
                .Distinct()
                .ToList();

            var budget = TryExtractBudget(lowerQuery);

            var scored = products.Select(p =>
            {
                var haystack = NormalizeSearchText($"{p.Name} {p.Category} {p.Brand} {p.Description}");
                var score = 0;

                foreach (var token in tokens)
                {
                    if (haystack.Contains(token))
                    {
                        score += token.Length >= 4 ? 3 : 1;
                    }
                }

                if ((lowerQuery.Contains("laptop") ||
                     lowerQuery.Contains("may tinh") ||
                     lowerQuery.Contains("hoc lap trinh") ||
                     lowerQuery.Contains("do hoa") ||
                     lowerQuery.Contains("van phong")) &&
                    (haystack.Contains("laptop") ||
                     haystack.Contains("notebook") ||
                     haystack.Contains("macbook")))
                {
                    score += 12;
                }

                if ((lowerQuery.Contains("iphone") ||
                     lowerQuery.Contains("dien thoai") ||
                     lowerQuery.Contains("smartphone")) &&
                    (haystack.Contains("iphone") ||
                     haystack.Contains("samsung") ||
                     haystack.Contains("galaxy") ||
                     haystack.Contains("phone")))
                {
                    score += 12;
                }

                if ((lowerQuery.Contains("gaming") || lowerQuery.Contains("game")) &&
                    haystack.Contains("gaming"))
                {
                    score += 8;
                }

                if ((lowerQuery.Contains("phu kien") ||
                     lowerQuery.Contains("chuot") ||
                     lowerQuery.Contains("ban phim") ||
                     lowerQuery.Contains("tai nghe") ||
                     lowerQuery.Contains("sac") ||
                     lowerQuery.Contains("cap")) &&
                    (haystack.Contains("chuot") ||
                     haystack.Contains("ban phim") ||
                     haystack.Contains("tai nghe") ||
                     haystack.Contains("sac") ||
                     haystack.Contains("cap") ||
                     haystack.Contains("phu kien")))
                {
                    score += 10;
                }

                if (budget.HasValue)
                {
                    if (p.Price <= budget.Value)
                    {
                        score += 6;
                    }
                    else if (p.Price <= budget.Value * 1.15m && !HasStrictBudgetIntent(lowerQuery))
                    {
                        score += 2;
                    }
                    else
                    {
                        score -= 100;
                    }
                }

                score += Math.Min(p.Stock, 10) / 5;

                return new { Product = p, Score = score };
            })
            .Where(x => x.Score >= 5)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Product.Price)
            .Take(3)
            .ToList();

            return scored.Select(x => x.Product.Id).ToList();
        }

        private static decimal? TryExtractBudget(string lowerQuery)
        {
            lowerQuery = NormalizeSearchText(lowerQuery);

            var millionMatch = Regex.Match(
                lowerQuery,
                @"(\d+(?:[\.,]\d+)?)\s*(trieu|tr|m)",
                RegexOptions.IgnoreCase
            );

            if (millionMatch.Success &&
                decimal.TryParse(
                    millionMatch.Groups[1].Value.Replace(',', '.'),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var millionValue))
            {
                return millionValue * 1000000m;
            }

            var rawNumberMatch = Regex.Match(
                lowerQuery.Replace(".", "").Replace(",", ""),
                @"\b(\d{7,10})\b"
            );

            if (rawNumberMatch.Success &&
                decimal.TryParse(rawNumberMatch.Groups[1].Value, out var rawValue))
            {
                return rawValue;
            }

            return null;
        }

        private static string BuildFallbackConsultMessage(
            string question,
            List<AiProductContext> products,
            List<int> suggestedIds,
            List<AiVoucherContext> vouchers)
        {
            if (!products.Any())
            {
                return BuildNoCandidateMessage(question);
            }

            if (!suggestedIds.Any())
            {
                return "Dạ em chưa tìm thấy sản phẩm thật sự phù hợp với nhu cầu này trong dữ liệu hiện có của NovaTech. Anh/chị có thể nói rõ hơn về loại sản phẩm công nghệ cần mua, ví dụ: laptop học tập, điện thoại chụp ảnh, phụ kiện máy tính, tai nghe hoặc thiết bị văn phòng để em tư vấn chính xác hơn ạ.";
            }

            var selected = products.Where(p => suggestedIds.Contains(p.Id)).Take(3).ToList();

            if (!selected.Any())
            {
                return "Dạ em chưa tìm thấy sản phẩm phù hợp trong danh sách hiện có. Anh/chị vui lòng mô tả rõ hơn nhu cầu mua sản phẩm công nghệ để em hỗ trợ tốt hơn ạ.";
            }

            var lines = selected.Select(p =>
                $"- **{p.Name}**: {p.Price:N0}đ, thương hiệu {p.Brand}, còn {p.Stock} sản phẩm.");

            var voucherLine = vouchers.Any()
                ? $"\n\n🎁 Anh/chị có thể tham khảo mã khuyến mãi **{vouchers.First().Code}** giảm {vouchers.First().Value:N0}đ nếu đơn hàng đủ điều kiện."
                : "";

            return "Dạ, em đã tìm trong dữ liệu sản phẩm NovaTech và gợi ý cho anh/chị một số lựa chọn phù hợp:\n"
                   + string.Join("\n", lines)
                   + voucherLine
                   + "\n\nAnh/chị có thể bấm **Xem chi tiết** hoặc **Thêm vào giỏ** ngay bên dưới ạ.";
        }

        private static string BuildNoCandidateMessage(string question)
        {
            var q = NormalizeSearchText(question);
            var budget = TryExtractBudget(q);

            if (budget.HasValue && HasStrictBudgetIntent(q))
            {
                return $"Dạ em đã kiểm tra dữ liệu hiện có của NovaTech nhưng chưa tìm thấy sản phẩm phù hợp dưới {budget.Value:N0}đ theo đúng nhu cầu này. Anh/chị có thể tăng ngân sách hoặc đổi tiêu chí để em tư vấn lựa chọn khác phù hợp hơn ạ.";
            }

            if (budget.HasValue)
            {
                return $"Dạ em đã kiểm tra dữ liệu hiện có của NovaTech nhưng chưa tìm thấy sản phẩm phù hợp quanh mức {budget.Value:N0}đ theo đúng nhu cầu này. Anh/chị có thể tăng ngân sách hoặc mô tả lại nhu cầu để em tư vấn chính xác hơn ạ.";
            }

            return "Dạ em chưa tìm thấy sản phẩm phù hợp với nhu cầu này trong dữ liệu hiện có của NovaTech. Anh/chị có thể nói rõ hơn loại sản phẩm công nghệ cần mua để em tư vấn chính xác hơn ạ.";
        }

        private static bool IsClearlyUnsupportedOrOffTopic(string question)
        {
            var q = NormalizeSearchText(question);

            if (string.IsNullOrWhiteSpace(q))
            {
                return true;
            }

            var unsupportedTerms = new[]
            {
                "quan sip",
                "quan lot",
                "ao lot",
                "do lot",
                "noi y",
                "ao thun",
                "ao khoac",
                "quan jean",
                "quan dai",
                "vay",
                "dam",
                "giay dep",
                "giay the thao",
                "son moi",
                "my pham",
                "nuoc hoa",
                "do an",
                "thuc an",
                "bun bo",
                "pho",
                "tra sua",
                "thuoc",
                "ban ghe",
                "giuong",
                "nem",
                "chan ga",
                "sach vo",
                "do choi tre em"
            };

            return unsupportedTerms.Any(term => ContainsWholeTerm(q, term));
        }

        private static bool HasProductConsultIntent(string question)
        {
            var q = NormalizeSearchText(question);

            if (string.IsNullOrWhiteSpace(q))
            {
                return false;
            }

            var techTerms = new[]
            {
                "laptop",
                "may tinh",
                "pc",
                "computer",
                "macbook",
                "iphone",
                "ipad",
                "dien thoai",
                "smartphone",
                "samsung",
                "oppo",
                "xiaomi",
                "asus",
                "acer",
                "dell",
                "hp",
                "lenovo",
                "msi",
                "gaming",
                "man hinh",
                "monitor",
                "chuot",
                "ban phim",
                "tai nghe",
                "loa",
                "camera",
                "ssd",
                "hdd",
                "ram",
                "cpu",
                "gpu",
                "vga",
                "linh kien",
                "phu kien",
                "sac",
                "cap",
                "cu sac",
                "pin du phong",
                "may in",
                "router",
                "wifi",
                "hoc lap trinh",
                "do hoa",
                "van phong",
                "thiet bi",
                "dien tu",
                "cong nghe"
            };

            var shoppingTerms = new[]
            {
                "tu van",
                "goi y",
                "nen mua",
                "nen chon",
                "can mua",
                "muon mua",
                "tim",
                "co san pham",
                "san pham nao",
                "ban chay",
                "khuyen mai",
                "voucher",
                "giam gia",
                "tam gia",
                "duoi",
                "tren",
                "trieu",
                "re",
                "tot"
            };

            var hasTechTerm = techTerms.Any(term => q.Contains(term));
            var hasShoppingTerm = shoppingTerms.Any(term => q.Contains(term));

            return hasTechTerm || hasShoppingTerm;
        }

        private static bool HasStrictBudgetIntent(string question)
        {
            var q = NormalizeSearchText(question);

            var strictTerms = new[]
            {
                "duoi",
                "khong qua",
                "toi da",
                "max",
                "nho hon",
                "be hon",
                "it hon",
                "nho hon hoac bang",
                "be hon hoac bang"
            };

            return strictTerms.Any(term => q.Contains(term));
        }

        private static bool ContainsWholeTerm(string text, string term)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(term))
            {
                return false;
            }

            text = NormalizeSearchText(text);
            term = NormalizeSearchText(term);

            return Regex.IsMatch(text, $@"(^|\s){Regex.Escape(term)}(\s|$)");
        }

        private static bool ContainsAny(string text, params string[] terms)
        {
            text = NormalizeSearchText(text);

            return terms.Any(term => text.Contains(NormalizeSearchText(term)));
        }

        private static bool ProductContainsAny(AiProductContext product, params string[] terms)
        {
            var text = NormalizeSearchText($"{product.Name} {product.Category} {product.Brand} {product.Description}");

            return terms.Any(term => text.Contains(NormalizeSearchText(term)));
        }

        private static string NormalizeSearchText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            text = text.ToLowerInvariant().Trim();
            text = Regex.Replace(text, @"\s+", " ");

            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);

            var chars = normalized
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(chars)
                .Normalize(System.Text.NormalizationForm.FormC)
                .Replace("đ", "d");
        }

        private static string NormalizeProductImage(string image)
        {
            if (string.IsNullOrWhiteSpace(image))
            {
                return "https://images.unsplash.com/photo-1531297484001-80022131f5a1?q=80&w=300";
            }

            if (image.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                image.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                image.StartsWith("/"))
            {
                return image;
            }

            return "/" + image.TrimStart('~', '/');
        }

        private static string LimitText(string? text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Không có mô tả";

            text = Regex.Replace(text.Trim(), @"\s+", " ");

            return text.Length <= maxLength
                ? text
                : text.Substring(0, maxLength) + "...";
        }

        private static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "toi", "em", "anh", "chi", "ban", "co", "khong", "nao", "gi", "la", "thi",
            "can", "muon", "mua", "tim", "cho", "voi", "de", "va", "hoac", "mot",
            "cai", "con", "hang", "san", "pham", "tu", "van", "goi", "y", "tot",
            "re", "gia", "tam", "duoi", "tren", "nen"
        };

        public class OnlineChatRequest
        {
            public string? Message { get; set; }
        }

        public class ConsultMessage
        {
            public string Sender { get; set; } = "";
            public string Text { get; set; } = "";
        }

        private class AiProductContext
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
            public string Description { get; set; } = "";
            public string Category { get; set; } = "";
            public string Brand { get; set; } = "";
            public int Stock { get; set; }
            public string Image { get; set; } = "";
        }

        private class AiVoucherContext
        {
            public string Code { get; set; } = "";
            public decimal Value { get; set; }
            public int? Quantity { get; set; }
            public DateTime? EndDate { get; set; }
        }

        private class AiConsultStructuredResponse
        {
            public string? Message { get; set; }
            public List<int>? RecommendedProductIds { get; set; }
            public List<string>? VoucherCodes { get; set; }
            public bool NeedsHumanSupport { get; set; }
        }
    }
}