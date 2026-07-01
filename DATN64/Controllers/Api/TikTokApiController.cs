using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DATN64.Models;

namespace DATN64.Controllers.Api
{
    [Route("api/tiktok")]
    [ApiController]
    public class TikTokApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TikTokApiController(AppDbContext context)
        {
            _context = context;
        }

        // --- WEBHOOK RECEIVER ENDPOINT ---
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] TikTokWebhookPayload payload)
        {
            if (payload == null || payload.Data == null)
            {
                return BadRequest("Payload không hợp lệ.");
            }

            var data = payload.Data;
            var identifier = $"[TikTokShop#{data.OrderId}]";

            // Map TikTok order status to NovaTech status
            string mappedStatus = MapTikTokStatus(data.Status);

            // 1. Check if the order is already synced
            var existingOrder = await _context.DonHangs
                .FirstOrDefaultAsync(o => o.GhiChu != null && o.GhiChu.Contains(identifier));

            if (existingOrder != null)
            {
                var oldStatus = existingOrder.TrangThai ?? "";

                // ✅ Hoàn kho khi TikTok báo hủy đơn (chỉ hoàn 1 lần, khi chưa ở trạng thái hủy)
                if (mappedStatus == "Đã hủy" && oldStatus != "Đã hủy")
                {
                    var orderDetails = await _context.ChiTietDonHangs
                        .Where(ct => ct.MaDonHang == existingOrder.MaDonHang)
                        .ToListAsync();

                    foreach (var detail in orderDetails)
                    {
                        var product = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == detail.MaSanPham);
                        if (product != null)
                        {
                            product.SoLuongTon += detail.SoLuong;
                        }
                    }
                }

                // Update status
                existingOrder.TrangThai = mappedStatus;

                // Record Sync Log
                var log = new TikTokSyncLog
                {
                    Type = "Cập nhật đơn hàng",
                    Message = $"Đồng bộ cập nhật trạng thái đơn hàng TikTok Shop #{data.OrderId} sang '{mappedStatus}'." +
                              (mappedStatus == "Đã hủy" && oldStatus != "Đã hủy" ? " Hàng đã hoàn về kho." : ""),
                    Status = "Thành công",
                    Timestamp = DateTime.Now
                };
                _context.TikTokSyncLogs.Add(log);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Đã cập nhật trạng thái đơn hàng {data.OrderId} sang '{mappedStatus}'!" });
            }

            // 2. Order does not exist, let's validate items first
            if (data.Items == null || !data.Items.Any())
            {
                return BadRequest("Đơn hàng không có sản phẩm nào.");
            }

            foreach (var item in data.Items)
            {
                var productExists = await _context.SanPhams.AnyAsync(p => p.MaSanPham == item.ProductId);
                if (!productExists)
                {
                    return BadRequest(new { message = $"Sản phẩm '{item.ProductName}' (Mã: {item.ProductId}) không tồn tại trên hệ thống NovaTech. Vui lòng đồng bộ lại danh mục sản phẩm trước!" });
                }
            }

            // Find or create customer (match both name and phone to prevent renaming existing ones)
            var customer = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == data.Phone && k.HoTen == data.CustomerName);
            if (customer == null)
            {
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<KhachHang>();
                customer = new KhachHang
                {
                    HoTen = data.CustomerName,
                    SoDienThoai = data.Phone,
                    DiaChi = data.Address,
                    Email = $"{data.Phone}@tiktok.com",
                    MatKhau = hasher.HashPassword(null!, "TikTok12345"), // Safe hashed password
                    DiemTichLuy = 0,
                    TrangThai = "Hoạt động",
                    NgayTao = DateTime.Now
                };
                _context.KhachHangs.Add(customer);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (!string.IsNullOrEmpty(data.Address))
                {
                    customer.DiaChi = data.Address;
                }
            }

            // Create Order
            DateTime orderDate = DateTime.Now;
            if (data.CreatedAt >= new DateTime(1753, 1, 1) && data.CreatedAt <= new DateTime(9999, 12, 31))
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
            await _context.SaveChangesAsync(); // Save to generate MaDonHang

            // Add Items & Deduct Stock
            foreach (var item in data.Items)
            {
                var product = await _context.SanPhams.FirstOrDefaultAsync(p => p.MaSanPham == item.ProductId);
                if (product != null)
                {
                    // Deduct stock
                    product.SoLuongTon = Math.Max(0, product.SoLuongTon - item.Quantity);
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

            // Create notification
            var notification = new SystemNotification
            {
                Title = "Đơn hàng TikTok Shop mới",
                Message = $"Đơn hàng TikTok #{data.OrderId} trị giá {data.TotalPrice.ToString("N0")} đ được đồng bộ thành công.",
                Type = "Đơn mới",
                Timestamp = DateTime.Now,
                IsRead = false
            };
            _context.SystemNotifications.Add(notification);

            // Record Sync Log
            var syncLog = new TikTokSyncLog
            {
                Type = "Đơn hàng",
                Message = $"Đồng bộ thành công đơn hàng mới từ TikTok Shop #{data.OrderId}.",
                Status = "Thành công",
                Timestamp = DateTime.Now
            };
            _context.TikTokSyncLogs.Add(syncLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đồng bộ thành công đơn hàng {data.OrderId}!", orderId = order.MaDonHang });
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

    // --- WEBHOOK PAYLOAD MODELS ---
    public class TikTokWebhookPayload
    {
        [System.Text.Json.Serialization.JsonPropertyName("event")]
        public string Event { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public TikTokWebhookData Data { get; set; } = null!;
    }

    public class TikTokWebhookData
    {
        [System.Text.Json.Serialization.JsonPropertyName("order_id")]
        public string OrderId { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("customer_name")]
        public string CustomerName { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("phone")]
        public string Phone { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("address")]
        public string Address { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("note")]
        public string? Note { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("total_price")]
        public decimal TotalPrice { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("items")]
        public List<TikTokWebhookItem> Items { get; set; } = new();
    }

    public class TikTokWebhookItem
    {
        [System.Text.Json.Serialization.JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("product_name")]
        public string ProductName { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("product_sku")]
        public string ProductSku { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}
