using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FakeTikTokShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FakeTikTokShop.Controllers
{
    [ApiController]
    [Route("api/tiktok")]
    public class TikTokApiController : ControllerBase
    {
        private readonly TikTokDbContext _context;
        private readonly HttpClient _httpClient;

        public TikTokApiController(TikTokDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        // --- ORDERS API (PULL FOR NOVATECH) ---
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return Ok(orders);
        }

        // --- CREATE MOCK ORDER ---
        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request == null || !request.Items.Any())
            {
                return BadRequest(new { message = "Đơn hàng phải có ít nhất 1 sản phẩm!" });
            }

            var orderId = "TT-" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999);

            var order = new TikTokOrder
            {
                OrderId = orderId,
                CustomerName = request.CustomerName,
                Phone = request.Phone,
                Address = request.Address,
                Note = request.Note,
                PaymentMethod = request.PaymentMethod,
                Status = "Awaiting Shipment",
                CreatedAt = DateTime.Now,
                SyncStatus = "Pending"
            };

            decimal total = 0;
            foreach (var item in request.Items)
            {
                var cachedProd = await _context.ProductCaches.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                if (cachedProd == null)
                {
                    return BadRequest(new { message = $"Sản phẩm với mã {item.ProductId} không tồn tại trên Fake TikTok Shop. Vui lòng đồng bộ danh mục trước!" });
                }

                if (cachedProd.Stock <= 0)
                {
                    return BadRequest(new { message = $"Sản phẩm '{cachedProd.Name}' đã hết hàng trong kho!" });
                }

                if (cachedProd.Stock < item.Quantity)
                {
                    return BadRequest(new { message = $"Sản phẩm '{cachedProd.Name}' không đủ tồn kho (Chỉ còn {cachedProd.Stock} sản phẩm)!" });
                }

                // Deduct stock
                cachedProd.Stock -= item.Quantity;

                var itemName = cachedProd.Name;
                var itemSku = cachedProd.Sku;
                var itemPrice = cachedProd.Price;

                order.OrderItems.Add(new TikTokOrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = itemName,
                    ProductSku = itemSku,
                    Quantity = item.Quantity,
                    Price = itemPrice,
                    ImageUrl = cachedProd.ImageUrl
                });

                total += itemPrice * item.Quantity;
            }

            order.TotalPrice = total;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var settings = await GetOrCreateSettingsAsync();
            if (settings.AutoPushWebhook)
            {
                await PushWebhookInternalAsync(order, "OrderCreated", settings.NovaTechBaseUrl);
            }

            return Ok(new { message = "Tạo đơn hàng TikTok thành công!", orderId, order });
        }

        // --- UPDATE ORDER STATUS ---
        [HttpPost("orders/{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn hàng!" });
            }

            // Restore or deduct stock based on transition to/from "Cancelled"
            if (request.Status == "Cancelled" && order.Status != "Cancelled")
            {
                foreach (var item in order.OrderItems)
                {
                    var cachedProd = await _context.ProductCaches.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                    if (cachedProd != null)
                    {
                        cachedProd.Stock += item.Quantity;
                    }
                }
            }
            else if (request.Status != "Cancelled" && order.Status == "Cancelled")
            {
                foreach (var item in order.OrderItems)
                {
                    var cachedProd = await _context.ProductCaches.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                    if (cachedProd != null)
                    {
                        if (cachedProd.Stock < item.Quantity)
                        {
                            return BadRequest(new { message = $"Sản phẩm '{cachedProd.Name}' không đủ tồn kho để khôi phục đơn hàng!" });
                        }
                        cachedProd.Stock -= item.Quantity;
                    }
                }
            }

            order.Status = request.Status;
            await _context.SaveChangesAsync();

            var settings = await GetOrCreateSettingsAsync();
            if (settings.AutoPushWebhook)
            {
                await PushWebhookInternalAsync(order, "StatusUpdated", settings.NovaTechBaseUrl);
            }

            return Ok(new { message = $"Cập nhật trạng thái đơn hàng sang '{request.Status}' thành công!", order });
        }

        // --- MANUALLY PUSH WEBHOOK ---
        [HttpPost("orders/{id}/push-webhook")]
        public async Task<IActionResult> PushWebhook(string id)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn hàng!" });
            }

            var settings = await GetOrCreateSettingsAsync();
            var success = await PushWebhookInternalAsync(order, "ManualPush", settings.NovaTechBaseUrl);

            if (success)
            {
                return Ok(new { message = "Đẩy Webhook thành công!" });
            }
            else
            {
                return StatusCode(500, new { message = "Đẩy Webhook thất bại!", error = order.WebhookErrorMessage });
            }
        }

        // --- WEBHOOK LOGS ---
        [HttpGet("webhook-logs")]
        public async Task<IActionResult> GetWebhookLogs()
        {
            var logs = await _context.WebhookLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .ToListAsync();
            return Ok(logs);
        }

        // --- CLEAR MOCK DATA ---
        [HttpPost("clear-data")]
        public async Task<IActionResult> ClearData()
        {
            try
            {
                _context.OrderItems.RemoveRange(_context.OrderItems);
                _context.Orders.RemoveRange(_context.Orders);
                _context.WebhookLogs.RemoveRange(_context.WebhookLogs);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã xóa toàn bộ đơn hàng và log webhook trên Fake TikTok Shop!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi xóa dữ liệu: {ex.Message}" });
            }
        }

        // --- SETTINGS API ---
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await GetOrCreateSettingsAsync();
            return Ok(settings);
        }

        [HttpPost("settings")]
        public async Task<IActionResult> SaveSettings([FromBody] TikTokShopSettings newSettings)
        {
            var settings = await GetOrCreateSettingsAsync();
            settings.NovaTechBaseUrl = newSettings.NovaTechBaseUrl.TrimEnd('/');
            settings.AutoPushWebhook = newSettings.AutoPushWebhook;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật cấu hình thành công!", settings });
        }

        // --- PRODUCTS CACHE API ---
        [HttpGet("products-cache")]
        public async Task<IActionResult> GetProductsCache()
        {
            var products = await _context.ProductCaches.ToListAsync();
            return Ok(products);
        }

        // --- SYNC PRODUCTS FROM NOVATECH ---
        [HttpPost("sync-products")]
        public async Task<IActionResult> SyncProducts()
        {
            var settings = await GetOrCreateSettingsAsync();
            var url = $"{settings.NovaTechBaseUrl}/api/product";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest(new { message = $"Không thể tải sản phẩm từ NovaTech (HTTP {(int)response.StatusCode})" });
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var externalProducts = JsonSerializer.Deserialize<List<ExternalProductDto>>(content, options);

                if (externalProducts == null || !externalProducts.Any())
                {
                    return Ok(new { message = "Không tìm thấy sản phẩm nào trên NovaTech để đồng bộ!" });
                }

                // Clear current cache
                _context.ProductCaches.RemoveRange(_context.ProductCaches);

                foreach (var p in externalProducts)
                {
                    _context.ProductCaches.Add(new TikTokProductCache
                    {
                        ProductId = p.MaSanPham,
                        Name = p.TenSanPham,
                        Sku = $"SP-{p.MaSanPham}",
                        Price = p.GiaBan,
                        ImageUrl = p.HinhAnh,
                        Stock = p.SoLuongTon
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"Đồng bộ thành công {externalProducts.Count} sản phẩm từ NovaTech!", productsCount = externalProducts.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi kết nối tới NovaTech: {ex.Message}" });
            }
        }

        // --- PRIVATE HELPER METHODS ---
        private async Task<TikTokShopSettings> GetOrCreateSettingsAsync()
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new TikTokShopSettings
                {
                    NovaTechBaseUrl = "http://localhost:5018",
                    AutoPushWebhook = true
                };
                _context.Settings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        private async Task<bool> PushWebhookInternalAsync(TikTokOrder order, string actionType, string baseUrl)
        {
            var webhookUrl = $"{baseUrl}/api/tiktok/webhook";

            // Prepare webhook payload
            var payloadObj = new
            {
                @event = actionType == "OrderCreated" ? "order_created" : "order_status_changed",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                data = new
                {
                    order_id = order.OrderId,
                    customer_name = order.CustomerName,
                    phone = order.Phone,
                    address = order.Address,
                    note = order.Note,
                    total_price = order.TotalPrice,
                    payment_method = order.PaymentMethod,
                    status = order.Status,
                    created_at = order.CreatedAt,
                    items = order.OrderItems.Select(i => new
                    {
                        product_id = i.ProductId,
                        product_name = i.ProductName,
                        product_sku = i.ProductSku,
                        quantity = i.Quantity,
                        price = i.Price
                    }).ToList()
                }
            };

            var json = JsonSerializer.Serialize(payloadObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var log = new WebhookLog
            {
                OrderId = order.OrderId,
                ActionType = actionType,
                Payload = json,
                Timestamp = DateTime.Now
            };

            bool isSuccess = false;
            try
            {
                var response = await _httpClient.PostAsync(webhookUrl, content);
                log.HttpStatus = (int)response.StatusCode;
                var responseText = await response.Content.ReadAsStringAsync();
                log.Message = $"Phản hồi từ server: {responseText}";

                if (response.IsSuccessStatusCode)
                {
                    order.SyncStatus = "Success";
                    order.WebhookErrorMessage = null;
                    isSuccess = true;
                }
                else
                {
                    order.SyncStatus = "Failed";
                    order.WebhookErrorMessage = $"Server trả về lỗi HTTP {(int)response.StatusCode}: {responseText}";
                }
            }
            catch (Exception ex)
            {
                log.Message = $"Lỗi kết nối: {ex.Message}";
                order.SyncStatus = "Failed";
                order.WebhookErrorMessage = $"Lỗi kết nối: {ex.Message}";
            }

            _context.WebhookLogs.Add(log);
            await _context.SaveChangesAsync();

            return isSuccess;
        }
    }

    // --- HELPER REQUEST/RESPONSE DTOs ---
    public class CreateOrderRequest
    {
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "COD";
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSku { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = "";
    }

    public class ExternalProductDto
    {
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; } = "";
        public decimal GiaBan { get; set; }
        public string? HinhAnh { get; set; }
        public int SoLuongTon { get; set; }
    }
}
