using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
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

        public AiController(AppDbContext context, GeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var history = _context.ChatMessages.OrderBy(m => m.Timestamp).ToList();
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

            // Save user message
            _context.ChatMessages.Add(new ChatMessage
            {
                Sender = "User",
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

            decimal totalRevenue = orders.Where(o => o.TrangThai == "Hoàn thành").Sum(o => o.TongTien ?? 0);
            int totalOrdersCount = orders.Count;
            int pendingOrdersCount = orders.Count(o => o.TrangThai == "Đơn mới" || o.TrangThai == "Đã xác nhận");
            int completedOrdersCount = orders.Count(o => o.TrangThai == "Hoàn thành");

            // Calculate best sellers (Top 5 products based on quantity sold in completed/confirmed orders)
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

            var lowStockProducts = products.Where(p => p.SoLuongTon <= 5).ToList();
            string lowStockText = lowStockProducts.Any()
                ? string.Join("\n", lowStockProducts.Select(p => $"- {p.TenSanPham} (SKU: {p.SKU}, Còn: {p.SoLuongTon})"))
                : "- Không có sản phẩm nào sắp hết hàng.";

            string supplierList = string.Join(", ", suppliers.Select(s => $"{s.MaNCC}: {s.TenNCC}"));
            string categoryList = string.Join(", ", categories.Select(c => $"{c.MaDanhMuc}: {c.TenDanhMuc}"));
            string brandList = string.Join(", ", brands.Select(b => $"{b.MaThuongHieu}: {b.TenThuongHieu}"));
            string productList = string.Join("\n", products.Take(30).Select(p =>
                $"- [{p.MaSanPham}] {p.TenSanPham} | GiaNhap: {p.GiaNhap:N0}đ | GiaBan: {p.GiaBan:N0}đ | Tồn: {p.SoLuongTon}"));

            // ─── System Instruction for Agentic Mode ────────────────────────────────
            string systemInstruction = $@"Bạn là trợ lý AI thông minh tích hợp trong ERP NovaTech - Cửa hàng Công nghệ.
Người dùng: {userName} | Thời gian: {currentTime}

=== DỮ LIỆU THỰC TẾ TỪ HỆ THỐNG ===
TỔNG QUAN:
- Doanh thu hoàn thành: {totalRevenue:N0}đ
- Tổng đơn: {totalOrdersCount} | Chờ xử lý: {pendingOrdersCount} | Hoàn thành: {completedOrdersCount}

SẢN PHẨM BÁN CHẠY NHẤT:
{topProductsText}

CẢNH BÁO TỒN KHO (≤5):
{lowStockText}

DANH SÁCH SẢN PHẨM HIỆN TẠI (30 đầu):
{productList}

NHÀ CUNG CẤP: {supplierList}
DANH MỤC: {categoryList}
THƯƠNG HIỆU: {brandList}

=== QUY TẮC TRẢ LỜI ===
Bạn PHẢI trả về JSON object hợp lệ với cấu trúc CHÍNH XÁC như sau:
{{
  ""message"": ""Nội dung trả lời bằng tiếng Việt, hỗ trợ markdown: **bold**, - list"",
  ""hasAction"": false,
  ""actionType"": null,
  ""actionPayload"": null
}}

Khi người dùng hỏi về sản phẩm mới muốn nhập/kinh doanh và bạn muốn đề xuất tạo phiếu nhập + thêm sản phẩm, dùng:
{{
  ""message"": ""📦 **Đề xuất sản phẩm mới: [TÊN]**\n\n[Mô tả, lý do, phân tích thị trường...]\n\n💰 **Chi phí ước tính:**\n- Giá nhập: X đ/cái\n- Giá bán đề xuất: Y đ/cái\n- Lợi nhuận: Z%\n\n⚠️ Bạn có muốn tôi **tự động tạo phiếu nhập** và **thêm sản phẩm này** vào hệ thống không?"",
  ""hasAction"": true,
  ""actionType"": ""CREATE_PRODUCT_AND_IMPORT"",
  ""actionPayload"": {{
    ""tenSanPham"": ""Tên sản phẩm"",
    ""moTa"": ""Mô tả sản phẩm"",
    ""giaNhap"": 1000000,
    ""giaBan"": 1500000,
    ""soLuongNhap"": 20,
    ""maDanhMuc"": 1,
    ""maThuongHieu"": 1,
    ""maNCC"": 1,
    ""sku"": ""SP-XXX"",
    ""lyDoDeXuat"": ""Lý do ngắn gọn tại sao nên nhập mặt hàng này""
  }}
}}

LƯU Ý QUAN TRỌNG:
- actionPayload.maDanhMuc phải là ID hợp lệ từ danh sách: {categoryList}
- actionPayload.maThuongHieu phải là ID hợp lệ từ danh sách: {brandList}
- actionPayload.maNCC phải là ID hợp lệ từ danh sách: {supplierList}
- Nếu không có NCC/danh mục phù hợp, dùng ID đầu tiên trong danh sách
- Chỉ đề xuất tạo phiếu khi user hỏi về sản phẩm MỚI muốn nhập hoặc có câu hỏi rõ ràng về việc thêm hàng
- Với các câu hỏi thông thường (doanh thu, tồn kho, v.v.), hasAction = false
- KHÔNG bao giờ trả về text ngoài JSON";

            string rawJson = await _geminiService.GenerateActionResponseAsync(systemInstruction, req.Message);

            // Parse AI response
            AiActionResponse? aiResp = null;
            try
            {
                // Strip potential markdown code fences
                string cleanJson = rawJson.Trim();
                if (cleanJson.StartsWith("```")) cleanJson = System.Text.RegularExpressions.Regex.Replace(cleanJson, @"```[a-z]*\n?", "").Replace("```", "").Trim();
                aiResp = JsonSerializer.Deserialize<AiActionResponse>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                // Fallback: treat the raw text as a plain message
                aiResp = new AiActionResponse { Message = rawJson, HasAction = false };
            }

            if (aiResp == null)
                aiResp = new AiActionResponse { Message = "Xin lỗi, tôi gặp sự cố khi xử lý yêu cầu.", HasAction = false };

            // Save AI message
            _context.ChatMessages.Add(new ChatMessage
            {
                Sender = "AI",
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
            if (nhanVien == null) return BadRequest(new { error = "Không tìm thấy nhân viên" });

            if (req?.ActionType == "CREATE_PRODUCT_AND_IMPORT" && req.ActionPayload != null)
            {
                try
                {
                    var payload = req.ActionPayload;

                    // 1. Validate references
                    int maNCC = payload.MaNCC > 0 ? payload.MaNCC : (_context.NhaCungCaps.FirstOrDefault()?.MaNCC ?? 1);
                    int maDanhMuc = payload.MaDanhMuc > 0 ? payload.MaDanhMuc : (_context.DanhMucs.FirstOrDefault()?.MaDanhMuc ?? 1);
                    int maThuongHieu = payload.MaThuongHieu > 0 ? payload.MaThuongHieu : (_context.ThuongHieus.FirstOrDefault()?.MaThuongHieu ?? 1);

                    // Ensure NCC/category/brand exist
                    if (!await _context.NhaCungCaps.AnyAsync(n => n.MaNCC == maNCC)) maNCC = (await _context.NhaCungCaps.FirstAsync()).MaNCC;
                    if (!await _context.DanhMucs.AnyAsync(d => d.MaDanhMuc == maDanhMuc)) maDanhMuc = (await _context.DanhMucs.FirstAsync()).MaDanhMuc;
                    if (!await _context.ThuongHieus.AnyAsync(t => t.MaThuongHieu == maThuongHieu)) maThuongHieu = (await _context.ThuongHieus.FirstAsync()).MaThuongHieu;

                    // 2. Create SanPham (Khoi tao ton kho = 0, cho duyet phieu moi cong kho)
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
                        MoTa = payload.MoTa,
                        TrangThai = "Đang bán"
                    };
                    _context.SanPhams.Add(newProduct);
                    await _context.SaveChangesAsync();

                    // 3. Create PhieuNhap
                    var phieuNhap = new PhieuNhap
                    {
                        MaNCC = maNCC,
                        MaNhanVien = nhanVien.MaNhanVien,
                        NgayNhap = DateTime.Now
                    };
                    _context.PhieuNhaps.Add(phieuNhap);
                    await _context.SaveChangesAsync();

                    // 4. Create ChiTietPhieuNhap
                    var chiTiet = new ChiTietPhieuNhap
                    {
                        MaPhieuNhap = phieuNhap.MaPhieuNhap,
                        MaSanPham = newProduct.MaSanPham,
                        SoLuong = payload.SoLuongNhap,
                        GiaNhap = payload.GiaNhap
                    };
                    _context.ChiTietPhieuNhaps.Add(chiTiet);

                    // 5. Log InventoryTransaction (Trang thai cho duyet, ma SP luu vao SKU de parse duoc ID)
                    int txCount = await _context.InventoryTransactions.CountAsync(t => t.Type == "Nhập kho") + 1;
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        Code = "AI-" + txCount.ToString("D6"),
                        Type = "Nhập kho",
                        ProductSKU = newProduct.MaSanPham.ToString(),
                        ProductName = newProduct.TenSanPham,
                        QuantityChange = payload.SoLuongNhap,
                        Creator = $"{nhanVien.HoTen} (AI Assistant)",
                        Date = DateTime.Now,
                        Note = $"AI tự động tạo đề xuất. Chờ duyệt để cộng kho. Lý do: {payload.LyDoDeXuat}",
                        TrangThai = "Chờ duyệt",
                        SoLuongTruoc = null,
                        SoLuongSau = null
                    });
                    await _context.SaveChangesAsync();

                    // 6. System Notification
                    _context.SystemNotifications.Add(new SystemNotification
                    {
                        Title = "🤖 Đề xuất nhập hàng từ AI đang chờ duyệt",
                        Message = $"Sản phẩm mới \"{newProduct.TenSanPham}\" đã được tạo với số lượng tồn kho = 0. Phiếu nhập #{phieuNhap.MaPhieuNhap} | SL: {payload.SoLuongNhap} đang chờ duyệt tại Trung tâm duyệt phiếu.",
                        Type = "Thông tin",
                        Timestamp = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    decimal tongChiPhi = payload.GiaNhap * payload.SoLuongNhap;
                    decimal loiNhuan = payload.GiaBan - payload.GiaNhap;
                    decimal tyLeLoiNhuan = payload.GiaNhap > 0 ? (loiNhuan / payload.GiaNhap * 100) : 0;

                    string confirmMsg = $@"✅ **Đã tạo sản phẩm và đề xuất nhập kho thành công!**

📦 **Sản phẩm mới:** {newProduct.TenSanPham}
🆔 **Mã SP:** #{newProduct.MaSanPham} | SKU: {newProduct.SKU}
📋 **Phiếu nhập:** #{phieuNhap.MaPhieuNhap} (Trạng thái: **Chờ duyệt**)

💰 **Chi tiết tài chính dự kiến:**
- Số lượng nhập: **{payload.SoLuongNhap} cái**
- Giá nhập: **{payload.GiaNhap:N0} đ/cái**
- Giá bán: **{payload.GiaBan:N0} đ/cái**
- **Tổng chi phí nhập dự kiến: {tongChiPhi:N0} đ**
- Lợi nhuận mỗi cái: {loiNhuan:N0} đ ({tyLeLoiNhuan:F1}%)

⚠️ **Lưu ý:** Sản phẩm đã được thêm vào hệ thống với **số lượng tồn kho ban đầu = 0**. Vui lòng vào **Trung tâm duyệt phiếu** để phê duyệt phiếu kho thì số lượng tồn kho mới được cộng vào và giao dịch mới chính thức hoàn tất.";

                    _context.ChatMessages.Add(new ChatMessage
                    {
                        Sender = "AI",
                        Message = confirmMsg,
                        Timestamp = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    return Ok(new { success = true, message = confirmMsg, productId = newProduct.MaSanPham, phieuNhapId = phieuNhap.MaPhieuNhap });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = $"Lỗi khi thực hiện: {ex.Message}" });
                }
            }

            return BadRequest(new { error = "Action không hợp lệ hoặc chưa được hỗ trợ" });
        }

        // ─── Legacy POST (keep for fallback) ───────────────────────────────────────
        [HttpPost("AskAi")]
        public async Task<IActionResult> AskAi(string question)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(question)) return RedirectToAction("Index");

            // Delegate to the AJAX endpoint logic by building a fake request
            _context.ChatMessages.Add(new ChatMessage { Sender = "User", Message = question, Timestamp = DateTime.Now });
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

            _context.ChatMessages.Add(new ChatMessage { Sender = "AI", Message = reply, Timestamp = DateTime.Now });
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost("ClearHistory")]
        public IActionResult ClearHistory()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var messages = _context.ChatMessages.ToList();
            _context.ChatMessages.RemoveRange(messages);
            _context.SaveChanges();

            TempData["ToastMessage"] = "Đã xóa sạch lịch sử trò chuyện!";
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
    }

    public class AiActionResponse
    {
        public string? Message { get; set; }
        public bool HasAction { get; set; }
        public string? ActionType { get; set; }
        public JsonElement? ActionPayload { get; set; }
    }
}
