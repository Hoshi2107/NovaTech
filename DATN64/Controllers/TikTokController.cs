using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Text.Json;

namespace DATN64.Controllers
{
    [HasPermission("View_TikTok")]
    public class TikTokController : Controller
    {
        private readonly AppDbContext _context;

        public TikTokController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var config = _context.TikTokShopConfigs.FirstOrDefault() ?? new TikTokShopConfig();
            ViewBag.SyncLogs = _context.TikTokSyncLogs.OrderByDescending(l => l.Timestamp).ToList();
            return View(config);
        }

        [HttpPost]
        [HasPermission("Sync_TikTok")]
        public async Task<IActionResult> TriggerSync(string syncType)
        {
            var config = _context.TikTokShopConfigs.FirstOrDefault();
            if (config == null)
            {
                config = new TikTokShopConfig
                {
                    ShopName = "NovaTech TikTok Shop (Fake)",
                    ShopId = "TT-NOVATECH",
                    IsConnected = true,
                    SyncStatus = "Đã đồng bộ",
                    LastSyncTime = System.DateTime.Now
                };
                _context.TikTokShopConfigs.Add(config);
            }

            if (syncType == "Đơn hàng")
            {
                using var client = new HttpClient();
                var simulatorUrl = "http://localhost:6060/api/tiktok/orders";

                try
                {
                    var response = await client.GetAsync(simulatorUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        var failLog = new TikTokSyncLog
                        {
                            Type = syncType,
                            Message = $"Đồng bộ đơn hàng thất bại. Không thể kết nối tới Trình giả lập tại {simulatorUrl}. HTTP {(int)response.StatusCode}",
                            Status = "Thất bại",
                            Timestamp = System.DateTime.Now
                        };
                        _context.TikTokSyncLogs.Add(failLog);
                        await _context.SaveChangesAsync();

                        TempData["ToastMessage"] = $"Lỗi đồng bộ: Trình giả lập trả về HTTP {(int)response.StatusCode}!";
                        TempData["ToastType"] = "danger";
                        return RedirectToAction("Index");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var tiktokOrders = System.Text.Json.JsonSerializer.Deserialize<List<TikTokOrderPullDto>>(json, options);

                    int newOrdersCount = 0;
                    int updatedOrdersCount = 0;

                    if (tiktokOrders != null && tiktokOrders.Any())
                    {
                        foreach (var data in tiktokOrders)
                        {
                            var identifier = $"[TikTokShop#{data.OrderId}]";
                            string mappedStatus = MapTikTokStatus(data.Status);

                            // Check existing
                            var existingOrder = _context.DonHangs
                                .FirstOrDefault(o => o.GhiChu != null && o.GhiChu.Contains(identifier));

                            if (existingOrder != null)
                            {
                                if (existingOrder.TrangThai != mappedStatus)
                                {
                                    existingOrder.TrangThai = mappedStatus;
                                    updatedOrdersCount++;
                                }
                                continue;
                            }

                            // Find or create customer (match both name and phone to prevent renaming existing ones)
                            var customer = _context.KhachHangs.FirstOrDefault(k => k.SoDienThoai == data.Phone && k.HoTen == data.CustomerName);
                            if (customer == null)
                            {
                                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<KhachHang>();
                                customer = new KhachHang
                                {
                                    HoTen = data.CustomerName,
                                    SoDienThoai = data.Phone,
                                    DiaChi = data.Address,
                                    Email = $"{data.Phone}@tiktok.com",
                                    MatKhau = hasher.HashPassword(null!, "TikTok12345"),
                                    DiemTichLuy = 0,
                                    TrangThai = "Hoạt động",
                                    NgayTao = System.DateTime.Now
                                };
                                _context.KhachHangs.Add(customer);
                                _context.SaveChanges(); // get customer ID
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(data.Address))
                                {
                                    customer.DiaChi = data.Address;
                                }
                            }

                            // Validate products for this order
                            bool allProductsExist = true;
                            foreach (var item in data.OrderItems)
                            {
                                if (!_context.SanPhams.Any(p => p.MaSanPham == item.ProductId))
                                {
                                    allProductsExist = false;
                                    break;
                                }
                            }

                            if (!allProductsExist)
                            {
                                var failLog = new TikTokSyncLog
                                {
                                    Type = syncType,
                                    Message = $"Bỏ qua đơn hàng TikTok #{data.OrderId} do chứa sản phẩm không tồn tại trên hệ thống NovaTech. Vui lòng đồng bộ danh mục sản phẩm trước!",
                                    Status = "Thất bại",
                                    Timestamp = System.DateTime.Now
                                };
                                _context.TikTokSyncLogs.Add(failLog);
                                continue;
                            }

                            // Create Order
                            System.DateTime orderDate = System.DateTime.Now;
                            if (data.CreatedAt >= new System.DateTime(1753, 1, 1) && data.CreatedAt <= new System.DateTime(9999, 12, 31))
                            {
                                orderDate = data.CreatedAt;
                            }

                            var order = new DonHang
                            {
                                MaKhachHang = customer.MaKhachHang,
                                NgayDat = orderDate,
                                TongTien = data.TotalPrice,
                                TrangThai = mappedStatus,
                                PhuongThucThanhToan = $"TikTok - {data.PaymentMethod}",
                                GhiChu = $"{identifier} Ghi chú khách: {data.Note ?? "Không có"}"
                            };
                            _context.DonHangs.Add(order);
                            _context.SaveChanges(); // get order ID

                            // Create details & deduct stock
                            foreach (var item in data.OrderItems)
                            {
                                var product = _context.SanPhams.FirstOrDefault(p => p.MaSanPham == item.ProductId);
                                if (product != null)
                                {
                                    product.SoLuongTon = System.Math.Max(0, product.SoLuongTon - item.Quantity);
                                }

                                var orderDetail = new ChiTietDonHang
                                {
                                    MaDonHang = order.MaDonHang,
                                    MaSanPham = item.ProductId,
                                    SoLuong = item.Quantity,
                                    DonGia = item.Price
                                };
                                _context.ChiTietDonHangs.Add(orderDetail);
                            }

                            // Add system notification
                            var notification = new SystemNotification
                            {
                                Title = "Đơn hàng TikTok Shop mới (Pull)",
                                Message = $"Đơn hàng TikTok #{data.OrderId} trị giá {data.TotalPrice.ToString("N0")} đ được đồng bộ thành công.",
                                Type = "Đơn mới",
                                Timestamp = System.DateTime.Now,
                                IsRead = false
                            };
                            _context.SystemNotifications.Add(notification);

                            newOrdersCount++;
                        }
                    }

                    // Log success
                    var log = new TikTokSyncLog
                    {
                        Type = syncType,
                        Message = $"Đồng bộ đơn hàng thành công với TikTok Shop. Đã thêm mới {newOrdersCount} đơn, cập nhật {updatedOrdersCount} đơn.",
                        Status = "Thành công",
                        Timestamp = System.DateTime.Now
                    };
                    _context.TikTokSyncLogs.Add(log);

                    config.LastSyncTime = System.DateTime.Now;
                    config.IsConnected = true;
                    config.ShopName = "NovaTech TikTok Shop (Fake)";
                    config.ShopId = "TT-NOVATECH";
                    config.SyncStatus = "Đã đồng bộ";
                    await _context.SaveChangesAsync();

                    TempData["ToastMessage"] = $"Đồng bộ thành công! Thêm {newOrdersCount} đơn hàng mới, cập nhật {updatedOrdersCount} đơn.";
                    TempData["ToastType"] = "success";
                }
                catch (System.Exception ex)
                {
                    var failLog = new TikTokSyncLog
                    {
                        Type = syncType,
                        Message = $"Lỗi kết nối tới Trình giả lập: {ex.Message}",
                        Status = "Thất bại",
                        Timestamp = System.DateTime.Now
                    };
                    _context.TikTokSyncLogs.Add(failLog);
                    await _context.SaveChangesAsync();

                    TempData["ToastMessage"] = $"Lỗi kết nối tới trình giả lập TikTok Shop: {ex.Message}";
                    TempData["ToastType"] = "danger";
                }
            }
            else
            {
                // Sản phẩm sync
                var log = new TikTokSyncLog
                {
                    Type = syncType,
                    Message = $"Đồng bộ thành công dữ liệu sản phẩm với TikTok API.",
                    Status = "Thành công",
                    Timestamp = System.DateTime.Now
                };
                _context.TikTokSyncLogs.Add(log);

                config.LastSyncTime = System.DateTime.Now;
                config.SyncStatus = "Đã đồng bộ";
                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = $"Đồng bộ dữ liệu sản phẩm hoàn tất!";
                TempData["ToastType"] = "success";
            }

            return RedirectToAction("Index");
        }

        private string MapTikTokStatus(string tikTokStatus)
        {
            return tikTokStatus switch
            {
                "Awaiting Shipment" => "Chờ duyệt",
                "Shipped" => "Đang giao",
                "Delivered" => "Đã giao",
                "Cancelled" => "Đã hủy",
                _ => "Chờ duyệt"
            };
        }
    }

    // --- DTOs for Pull Integration ---
    public class TikTokOrderPullDto
    {
        public string OrderId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string? Note { get; set; }
        public decimal TotalPrice { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string Status { get; set; } = "";
        public System.DateTime CreatedAt { get; set; }
        public System.Collections.Generic.List<TikTokOrderItemPullDto> OrderItems { get; set; } = new();
    }

    public class TikTokOrderItemPullDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductSku { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
