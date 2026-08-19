using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DATN64.Controllers
{
    [Route("Ai")]
    public class AiController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GeminiService _geminiService;
        private readonly EmailService _emailService;

        public AiController(AppDbContext context, GeminiService geminiService, EmailService emailService)
        {
            _context = context;
            _geminiService = geminiService;
            _emailService = emailService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            // Filter history by current user email or legacy records
            var history = _context.ChatMessages
                .Where(m => m.Sender.Contains(userEmail) || (!m.Sender.Contains(":") && userEmail == "admin@novatech.com"))
                .OrderBy(m => m.Timestamp)
                .ToList();

            // Clean sender names for UI presentation
            foreach (var msg in history)
            {
                if (msg.Sender.StartsWith("User:")) msg.Sender = "User";
                else if (msg.Sender.StartsWith("AI:")) msg.Sender = "AI";
            }

            return View(history);
        }

        // ─── AJAX Chat Endpoint ─────────────────────────────────────────────────────
        [HttpPost("ChatAsync")]
        public async Task<IActionResult> ChatAsync([FromBody] ChatRequest req)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized(new { error = "Chưa đăng nhập" });

            if (string.IsNullOrWhiteSpace(req?.Message))
                return BadRequest(new { error = "Tin nhắn trống" });

            string userName = HttpContext.Session.GetString("UserName") ?? "Nhân viên";
            string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // Save user message with user email tag for per-user isolation
            _context.ChatMessages.Add(new ChatMessage
            {
                Sender = $"User:{userEmail}",
                Message = req.Message,
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            // Fetch live data
            var orders = await _context.DonHangs.ToListAsync();
            var products = await _context.SanPhams.ToListAsync();
            var suppliers = await _context.NhaCungCaps.ToListAsync();
            var categories = await _context.DanhMucs.ToListAsync();
            var brands = await _context.ThuongHieus.ToListAsync();
            var customers = await _context.KhachHangs.ToListAsync();

            decimal totalRevenue = orders.Where(o => o.TrangThai == "Hoàn thành").Sum(o => o.TongTien ?? 0);
            int totalOrdersCount = orders.Count;
            int pendingOrdersCount = orders.Count(o => o.TrangThai == "Đơn mới" || o.TrangThai == "Đã xác nhận");
            int completedOrdersCount = orders.Count(o => o.TrangThai == "Hoàn thành");

            // Calculate best sellers
            var orderDetails = await _context.ChiTietDonHangs.ToListAsync();
            var topProductsData = orderDetails
                .GroupBy(ct => ct.MaSanPham)
                .Select(g => new
                {
                    MaSanPham = g.Key,
                    QuantitySold = g.Sum(ct => ct.SoLuong)
                })
                .OrderByDescending(g => g.QuantitySold)
                .Take(5)
                .ToList();

            string topProductsText = topProductsData.Any()
                ? string.Join("\n", topProductsData.Select((tp, idx) => {
                    var p = products.FirstOrDefault(prod => prod.MaSanPham == tp.MaSanPham);
                    return $"- Top {idx + 1}: {p?.TenSanPham ?? "Sản phẩm ẩn"} (Mã SP: {tp.MaSanPham}, SKU: {p?.SKU}, Đã bán: {tp.QuantitySold} sản phẩm)";
                }))
                : "- Chưa có dữ liệu sản phẩm bán chạy.";

            var highStockProducts = products.OrderByDescending(p => p.SoLuongTon).Take(5).ToList();
            string highStockText = string.Join("\n", highStockProducts.Select(p => $"- {p.TenSanPham} (SKU: {p.SKU}, Tồn kho cao: {p.SoLuongTon} cái)"));

            // Tính sản phẩm bán chậm nhất tháng này
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var allOrders = await _context.DonHangs.Where(o => o.NgayDat >= startOfMonth && o.TrangThai == "Hoàn thành").ToListAsync();
            var allOrderIds = allOrders.Select(o => o.MaDonHang).ToHashSet();
            var allDetails = await _context.ChiTietDonHangs.ToListAsync();
            var soldThisMonth = allDetails
                .Where(ct => allOrderIds.Contains(ct.MaDonHang))
                .GroupBy(ct => ct.MaSanPham)
                .ToDictionary(g => g.Key, g => g.Sum(ct => ct.SoLuong));

            // Top 3 tồn kho cao + bán ít nhất tháng này
            var slowMovingTop3 = products
                .Where(p => p.SoLuongTon > 0 && p.TrangThai == "Đang bán")
                .Select(p => new { Product = p, SoldThisMonth = soldThisMonth.TryGetValue(p.MaSanPham, out var s) ? s : 0 })
                .OrderBy(x => x.SoldThisMonth)
                .ThenByDescending(x => x.Product.SoLuongTon)
                .Take(3)
                .ToList();

            string slowMovingText = slowMovingTop3.Any()
                ? string.Join("\n", slowMovingTop3.Select((x, i) => $"- TOP{i+1}: {x.Product.TenSanPham} | SKU: {x.Product.SKU} | Tồn: {x.Product.SoLuongTon} cái | Bán tháng này: {x.SoldThisMonth} cái | Giá: {x.Product.GiaBan:N0}đ"))
                : "- Không có dữ liệu sản phẩm bán chậm.";

            var lowStockProducts = products.Where(p => p.SoLuongTon <= 5).ToList();
            string lowStockText = lowStockProducts.Any()
                ? string.Join("\n", lowStockProducts.Select(p => $"- {p.TenSanPham} (SKU: {p.SKU}, Còn: {p.SoLuongTon})"))
                : "- Không có sản phẩm nào sắp hết hàng.";

            string topCustomersText = string.Join(", ", customers.OrderByDescending(c => c.DiemTichLuy).Take(5).Select(c => $"{c.HoTen} ({c.Email})"));

            string supplierList = string.Join(", ", suppliers.Select(s => $"{s.MaNCC}: {s.TenNCC}"));
            string categoryList = string.Join(", ", categories.Select(c => $"{c.MaDanhMuc}: {c.TenDanhMuc}"));
            string brandList = string.Join(", ", brands.Select(b => $"{b.MaThuongHieu}: {b.TenThuongHieu}"));
            string productList = string.Join("\n", products.Take(30).Select(p =>
                $"- [{p.MaSanPham}] {p.TenSanPham} | GiaNhap: {p.GiaNhap:N0}đ | GiaBan: {p.GiaBan:N0}đ | Tồn: {p.SoLuongTon}"));

            // ─── System Instruction for Agentic Mode ────────────────────────────────
            string systemInstruction = $@"Bạn là Trợ lý AI Autonomous Agent tích hợp trong ERP NovaTech - Cửa hàng Công nghệ.
Người dùng: {userName} | Tài khoản: {userEmail} | Thời gian: {currentTime}

=== DỮ LIỆU THỰC TẾ HỆ THỐNG ===
DOANH THU & ĐƠN HÀNG:
- Doanh thu hoàn thành: {totalRevenue:N0}đ
- Tổng đơn: {totalOrdersCount} | Chờ: {pendingOrdersCount} | Hoàn thành: {completedOrdersCount}

TOP BÁN CHẠY:
{topProductsText}

DANH SÁCH TOP 3 SẢN PHẨM TỒN KHO CAO - BÁN CHẬM NHẤT THÁNG NÀY (để phân tích và đề xuất xả hàng):
{slowMovingText}

TỒN KHO CAO TỔNG QUAN (TOP 5):
{highStockText}

CẢNH BÁO TỒN KHO THẤP (≤5):
{lowStockText}

KHÁCH HÀNG VIP:
{topCustomersText}

DANH SÁCH MỘT SỐ SẢN PHẨM:
{productList}
NHÀ CUNG CẤP: {supplierList}
DANH MỤC: {categoryList} | THƯƠNG HIỆU: {brandList}

=== QUY TẮC AGENTIC RESPONSE (BẮT BUỘC TRẢ VỀ JSON HỢP LỆ) ===
Trả về CHÍNH XÁC cấu trúc JSON:
{{
  ""message"": ""Nội dung trả lời bằng tiếng Việt, hỗ trợ markdown: **bold**, - list"",
  ""hasAction"": true/false,
  ""actionType"": ""CREATE_PRODUCT_AND_IMPORT"" | ""CREATE_PROMOTION_CAMPAIGN"" | ""SEND_VIP_REWARD"" | null,
  ""actionPayload"": {{ ... }}
}}

1. Khi user muốn NHẬP SẢN PHẨM MỚI hoặc HỎI VỀ XU HƯỚNG / TOP 5 SẢN PHẨM HOT TRÊN THỊ TRƯỜNG ĐỂ NHẬP HÀNG (ActionType: CREATE_PRODUCT_AND_IMPORT):
   actionPayload: {{
     ""tenSanPham"": ""iPhone 16 Pro Max 256GB"",
     ""hinhAnh"": ""https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80"",
     ""sku"": ""AP-IP16PM-256G-DES"",
     ""giaNhap"": 29500000,
     ""giaBan"": 34990000,
     ""soLuongNhap"": 20,
     ""maDanhMuc"": 1,
     ""maThuongHieu"": 1,
     ""maNCC"": 3,
     ""tenNCC"": ""Apple Vietnam"",
     ""bienThe"": ""Titan Sa Mạc | 256GB | 8GB RAM"",
     ""moTa"": ""Siêu phẩm công nghệ hot nhất thị trường hiện tại, khung Titan siêu nhẹ, chip A18 Pro hiệu năng cao."",
     ""lyDoDeXuat"": ""Nhu cầu thị trường cực cao, biên lợi nhuận tốt (~18%), phù hợp nhập bán ngay."",
     ""top5Products"": [
       {{ ""tenSanPham"": ""iPhone 16 Pro Max 256GB"", ""hinhAnh"": ""https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80"", ""sku"": ""AP-IP16PM-256G-DES"", ""giaNhap"": 29500000, ""giaBan"": 34990000, ""soLuongNhap"": 20, ""maDanhMuc"": 1, ""maThuongHieu"": 1, ""maNCC"": 3, ""tenNCC"": ""Apple Vietnam"", ""bienThe"": ""Titan Sa Mạc | 256GB"", ""lyDoDeXuat"": ""Top 1 Flagship hot nhất năm"" }},
       {{ ""tenSanPham"": ""Samsung Galaxy S25 Ultra"", ""hinhAnh"": ""https://images.unsplash.com/photo-1510557880182-3f8ed9f4a7b6?auto=format&fit=crop&w=800&q=80"", ""sku"": ""SS-S25U-512G-GRY"", ""giaNhap"": 27000000, ""giaBan"": 32990000, ""soLuongNhap"": 15, ""maDanhMuc"": 1, ""maThuongHieu"": 2, ""maNCC"": 2, ""tenNCC"": ""Samsung Vietnam"", ""bienThe"": ""Xám Titan | 512GB"", ""lyDoDeXuat"": ""AI Phone camera 200MP dẫn đầu xu hướng"" }},
       {{ ""tenSanPham"": ""Xiaomi 15 Ultra"", ""hinhAnh"": ""https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80"", ""sku"": ""XI-XM15U-512G-BLK"", ""giaNhap"": 19500000, ""giaBan"": 24990000, ""soLuongNhap"": 25, ""maDanhMuc"": 1, ""maThuongHieu"": 3, ""maNCC"": 4, ""tenNCC"": ""Xiaomi Vietnam"", ""bienThe"": ""Đen Nhám | 512GB | 16GB RAM"", ""lyDoDeXuat"": ""Camera Leica đỉnh cao, giá cạnh tranh"" }},
       {{ ""tenSanPham"": ""MacBook Air 15 inch M3"", ""hinhAnh"": ""https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=800&q=80"", ""sku"": ""AP-MBA15M3-16G-SLV"", ""giaNhap"": 28000000, ""giaBan"": 33990000, ""soLuongNhap"": 10, ""maDanhMuc"": 2, ""maThuongHieu"": 1, ""maNCC"": 3, ""tenNCC"": ""Apple Vietnam"", ""bienThe"": ""Bạc Starlight | 16GB RAM | 512GB SSD"", ""lyDoDeXuat"": ""Laptop mỏng nhẹ bán chạy nhất phân khúc cao cấp"" }},
       {{ ""tenSanPham"": ""Sony WH-1000XM5"", ""hinhAnh"": ""https://images.unsplash.com/photo-1511367461989-f85a21fda167?auto=format&fit=crop&w=800&q=80"", ""sku"": ""SN-WH1000XM5-SLV"", ""giaNhap"": 5500000, ""giaBan"": 7490000, ""soLuongNhap"": 30, ""maDanhMuc"": 4, ""maThuongHieu"": 7, ""maNCC"": 1, ""tenNCC"": ""NovaTech Logistics"", ""bienThe"": ""Bạc Bạch Kim | Bluetooth 5.2"", ""lyDoDeXuat"": ""Tai nghe chống ồn đỉnh cao luôn cháy hàng"" }}
     ]
   }}

2. Khi user muốn KHUYẾN MÃI / XẢ HÀNG TỒN (ActionType: CREATE_PROMOTION_CAMPAIGN):
   actionPayload: {{ ""maCode"": ""SALE15"", ""giaTri"": 15, ""soLuongNhap"": 50, ""lyDoDeXuat"": ""Tạo khuyến mãi 15% kích cầu sản phẩm tồn kho cao"" }}

3. Khi user muốn TRI ÂN KHÁCH HÀNG VIP (ActionType: SEND_VIP_REWARD):
   actionPayload: {{ ""maCode"": ""VIP2026"", ""giaTri"": 20, ""soLuongNhap"": 20, ""danhSachKhachHang"": ""{topCustomersText}"", ""lyDoDeXuat"": ""Tặng mã giảm giá 20% cho Top 5 khách hàng VIP"" }}

4. Khi user muốn TÌM SP BÁN CHẬM + TẠO VOUCHER 15% + GỬI EMAIL CHO KHÁCH HÀNG ĐỒNG TRỞ LÊN (ActionType: SEND_PROMO_EMAIL_DONG_PLUS):
   actionPayload: {{ ""maCode"": ""SALE15_{DateTime.Now:MMyy}"", ""giaTri"": 15, ""soLuongNhap"": 100, ""lyDoDeXuat"": ""Top 3 SP bán chậm tháng {DateTime.Now.Month}: {slowMovingTop3.FirstOrDefault()?.Product.TenSanPham}..."" }}
   Điều kiện trigger: khi user đề cập đến việc gửi email khuyến mãi cho khách hàng Đồng/tất cả khách, tìm sản phẩm bán chậm và gửi email.

Nếu là câu hỏi thông thường, đặt hasAction = false, actionType = null, actionPayload = null.
KHÔNG bao giờ trả về text bên ngoài JSON.";

            string rawJson = await _geminiService.GenerateActionResponseAsync(systemInstruction, req.Message);

            // Parse AI response
            AiActionResponse? aiResp = null;
            try
            {
                string cleanJson = rawJson.Trim();
                if (cleanJson.StartsWith("```")) cleanJson = System.Text.RegularExpressions.Regex.Replace(cleanJson, @"```[a-z]*\n?", "").Replace("```", "").Trim();
                aiResp = JsonSerializer.Deserialize<AiActionResponse>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                aiResp = null;
            }

            // Nếu không Deserialize thành công JSON (do Gemini API trả về chuỗi lỗi hoặc định dạng khác), sử dụng trực tiếp chuỗi rawJson
            if (aiResp == null || string.IsNullOrWhiteSpace(aiResp.Message))
            {
                aiResp = new AiActionResponse
                {
                    Message = rawJson,
                    HasAction = false
                };
            }

            // Save AI message with per-user isolation
            _context.ChatMessages.Add(new ChatMessage
            {
                Sender = $"AI:{userEmail}",
                Message = aiResp.Message ?? "",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = aiResp.Message,
                hasAction = aiResp.HasAction,
                actionType = aiResp.ActionType,
                actionPayload = aiResp.ActionPayload
            });
        }

        // ─── Execute Action Endpoint ────────────────────────────────────────────────
        [HttpPost("ExecuteAction")]
        public async Task<IActionResult> ExecuteAction([FromBody] ExecuteActionRequest req)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized(new { error = "Chưa đăng nhập" });

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.Email == userEmail);

            // 1. Action: CREATE_PRODUCT_AND_IMPORT
            if (req?.ActionType == "CREATE_PRODUCT_AND_IMPORT" && req.ActionPayload != null)
            {
                try
                {
                    var payload = req.ActionPayload;
                    int maNCC = payload.MaNCC > 0 ? payload.MaNCC : (_context.NhaCungCaps.FirstOrDefault()?.MaNCC ?? 1);
                    int maDanhMuc = payload.MaDanhMuc > 0 ? payload.MaDanhMuc : (_context.DanhMucs.FirstOrDefault()?.MaDanhMuc ?? 1);
                    int maThuongHieu = payload.MaThuongHieu > 0 ? payload.MaThuongHieu : (_context.ThuongHieus.FirstOrDefault()?.MaThuongHieu ?? 1);

                    if (!await _context.NhaCungCaps.AnyAsync(n => n.MaNCC == maNCC)) maNCC = (await _context.NhaCungCaps.FirstAsync()).MaNCC;
                    if (!await _context.DanhMucs.AnyAsync(d => d.MaDanhMuc == maDanhMuc)) maDanhMuc = (await _context.DanhMucs.FirstAsync()).MaDanhMuc;
                    if (!await _context.ThuongHieus.AnyAsync(t => t.MaThuongHieu == maThuongHieu)) maThuongHieu = (await _context.ThuongHieus.FirstAsync()).MaThuongHieu;

                    string hinhAnhUrl = !string.IsNullOrWhiteSpace(payload.HinhAnh)
                        ? payload.HinhAnh
                        : "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80";

                    string moTaFull = (payload.MoTa ?? "") + (string.IsNullOrWhiteSpace(payload.BienThe) ? "" : $"\n📌 Biến thể: {payload.BienThe}");

                    var newProduct = new SanPham
                    {
                        TenSanPham = payload.TenSanPham ?? "Sản phẩm mới",
                        SKU = payload.SKU ?? $"AI-{DateTime.Now:yyyyMMddHHmm}",
                        MaDanhMuc = maDanhMuc,
                        MaThuongHieu = maThuongHieu,
                        MaNCC = maNCC,
                        GiaNhap = payload.GiaNhap,
                        GiaBan = payload.GiaBan,
                        SoLuongTon = 0,
                        MoTa = moTaFull,
                        HinhAnh = hinhAnhUrl,
                        TrangThai = "Đang bán"
                    };
                    _context.SanPhams.Add(newProduct);
                    await _context.SaveChangesAsync();

                    var phieuNhap = new PhieuNhap
                    {
                        MaNCC = maNCC,
                        MaNhanVien = nhanVien?.MaNhanVien ?? 1,
                        NgayNhap = DateTime.Now
                    };
                    _context.PhieuNhaps.Add(phieuNhap);
                    await _context.SaveChangesAsync();

                    var chiTiet = new ChiTietPhieuNhap
                    {
                        MaPhieuNhap = phieuNhap.MaPhieuNhap,
                        MaSanPham = newProduct.MaSanPham,
                        SoLuong = payload.SoLuongNhap,
                        GiaNhap = payload.GiaNhap,
                        GiaNiemYetLucNhap = newProduct.GiaBan
                    };
                    _context.ChiTietPhieuNhaps.Add(chiTiet);

                    int txCount = await _context.InventoryTransactions.CountAsync(t => t.Type == "Nhập kho") + 1;
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        Code = "AI-" + txCount.ToString("D6"),
                        Type = "Nhập kho",
                        ProductSKU = newProduct.MaSanPham.ToString(),
                        ProductName = newProduct.TenSanPham,
                        QuantityChange = payload.SoLuongNhap,
                        Creator = $"{nhanVien?.HoTen ?? "Admin"} (AI Assistant)",
                        Date = DateTime.Now,
                        Note = $"AI tự động tạo đề xuất. Chờ duyệt để cộng kho. Lý do: {payload.LyDoDeXuat}",
                        TrangThai = "Chờ duyệt"
                    });
                    await _context.SaveChangesAsync();

                    _context.SystemNotifications.Add(new SystemNotification
                    {
                        Title = "🤖 Đề xuất nhập hàng từ AI đang chờ duyệt",
                        Message = $"Sản phẩm \"{newProduct.TenSanPham}\" (Mã #{newProduct.MaSanPham}) đã tạo phiếu nhập #{phieuNhap.MaPhieuNhap}. SL: {payload.SoLuongNhap} chờ duyệt tại Trung tâm duyệt phiếu.",
                        Type = "Thông tin",
                        Timestamp = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    string confirmMsg = $"✅ **Đã tự động tạo sản phẩm & phiếu nhập kho #{phieuNhap.MaPhieuNhap}!**\n- Sản phẩm: **{newProduct.TenSanPham}**\n- Số lượng: **{payload.SoLuongNhap} cái**\n- Trạng thái: **Chờ duyệt tại Trung tâm phê duyệt kho**.";
                    _context.ChatMessages.Add(new ChatMessage { Sender = $"AI:{userEmail}", Message = confirmMsg, Timestamp = DateTime.Now });
                    await _context.SaveChangesAsync();

                    return Ok(new { success = true, message = confirmMsg });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = $"Lỗi: {ex.Message}" });
                }
            }

            // 2. Action: CREATE_PROMOTION_CAMPAIGN
            if (req?.ActionType == "CREATE_PROMOTION_CAMPAIGN" && req.ActionPayload != null)
            {
                try
                {
                    var p = req.ActionPayload;
                    string code = string.IsNullOrWhiteSpace(p.MaCode) ? $"SALE{DateTime.Now:MMddHH}" : p.MaCode.ToUpper();
                    decimal giaTri = p.GiaTri > 0 ? p.GiaTri : 15;
                    int soLuong = p.SoLuongNhap > 0 ? p.SoLuongNhap : 50;

                    var voucher = new Voucher
                    {
                        MaCode = code,
                        GiaTri = giaTri,
                        SoLuong = soLuong,
                        NgayBatDau = DateTime.Now,
                        NgayKetThuc = DateTime.Now.AddDays(30)
                    };
                    _context.Vouchers.Add(voucher);
                    await _context.SaveChangesAsync();

                    _context.SystemNotifications.Add(new SystemNotification
                    {
                        Title = $"🔥 AI Khởi tạo Mã Khuyến Mãi {code}",
                        Message = $"Voucher giảm {giaTri}% ({code}) đã kích hoạt tự động với số lượng {soLuong} lượt dùng. Lý do: {p.LyDoDeXuat}",
                        Type = "Khuyến mãi",
                        Timestamp = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    string confirmMsg = $"🎉 **Đã kích hoạt Chiến dịch Khuyến mãi thành công!**\n\n🎟️ **Mã Voucher:** `{code}`\n💰 **Mức giảm:** {giaTri}%\n📦 **Số lượng phát hành:** {soLuong} lượt\n📅 **Hạn sử dụng:** 30 ngày kể từ hôm nay\n\n📢 *Đã tự động đăng thông báo đến toàn hệ thống Cửa hàng Online.*";
                    _context.ChatMessages.Add(new ChatMessage { Sender = $"AI:{userEmail}", Message = confirmMsg, Timestamp = DateTime.Now });
                    await _context.SaveChangesAsync();

                    return Ok(new { success = true, message = confirmMsg, code = code });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = $"Lỗi khi tạo khuyến mãi: {ex.Message}" });
                }
            }

            // 3. Action: SEND_VIP_REWARD
            if (req?.ActionType == "SEND_VIP_REWARD" && req.ActionPayload != null)
            {
                try
                {
                    var p = req.ActionPayload;
                    string code = string.IsNullOrWhiteSpace(p.MaCode) ? $"VIP{DateTime.Now:MMddHH}" : p.MaCode.ToUpper();
                    decimal giaTri = p.GiaTri > 0 ? p.GiaTri : 20;

                    var voucher = new Voucher
                    {
                        MaCode = code,
                        GiaTri = giaTri,
                        SoLuong = 20,
                        NgayBatDau = DateTime.Now,
                        NgayKetThuc = DateTime.Now.AddDays(15)
                    };
                    _context.Vouchers.Add(voucher);
                    await _context.SaveChangesAsync();

                    _context.SystemNotifications.Add(new SystemNotification
                    {
                        Title = $"💎 AI Tri ân Khách hàng VIP mã {code}",
                        Message = $"Mã tri ân {code} giảm {giaTri}% đã gửi đến nhóm Khách hàng VIP: {p.DanhSachKhachHang}",
                        Type = "VIP",
                        Timestamp = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    string targetCustomers = string.IsNullOrWhiteSpace(p.DanhSachKhachHang) ? "Top Khách hàng thân thiết" : p.DanhSachKhachHang;
                    string confirmMsg = $"👑 **Đã thực hiện gửi quà Tri ân Khách hàng VIP!**\n\n💎 **Mã ưu đãi VIP:** `{code}` (Giảm {giaTri}%)\n👥 **Đối tượng nhận:** {targetCustomers}\n📩 *Đã tự động ghi nhận quà tặng tri ân vào hệ thống.*";
                    _context.ChatMessages.Add(new ChatMessage { Sender = $"AI:{userEmail}", Message = confirmMsg, Timestamp = DateTime.Now });
                    await _context.SaveChangesAsync();

                    return Ok(new { success = true, message = confirmMsg, code = code });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = $"Lỗi tri ân VIP: {ex.Message}" });
                }
            }

            // 4. Action: SEND_PROMO_EMAIL_DONG_PLUS
            if (req?.ActionType == "SEND_PROMO_EMAIL_DONG_PLUS" && req.ActionPayload != null)
            {
                try
                {
                    var p = req.ActionPayload;
                    string code = string.IsNullOrWhiteSpace(p.MaCode)
                        ? $"SALE15_{DateTime.Now:MMyy}"
                        : p.MaCode.ToUpper();
                    decimal giaTri = p.GiaTri > 0 ? p.GiaTri : 15;
                    int soLuong = p.SoLuongNhap > 0 ? p.SoLuongNhap : 100;

                    // ─── Bước 1: Tìm 3 SP tồn kho cao + bán chậm nhất tháng này ───
                    var allProducts = await _context.SanPhams.ToListAsync();
                    var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    var monthOrders = await _context.DonHangs
                        .Where(o => o.NgayDat >= startOfMonth && o.TrangThai == "Hoàn thành")
                        .ToListAsync();
                    var monthOrderIds = monthOrders.Select(o => o.MaDonHang).ToHashSet();
                    var monthDetails = await _context.ChiTietDonHangs.ToListAsync();
                    var soldMap = monthDetails
                        .Where(ct => monthOrderIds.Contains(ct.MaDonHang))
                        .GroupBy(ct => ct.MaSanPham)
                        .ToDictionary(g => g.Key, g => g.Sum(ct => ct.SoLuong));

                    var top3SlowProducts = allProducts
                        .Where(sp => sp.SoLuongTon > 0 && sp.TrangThai == "Đang bán")
                        .Select(sp => new
                        {
                            Product = sp,
                            SoldQty = soldMap.TryGetValue(sp.MaSanPham, out var s) ? s : 0
                        })
                        .OrderBy(x => x.SoldQty)
                        .ThenByDescending(x => x.Product.SoLuongTon)
                        .Take(3)
                        .ToList();

                    // ─── Bước 2: Tạo Voucher 15% vào CSDL ───
                    var existingVoucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.MaCode == code);
                    if (existingVoucher == null)
                    {
                        _context.Vouchers.Add(new Voucher
                        {
                            MaCode = code,
                            GiaTri = giaTri,
                            SoLuong = soLuong,
                            NgayBatDau = DateTime.Now,
                            NgayKetThuc = DateTime.Now.AddDays(30)
                        });
                        await _context.SaveChangesAsync();
                    }

                    // ─── Bước 3: Lấy danh sách khách hàng có email hợp lệ (loại bỏ số điện thoại giả) ───
                    var emailRegex = new System.Text.RegularExpressions.Regex(
                        @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    var allCandidates = await _context.KhachHangs
                        .Where(k => !string.IsNullOrEmpty(k.Email))
                        .ToListAsync();

                    // Lấy email cụ thể từ payload nếu có
                    string? explicitTargetEmail = req.ActionPayload?.TargetEmail;

                    // Lọc: email đúng định dạng, không chứa tiktok.com, guest, vanglai, novatech.vn
                    var dongPlusCustomers = allCandidates
                        .Where(k =>
                            emailRegex.IsMatch(k.Email ?? "") &&
                            k.DiemTichLuy >= 0 &&
                            !(k.Email ?? "").Contains("guest") &&
                            !(k.Email ?? "").Contains("vanglai") &&
                            !(k.Email ?? "").Contains("novatech.vn") &&
                            !(k.Email ?? "").Contains("tiktok.com"))
                        .ToList();

                    // Đọc targetRank từ payload nếu có
                    string? explicitTargetRank = req.ActionPayload?.TargetRank;

                    // Nếu user có yêu cầu gửi cho 1 Email cụ thể, chỉ gửi duy nhất cho Email đó (hoặc ưu tiên)
                    if (!string.IsNullOrEmpty(explicitTargetEmail))
                    {
                        var explicitKh = allCandidates.FirstOrDefault(k => (k.Email ?? "").Equals(explicitTargetEmail, StringComparison.OrdinalIgnoreCase));
                        if (explicitKh == null)
                        {
                            // Email không phải khách hàng NovaTech → từ chối gửi
                            return Ok(new { success = false, message = $"❌ Email `{explicitTargetEmail}` không phải là khách hàng của NovaTech. Không thể gửi voucher." });
                        }
                        dongPlusCustomers = new List<DATN64.Models.KhachHang> { explicitKh };
                    }
                    else if (!string.IsNullOrEmpty(explicitTargetRank))
                    {
                        // Lọc theo Rank: Kim Cương (>=3000), Vàng (1500-2999), Bạc (500-1499), Đồng (0-499)
                        dongPlusCustomers = dongPlusCustomers.Where(k =>
                        {
                            int d = k.DiemTichLuy;
                            return explicitTargetRank switch
                            {
                                "Kim Cương" => d >= 3000,
                                "Vàng" => d >= 1500 && d < 3000,
                                "Bạc" => d >= 500 && d < 1500,
                                "Đồng" => d < 500,
                                _ => true
                            };
                        }).ToList();
                    }

                    // Log để debug
                    Console.WriteLine($"[EMAIL] Tổng khách trong DB: {allCandidates.Count}, sau lọc email hợp lệ: {dongPlusCustomers.Count}");
                    foreach (var kh in dongPlusCustomers)
                        Console.WriteLine($"[EMAIL]   -> {kh.Email} (Điểm: {kh.DiemTichLuy})");

                    // ─── Bước 4: Gửi Email HTML cho từng khách ───
                    int sentCount = 0;
                    int failCount = 0;
                    var failedEmails = new List<string>();

                    // Build email HTML template
                    string GetRankBadge(int diem) => diem >= 3000 ? "💎 Kim Cương" : diem >= 1500 ? "🥇 Vàng" : diem >= 500 ? "🥈 Bạc" : "🥉 Đồng";

                    string productRowsHtml = string.Join("", top3SlowProducts.Select((item, idx) =>
                    {
                        var sp = item.Product;
                        decimal discountedPrice = sp.GiaBan * (1 - giaTri / 100m);
                        string imgSrc = !string.IsNullOrEmpty(sp.HinhAnh)
                            ? (sp.HinhAnh.StartsWith("http") ? sp.HinhAnh : sp.HinhAnh)
                            : "https://via.placeholder.com/80x80?text=SP";
                        return $@"
                        <tr>
                          <td style='padding:12px;border-bottom:1px solid #f0f0f0;'>
                            <table cellpadding='0' cellspacing='0' width='100%'><tr>
                              <td width='90' style='vertical-align:top;'>
                                <img src='{imgSrc}' width='80' height='80' style='border-radius:8px;object-fit:cover;' />
                              </td>
                              <td style='padding-left:12px;vertical-align:top;'>
                                <div style='font-weight:700;color:#1a1a2e;font-size:14px;'>{sp.TenSanPham}</div>
                                <div style='margin-top:4px;font-size:13px;'>
                                  <span style='text-decoration:line-through;color:#999;'>{sp.GiaBan:N0}đ</span>
                                  &nbsp;<span style='color:#e63946;font-weight:700;font-size:15px;'>{discountedPrice:N0}đ</span>
                                  &nbsp;<span style='background:#e63946;color:#fff;border-radius:4px;padding:2px 6px;font-size:11px;font-weight:700;'>-{giaTri}%</span>
                                </div>
                                <div style='font-size:12px;color:#666;margin-top:4px;'>Tồn kho: {sp.SoLuongTon} sản phẩm</div>
                              </td>
                            </tr></table>
                          </td>
                        </tr>";
                    }));

                    string lastEmailError = "";
                    foreach (var kh in dongPlusCustomers)
                    {
                        try
                        {
                            string rankBadge = GetRankBadge(kh.DiemTichLuy);
                            string emailHtml = $@"<!DOCTYPE html>
<html lang='vi'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>
<body style='margin:0;padding:0;background:#f4f7fc;font-family:Arial,sans-serif;'>
  <table cellpadding='0' cellspacing='0' width='100%' style='background:#f4f7fc;padding:30px 0;'>
    <tr><td align='center'>
      <table cellpadding='0' cellspacing='0' width='600' style='background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>

        <!-- Header -->
        <tr><td style='background:linear-gradient(135deg,#1a1a2e 0%,#16213e 50%,#0f3460 100%);padding:32px 40px;text-align:center;'>
          <div style='color:#e94560;font-size:28px;font-weight:900;letter-spacing:2px;'>NOVA<span style='color:#ffffff;'>TECH</span></div>
          <div style='color:#a0aec0;font-size:13px;margin-top:4px;'>Cửa hàng Công nghệ hàng đầu</div>
        </td></tr>

        <!-- Hero Banner -->
        <tr><td style='background:linear-gradient(135deg,#e63946,#c1121f);padding:28px 40px;text-align:center;'>
          <div style='font-size:40px;'>🔥</div>
          <div style='color:#fff;font-size:26px;font-weight:900;margin-top:8px;'>ƯU ĐÃI ĐẶC BIỆT</div>
          <div style='color:#ffe5e5;font-size:15px;margin-top:6px;'>Dành riêng cho thành viên {rankBadge}</div>
        </td></tr>

        <!-- Greeting -->
        <tr><td style='padding:28px 40px 12px;'>
          <div style='font-size:16px;color:#333;'>Xin chào <strong style='color:#0f3460;'>{kh.HoTen}</strong>! 👋</div>
          <div style='color:#555;font-size:14px;margin-top:8px;line-height:1.6;'>
            NovaTech trân trọng gửi đến bạn ưu đãi giảm giá <strong style='color:#e63946;'>{giaTri}%</strong> 
            cho các sản phẩm đang có tồn kho cao trong tháng {DateTime.Now.Month}/{DateTime.Now.Year}.
            Đừng bỏ lỡ cơ hội này!
          </div>
        </td></tr>

        <!-- Voucher Code -->
        <tr><td style='padding:12px 40px;'>
          <table cellpadding='0' cellspacing='0' width='100%'>
            <tr><td style='background:#fff8f0;border:2px dashed #e63946;border-radius:12px;padding:20px;text-align:center;'>
              <div style='font-size:13px;color:#777;'>🎟️ Mã giảm giá của bạn</div>
              <div style='font-size:32px;font-weight:900;color:#e63946;letter-spacing:4px;margin:8px 0;font-family:monospace;'>{code}</div>
              <div style='font-size:12px;color:#999;'>Giảm {giaTri}% • Hạn sử dụng: 30 ngày • {soLuong} lượt dùng</div>
            </td></tr>
          </table>
        </td></tr>

        <!-- Products -->
        <tr><td style='padding:20px 40px 12px;'>
          <div style='font-size:16px;font-weight:700;color:#1a1a2e;margin-bottom:12px;'>🛒 Sản phẩm áp dụng ưu đãi:</div>
          <table cellpadding='0' cellspacing='0' width='100%' style='border:1px solid #f0f0f0;border-radius:12px;overflow:hidden;'>
            {productRowsHtml}
          </table>
        </td></tr>

        <!-- CTA Button -->
        <tr><td style='padding:24px 40px;text-align:center;'>
          <a href='https://localhost:5001/Online/ProductsList' 
             style='background:linear-gradient(135deg,#e63946,#c1121f);color:#fff;text-decoration:none;padding:14px 40px;border-radius:50px;font-size:16px;font-weight:700;display:inline-block;box-shadow:0 4px 16px rgba(230,57,70,0.4);'>🛍️ Mua ngay tại NovaTech</a>
        </td></tr>

        <!-- Footer -->
        <tr><td style='background:#f8f9fa;padding:20px 40px;text-align:center;border-top:1px solid #eee;'>
          <div style='font-size:12px;color:#999;line-height:1.6;'>
            © {DateTime.Now.Year} NovaTech Store. Mọi thắc mắc liên hệ qua website.<br/>
            <span style='color:#ccc;'>Email này được gửi tự động bởi hệ thống AI NovaTech ERP.</span>
          </div>
        </td></tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

                            _emailService.SendEmail(
                                kh.Email!,
                                $"🔥 [{rankBadge}] Ưu đãi {giaTri}% đặc biệt từ NovaTech - Mã {code}",
                                emailHtml
                            );
                            sentCount++;
                            Console.WriteLine($"[EMAIL SUCCESS] Gửi thành công tới: {kh.Email}");
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            lastEmailError = ex.Message;
                            failedEmails.Add(kh.Email ?? "?");
                            Console.WriteLine($"[EMAIL FAILED] Gửi thất bại tới: {kh.Email} | Lỗi: {ex.Message}");
                        }
                    }

                    // ─── Bước 5: Tạo System Notification ───
                    string productSummary = string.Join(", ", top3SlowProducts.Select(x => x.Product.TenSanPham));
                    _context.SystemNotifications.Add(new SystemNotification
                    {
                        Title = $"🤖 AI Gửi Email Khuyến Mãi Thành Công ({sentCount} email)",
                        Message = $"Voucher {code} giảm {giaTri}% đã gửi đến {sentCount}/{dongPlusCustomers.Count} khách hàng có email hợp lệ. SP xả hàng: {productSummary}. Thất bại: {failCount}.",
                        Type = "Khuyến mãi",
                        Timestamp = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    // ─── Bước 6: Phản hồi Chat ───
                    string product1 = top3SlowProducts.Count > 0 ? top3SlowProducts[0].Product.TenSanPham : "N/A";
                    string product2 = top3SlowProducts.Count > 1 ? top3SlowProducts[1].Product.TenSanPham : "N/A";
                    string product3 = top3SlowProducts.Count > 2 ? top3SlowProducts[2].Product.TenSanPham : "N/A";
                    string smtpNote = failCount > 0 ? $"({lastEmailError})" : "";

                    string confirmMsg =
                        $"\U0001f3af **AI đã hoàn thành chiến dịch!**\n\n" +
                        $"\U0001f4ca **Phân tích tháng {DateTime.Now.Month}/{DateTime.Now.Year}:**\n" +
                        $"- TOP 1 bán chậm: **{product1}**\n" +
                        $"- TOP 2 bán chậm: **{product2}**\n" +
                        $"- TOP 3 bán chậm: **{product3}**\n\n" +
                        $"**Voucher đã tạo:** `{code}` - Giảm **{giaTri}%** | {soLuong} lượt | Hạn 30 ngày\n\n" +
                        $"**Kết quả gửi Email:**\n" +
                        $"- ✅ Gửi thành công: **{sentCount}/{dongPlusCustomers.Count}** khách hàng\n" +
                        $"- ❌ Thất bại: **{failCount}** {smtpNote}\n\n" +
                        $"*Chiến dịch xả hàng tồn kho đã được kích hoạt!*";

                    _context.ChatMessages.Add(new ChatMessage { Sender = $"AI:{userEmail}", Message = confirmMsg, Timestamp = DateTime.Now });
                    await _context.SaveChangesAsync();

                    return Ok(new { success = true, message = confirmMsg, code = code, sentCount, failCount, totalCustomers = dongPlusCustomers.Count });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = $"Lỗi chiến dịch khuyến mãi: {ex.Message}" });
                }
            }

            return BadRequest(new { error = "Action không hợp lệ hoặc chưa được hỗ trợ" });
        }

        // ─── Legacy POST ───────────────────────────────────────────────────────────
        [HttpPost("AskAi")]
        public async Task<IActionResult> AskAi(string question)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(question)) return RedirectToAction("Index");

            _context.ChatMessages.Add(new ChatMessage { Sender = $"User:{userEmail}", Message = question, Timestamp = DateTime.Now });
            await _context.SaveChangesAsync();

            var orders = await _context.DonHangs.ToListAsync();
            var products = await _context.SanPhams.ToListAsync();

            decimal totalRevenue = orders.Where(o => o.TrangThai == "Hoàn thành").Sum(o => o.TongTien ?? 0);
            int totalOrdersCount = orders.Count;
            int pendingOrdersCount = orders.Count(o => o.TrangThai == "Đơn mới" || o.TrangThai == "Đã xác nhận");
            int completedOrdersCount = orders.Count(o => o.TrangThai == "Hoàn thành");

            var lowStockProductsList = products.Where(p => p.SoLuongTon <= 5).ToList();
            string lowStockText = lowStockProductsList.Any()
                ? string.Join("\n", lowStockProductsList.Select(p => $"- {p.TenSanPham} (Còn: {p.SoLuongTon})"))
                : "- Không có sản phẩm nào sắp hết hàng.";

            string userName = HttpContext.Session.GetString("UserName") ?? "Nhân viên";
            string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            string systemInstruction = $@"Bạn là trợ lý AI thông minh tích hợp trong hệ thống ERP NovaTech. Tài khoản: {userName}. Thời gian: {currentTime}.
Dữ liệu: Doanh thu: {totalRevenue:N0}đ | Tổng đơn: {totalOrdersCount} | Chờ: {pendingOrdersCount} | Hoàn thành: {completedOrdersCount}
Tồn kho thấp: {lowStockText}
Trả lời tiếng Việt, ngắn gọn, chuyên nghiệp. Hỗ trợ markdown: **bold**, - list.";

            string reply = await _geminiService.GenerateResponseAsync(systemInstruction, question);

            if (reply.StartsWith("Lỗi API Gemini") || reply.StartsWith("Lỗi kết nối AI"))
                reply = GenerateLocalResponse(question, totalRevenue, totalOrdersCount, pendingOrdersCount, completedOrdersCount, lowStockText, lowStockProductsList.Count);

            _context.ChatMessages.Add(new ChatMessage { Sender = $"AI:{userEmail}", Message = reply, Timestamp = DateTime.Now });
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost("ClearHistory")]
        public IActionResult ClearHistory()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var messages = _context.ChatMessages
                .Where(m => m.Sender.Contains(userEmail) || (!m.Sender.Contains(":") && userEmail == "admin@novatech.com"))
                .ToList();
            _context.ChatMessages.RemoveRange(messages);
            _context.SaveChanges();

            TempData["ToastMessage"] = "Đã xóa lịch sử trò chuyện của bạn!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        private string GenerateLocalResponse(string question, decimal totalRevenue, int totalOrders, int pendingOrders, int completedOrders, string lowStockText, int lowStockCount)
        {
            string q = question.ToLower();

            if (q.Contains("doanh thu") || q.Contains("tiền") || q.Contains("bán được"))
                return $"📊 **Báo cáo doanh thu:**\n\n💰 **{totalRevenue:N0} đ** doanh thu hoàn thành từ {completedOrders} đơn.";

            if (q.Contains("đơn hàng") || q.Contains("don hang"))
                return $"📦 **Đơn hàng:** Tổng {totalOrders} | ⏳ Chờ: {pendingOrders} | ✅ Hoàn thành: {completedOrders}";

            if (q.Contains("tồn kho") || q.Contains("kho") || q.Contains("hết hàng"))
            {
                return lowStockCount > 0
                    ? $"⚠️ **{lowStockCount} sản phẩm sắp hết:**\n{lowStockText}"
                    : "✅ Tồn kho ổn định, không có sản phẩm nào dưới ngưỡng tối thiểu.";
            }

            return $"👋 Xin chào **{HttpContext.Session.GetString("UserName")}**! Tôi đang ở chế độ offline. Hỏi tôi về doanh thu, đơn hàng, hoặc tồn kho nhé!";
        }
    }

    // ─── Request/Response DTOs ──────────────────────────────────────────────────
    public class ChatRequest
    {
        public string? Message { get; set; }
    }

    public class ExecuteActionRequest
    {
        public string? ActionType { get; set; }
        public ActionPayloadDto? ActionPayload { get; set; }
    }

    public class ActionPayloadDto
    {
        public string? TenSanPham { get; set; }
        public string? MoTa { get; set; }
        public decimal GiaNhap { get; set; }
        public decimal GiaBan { get; set; }
        public int SoLuongNhap { get; set; }
        public int MaDanhMuc { get; set; }
        public int MaThuongHieu { get; set; }
        public int MaNCC { get; set; }
        public string? SKU { get; set; }
        public string? LyDoDeXuat { get; set; }

        // New properties for Image, Supplier name, and Variant specs
        public string? HinhAnh { get; set; }
        public string? TenNCC { get; set; }
        public string? BienThe { get; set; }

        // Top 5 products array for market recommendations
        public List<ActionPayloadDto>? Top5Products { get; set; }

        // New properties for Promotion & VIP actions
        public string? MaCode { get; set; }
        public decimal GiaTri { get; set; }
        public string? DanhSachKhachHang { get; set; }

        // Target recipient properties
        public string? TargetEmail { get; set; }
        public string? TargetName { get; set; }
        public string? TargetRank { get; set; }
    }

    public class AiActionResponse
    {
        public string? Message { get; set; }
        public bool HasAction { get; set; }
        public string? ActionType { get; set; }
        public JsonElement? ActionPayload { get; set; }
    }
}
