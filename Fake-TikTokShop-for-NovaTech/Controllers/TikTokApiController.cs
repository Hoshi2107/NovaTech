using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FakeTikTokShop.Hubs;
using FakeTikTokShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FakeTikTokShop.Controllers
{
    [ApiController]
    [Route("api/tiktok")]
    public class TikTokApiController : ControllerBase
    {
        private readonly TikTokDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IHubContext<LivestreamHub> _hub;

        public TikTokApiController(TikTokDbContext context, IHubContext<LivestreamHub> hub)
        {
            _context = context;
            _httpClient = new HttpClient();
            _hub = hub;
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

                // Also update stock & info of active livestream products
                var liveProducts = await _context.LivestreamProducts.ToListAsync();
                foreach (var lp in liveProducts)
                {
                    var extProd = externalProducts.FirstOrDefault(ep => ep.MaSanPham == lp.ProductId);
                    if (extProd != null)
                    {
                        lp.Stock = extProd.SoLuongTon;
                        lp.Price = extProd.GiaBan;
                        lp.Name = extProd.TenSanPham;
                        lp.ImageUrl = extProd.HinhAnh;
                    }
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

        // --- LIVESTREAM API ---
        [HttpGet("livestream/products")]
        public async Task<IActionResult> GetLivestreamProducts()
        {
            var products = await _context.LivestreamProducts.ToListAsync();
            return Ok(products);
        }

        [HttpPost("livestream/products")]
        public async Task<IActionResult> AddLivestreamProduct([FromBody] AddLivestreamProductRequest request)
        {
            var cache = await _context.ProductCaches.FirstOrDefaultAsync(p => p.ProductId == request.ProductId);
            if (cache == null)
            {
                return NotFound(new { message = "Sản phẩm không có trong catalog." });
            }

            var existing = await _context.LivestreamProducts.FirstOrDefaultAsync(p => p.ProductId == request.ProductId);
            if (existing != null)
            {
                return BadRequest(new { message = "Sản phẩm này đã được thêm vào livestream." });
            }

            var liveProduct = new TikTokLivestreamProduct
            {
                ProductId = cache.ProductId,
                Name = cache.Name,
                Sku = cache.Sku,
                Price = cache.Price,
                ImageUrl = cache.ImageUrl,
                Stock = cache.Stock,
                IsPinned = false,
                SalesCount = 0
            };

            _context.LivestreamProducts.Add(liveProduct);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã thêm sản phẩm vào livestream thành công!", product = liveProduct });
        }

        [HttpDelete("livestream/products/{productId}")]
        public async Task<IActionResult> DeleteLivestreamProduct(int productId)
        {
            var product = await _context.LivestreamProducts.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                return NotFound(new { message = "Không tìm thấy sản phẩm livestream." });
            }

            _context.LivestreamProducts.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa sản phẩm khỏi livestream." });
        }

        [HttpPost("livestream/products/{productId}/pin")]
        public async Task<IActionResult> PinProduct(int productId)
        {
            var products = await _context.LivestreamProducts.ToListAsync();
            var target = products.FirstOrDefault(p => p.ProductId == productId);
            if (target == null)
            {
                return NotFound(new { message = "Không tìm thấy sản phẩm livestream." });
            }

            foreach (var p in products)
            {
                p.IsPinned = (p.ProductId == productId);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã ghim sản phẩm: {target.Name}", products });
        }

        [HttpPost("livestream/products/{productId}/unpin")]
        public async Task<IActionResult> UnpinProduct(int productId)
        {
            var product = await _context.LivestreamProducts.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                return NotFound(new { message = "Không tìm thấy sản phẩm livestream." });
            }

            product.IsPinned = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã bỏ ghim sản phẩm: {product.Name}", product });
        }

        [HttpPost("livestream/products/{productId}/stock")]
        public async Task<IActionResult> UpdateLiveProductStock(int productId, [FromBody] UpdateStockRequest request)
        {
            var product = await _context.LivestreamProducts.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                return NotFound(new { message = "Không tìm thấy sản phẩm livestream." });
            }

            product.Stock = request.Stock;
            
            var cachedProd = await _context.ProductCaches.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (cachedProd != null)
            {
                cachedProd.Stock = request.Stock;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật tồn kho thành công!", product });
        }

        [HttpPost("livestream/simulate-buy")]
        public async Task<IActionResult> SimulateBuy([FromBody] SimulateBuyRequest request)
        {
            var liveProd = await _context.LivestreamProducts.FirstOrDefaultAsync(p => p.ProductId == request.ProductId);
            if (liveProd == null)
            {
                return BadRequest(new { message = "Sản phẩm không có trong livestream." });
            }

            if (liveProd.Stock < request.Quantity)
            {
                return BadRequest(new { message = $"Sản phẩm chỉ còn {liveProd.Stock} trong live! Không đủ để mua {request.Quantity}." });
            }

            // Create a fake order
            var orderId = "TT-LIVE-" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999);
            var order = new TikTokOrder
            {
                OrderId = orderId,
                CustomerName = request.CustomerName ?? "Khách xem Live",
                Phone = request.Phone ?? "0987654321",
                Address = request.Address ?? "Màn hình Livestream TikTok",
                Note = "Mua từ Livestream",
                PaymentMethod = "COD",
                Status = "Awaiting Shipment",
                CreatedAt = DateTime.Now,
                SyncStatus = "Pending"
            };

            liveProd.Stock -= request.Quantity;
            liveProd.SalesCount += request.Quantity;

            var cachedProd = await _context.ProductCaches.FirstOrDefaultAsync(p => p.ProductId == request.ProductId);
            if (cachedProd != null)
            {
                cachedProd.Stock = Math.Max(0, cachedProd.Stock - request.Quantity);
            }

            order.OrderItems.Add(new TikTokOrderItem
            {
                ProductId = liveProd.ProductId,
                ProductName = liveProd.Name,
                ProductSku = liveProd.Sku,
                Quantity = request.Quantity,
                Price = liveProd.Price,
                ImageUrl = liveProd.ImageUrl
            });

            order.TotalPrice = liveProd.Price * request.Quantity;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Add public livestream activity log
            LiveStreamState.AddComment("Hệ thống Live", $"🎉 Khách hàng {order.CustomerName} đã đặt mua {request.Quantity} x {liveProd.Name}!", "color-teal");
            // Push the order notification via SignalR to all connected viewers
            await _hub.Clients.All.SendAsync("ReceiveComment", "Hệ thống Live", $"🎉 Khách hàng {order.CustomerName} đã đặt mua {request.Quantity} x {liveProd.Name}!", "color-teal");
            // Also push updated products to all viewers
            var updatedProducts = await _context.LivestreamProducts.ToListAsync();
            await _hub.Clients.All.SendAsync("ProductsUpdated", updatedProducts);

            var settings = await GetOrCreateSettingsAsync();
            if (settings.AutoPushWebhook)
            {
                await PushWebhookInternalAsync(order, "OrderCreated", settings.NovaTechBaseUrl);
            }

            return Ok(new { message = "Giả lập mua hàng trên livestream thành công!", orderId, order });
        }

        // --- SHARED LIVE STATE ENDPOINTS ---
        [HttpGet("livestream/state")]
        public IActionResult GetLiveState()
        {
            LiveStreamState.GenerateMockCommentIfNeeded();
            return Ok(new
            {
                isLive = LiveStreamState.IsLive,
                viewerCount = LiveStreamState.ViewerCount,
                likesCount = LiveStreamState.LikesCount,
                comments = LiveStreamState.Comments.Select(c => new {
                    username = c.Username,
                    text = c.Text,
                    color = c.Color,
                    timestamp = c.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList()
            });
        }

        [HttpPost("livestream/state/start")]
        public async Task<IActionResult> StartLive()
        {
            LiveStreamState.IsLive = true;
            LiveStreamState.ViewerCount = new Random().Next(50) + 120;
            LiveStreamState.LikesCount = new Random().Next(500) + 1500;
            lock (LiveStreamState.Comments)
            {
                LiveStreamState.Comments.Clear();
                LiveStreamState.AddComment("Hệ thống", "Phòng phát sóng bắt đầu. Trực tiếp từ NovaTech Shop!", "color-red");
            }
            lock (LiveStreamState.AudioChunks)
            {
                LiveStreamState.AudioChunks.Clear();
                LiveStreamState.NextAudioChunkId = 1;
            }
            // Broadcast via SignalR so viewers know immediately
            await _hub.Clients.All.SendAsync("LiveStateChanged", true, LiveStreamState.ViewerCount, LiveStreamState.LikesCount);
            await _hub.Clients.All.SendAsync("ReceiveComment", "Hệ thống", "Phòng phát sóng bắt đầu. Trực tiếp từ NovaTech Shop!", "color-red");
            return Ok(new { message = "Bắt đầu phát sóng!" });
        }

        [HttpPost("livestream/state/stop")]
        public async Task<IActionResult> StopLive()
        {
            LiveStreamState.Reset();
            LiveStreamState.AddComment("Hệ thống", "Đã kết thúc phiên Live Stream.", "color-teal");
            // Broadcast stop state via SignalR
            await _hub.Clients.All.SendAsync("LiveStateChanged", false, 0, LiveStreamState.LikesCount);
            await _hub.Clients.All.SendAsync("ReceiveComment", "Hệ thống", "Đã kết thúc phiên Live Stream.", "color-teal");
            return Ok(new { message = "Đã dừng phát sóng!" });
        }

        [HttpPost("livestream/like")]
        public async Task<IActionResult> AddLike()
        {
            lock (LiveStreamState.Comments)
            {
                LiveStreamState.LikesCount++;
            }
            await _hub.Clients.All.SendAsync("StatsUpdated", LiveStreamState.ViewerCount, LiveStreamState.LikesCount);
            return Ok();
        }

        [HttpPost("livestream/comment")]
        public async Task<IActionResult> AddUserComment([FromBody] AddCommentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text)) return BadRequest();
            var colors = new[] { "color-teal", "color-yellow", "color-pink", "color-green", "color-red" };
            var color = colors[new Random().Next(colors.Length)];
            LiveStreamState.AddComment(request.Username ?? "Khách vãng lai", request.Text, color);
            // Push comment to all viewers instantly via SignalR
            await _hub.Clients.All.SendAsync("ReceiveComment", request.Username ?? "Khách vãng lai", request.Text, color);
            return Ok();
        }

        [HttpPost("livestream/frame")]
        public async Task<IActionResult> UploadFrame([FromBody] UploadFrameRequest request)
        {
            LiveStreamState.CurrentFrame = request.Frame;
            // Push frame instantly to all viewers via SignalR (no more polling!)
            if (request.Frame != null)
            {
                await _hub.Clients.All.SendAsync("ReceiveFrame", request.Frame);
            }
            return Ok();
        }

        [HttpGet("livestream/frame")]
        public IActionResult GetFrame()
        {
            return Ok(new { frame = LiveStreamState.CurrentFrame });
        }

        [HttpPost("livestream/audio")]
        public async Task<IActionResult> UploadAudioChunk([FromBody] UploadAudioChunkRequest request)
        {
            if (string.IsNullOrEmpty(request.Audio)) return BadRequest();
            lock (LiveStreamState.AudioChunks)
            {
                LiveStreamState.AudioChunks.Add(new LiveAudioChunk
                {
                    Id = LiveStreamState.NextAudioChunkId++,
                    Data = request.Audio,
                    Timestamp = DateTime.Now
                });
                if (LiveStreamState.AudioChunks.Count > 15)
                {
                    LiveStreamState.AudioChunks.RemoveAt(0);
                }
            }
            // Push audio instantly to all viewers via SignalR (no more polling!)
            await _hub.Clients.All.SendAsync("ReceiveAudio", request.Audio);
            return Ok();
        }

        [HttpGet("livestream/audio")]
        public IActionResult GetAudioChunks([FromQuery] long lastId)
        {
            lock (LiveStreamState.AudioChunks)
            {
                var chunks = LiveStreamState.AudioChunks
                    .Where(c => c.Id > lastId)
                    .OrderBy(c => c.Id)
                    .ToList();
                return Ok(chunks.Select(c => new { id = c.Id, data = c.Data }));
            }
        }

        [HttpPost("livestream/audio/clear")]
        public IActionResult ClearAudio()
        {
            lock (LiveStreamState.AudioChunks)
            {
                LiveStreamState.AudioChunks.Clear();
                LiveStreamState.NextAudioChunkId = 1;
            }
            return Ok();
        }
    }

    // --- LIVE STATE STORAGE ---
    public static class LiveStreamState
    {
        public static bool IsLive { get; set; } = false;
        public static int ViewerCount { get; set; } = 0;
        public static int LikesCount { get; set; } = 0;
        public static string? CurrentFrame { get; set; } = null;
        public static List<LiveCommentDto> Comments { get; } = new();
        public static List<LiveAudioChunk> AudioChunks { get; } = new();
        public static long NextAudioChunkId { get; set; } = 1;
        private static readonly object _lock = new();

        private static DateTime _lastMockCommentTime = DateTime.MinValue;
        private static readonly string[] MockUsernames = { "quynh_anh99", "tung_son_gaming", "lan_huong_official", "minh_quan_erp", "linh_chi_cute", "ha_anh_tuan", "viet_anh_dev", "kieu_trang_123", "duy_manh_vip" };
        private static readonly string[] MockTexts = {
            "Sản phẩm đẹp quá shop ơi!",
            "Có mã giảm giá không ạ?",
            "Laptop này dùng ổn định không shop?",
            "Mới đặt 1 đơn rồi nha, shop giao nhanh giúp em",
            "Uầy, giá hạt dẻ thế nhở!",
            "Em xin giá dell xps 13 với",
            "Chuột logitech kia có bảo hành không ạ?",
            "Thích cái bàn phím cơ ghê",
            "Đồng bộ đơn hàng TikTok siêu mượt luôn",
            "Shop dễ thương ghê, thả tim nè!"
        };
        private static readonly string[] MockColors = { "color-teal", "color-yellow", "color-pink", "color-green", "color-red" };
        private static readonly Random _random = new();

        static LiveStreamState()
        {
            Reset();
        }

        public static void Reset()
        {
            lock (_lock)
            {
                IsLive = false;
                ViewerCount = 0;
                LikesCount = 0;
                CurrentFrame = null;
                Comments.Clear();
                Comments.Add(new LiveCommentDto
                {
                    Username = "Hệ thống",
                    Text = "Chào mừng bạn đến với phòng Livestream Studio! Nhấn Bắt đầu phát sóng để live.",
                    Color = "color-teal",
                    Timestamp = DateTime.Now
                });
                AudioChunks.Clear();
                NextAudioChunkId = 1;
            }
        }

        public static void AddComment(string username, string text, string color)
        {
            lock (_lock)
            {
                Comments.Add(new LiveCommentDto
                {
                    Username = username,
                    Text = text,
                    Color = color,
                    Timestamp = DateTime.Now
                });
                if (Comments.Count > 50)
                {
                    Comments.RemoveAt(0);
                }
            }
        }

        public static void GenerateMockCommentIfNeeded()
        {
            if (!IsLive) return;
            lock (_lock)
            {
                var now = DateTime.Now;
                if ((now - _lastMockCommentTime).TotalSeconds >= 4)
                {
                    _lastMockCommentTime = now;
                    var username = MockUsernames[_random.Next(MockUsernames.Length)];
                    var text = MockTexts[_random.Next(MockTexts.Length)];
                    var color = MockColors[_random.Next(MockColors.Length)];
                    AddComment(username, text, color);

                    // Also randomly adjust viewer count slightly
                    var delta = _random.Next(15) - 7;
                    ViewerCount = Math.Max(10, ViewerCount + delta);

                    // Randomly add a few likes
                    LikesCount += _random.Next(5) + 1;
                }
            }
        }
    }

    public class LiveCommentDto
    {
        public string Username { get; set; } = "";
        public string Text { get; set; } = "";
        public string Color { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    public class AddCommentRequest
    {
        public string? Username { get; set; }
        public string Text { get; set; } = "";
    }

    public class UploadFrameRequest
    {
        public string? Frame { get; set; }
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

    public class AddLivestreamProductRequest
    {
        public int ProductId { get; set; }
    }

    public class UpdateStockRequest
    {
        public int Stock { get; set; }
    }

    public class SimulateBuyRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? CustomerName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class LiveAudioChunk
    {
        public long Id { get; set; }
        public string Data { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    public class UploadAudioChunkRequest
    {
        public string Audio { get; set; } = "";
    }
}
