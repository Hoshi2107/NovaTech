using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http;

namespace DATN64.Controllers
{
    public class AiController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GeminiService _geminiService;

        public AiController(AppDbContext context, GeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var history = _context.ChatMessages.OrderBy(m => m.Timestamp).ToList();
            return View(history);
        }

        [HttpPost]
        public async Task<IActionResult> AskAi(string question)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(question)) return RedirectToAction("Index");

            // Add user message to database
            _context.ChatMessages.Add(new ChatMessage
            {
                Sender = "User",
                Message = question,
                Timestamp = DateTime.Now
            });
            _context.SaveChanges();

            // Fetch live data from Database
            var orders = _context.DonHangs.ToList();
            var products = _context.SanPhams.ToList();

            decimal totalRevenue = orders.Where(o => o.TrangThai == "Hoàn thành").Sum(o => o.TongTien ?? 0);
            int totalOrdersCount = orders.Count;
            int pendingOrdersCount = orders.Count(o => o.TrangThai == "Đơn mới" || o.TrangThai == "Đã xác nhận");
            int completedOrdersCount = orders.Count(o => o.TrangThai == "Hoàn thành");

            // Low stock products (stock <= 5)
            var lowStockProductsList = products.Where(p => p.SoLuongTon <= 5).ToList();
            string lowStockText = "";
            if (lowStockProductsList.Any())
            {
                foreach (var p in lowStockProductsList)
                {
                    lowStockText += $"- {p.TenSanPham} (Còn: {p.SoLuongTon} sản phẩm)\n";
                }
            }
            else
            {
                lowStockText = "- Không có sản phẩm nào sắp hết hàng (tất cả đều tồn kho trên 5 sản phẩm).\n";
            }

            // Current date and time
            string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            string userName = HttpContext.Session.GetString("UserName") ?? "Nhân viên";

            // Construct System Instruction (Developer context)
            string systemInstruction = $@"Bạn là trợ lý AI thông minh tích hợp sẵn trong hệ thống ERP NovaTech (Cửa hàng Công nghệ NovaTech).
Nhiệm vụ của bạn là hỗ trợ ban quản trị và nhân viên phân tích số liệu kinh doanh, kiểm tra tồn kho, đơn hàng và các thông tin khác trong hệ thống.
Tài khoản đang thực hiện cuộc trò chuyện này: {userName}.
Thời gian hệ thống hiện tại: {currentTime}.

Dưới đây là số liệu thực tế được truy vấn trực tiếp từ cơ sở dữ liệu hệ thống ERP NovaTech:
1. TỔNG QUAN DOANH THU & ĐƠN HÀNG:
- Doanh thu hoàn thành (đã thanh toán): {totalRevenue.ToString("N0")} đ
- Tổng số lượng đơn hàng: {totalOrdersCount} đơn
- Số đơn chờ xử lý (mới/đã xác nhận): {pendingOrdersCount} đơn
- Số đơn đã hoàn thành: {completedOrdersCount} đơn

2. SẢN PHẨM & CẢNH BÁO TỒN KHO THẤP (tồn <= 5):
{lowStockText}

HƯỚNG DẪN TRẢ LỜI:
- Trả lời bằng tiếng Việt, ngắn gọn, súc tích, chuyên nghiệp và lịch sự.
- Sử dụng các biểu tượng cảm xúc (emoji) phù hợp để câu trả lời sinh động hơn.
- Khi người dùng hỏi về doanh thu, đơn hàng, hay sản phẩm sắp hết hàng, hãy đối chiếu chính xác các số liệu trên và đưa ra lời khuyên hữu ích (ví dụ: khuyên lập phiếu nhập cho sản phẩm tồn kho thấp).
- Ngoài việc phân tích số liệu nội bộ của ERP NovaTech, bạn ĐƯỢC PHÉP sử dụng kiến thức rộng lớn của mình để phân tích, tư vấn các xu hướng công nghệ, sản phẩm đang hot bên ngoài thị trường để giúp cửa hàng lên kế hoạch nhập hàng hoặc định hướng kinh doanh.
- Nếu câu hỏi kết hợp cả hai (ví dụ: đối chiếu hàng tồn kho hiện tại với xu hướng thị trường), hãy đưa ra phân tích chuyên sâu để tối ưu hóa hiệu quả kinh doanh.
- Hỗ trợ định dạng Markdown cơ bản như: **in đậm**, xuống dòng và - danh sách gạch đầu dòng.";

            // Generate AI response
            string reply = await _geminiService.GenerateResponseAsync(systemInstruction, question);

            // Graceful degradation / Fallback if the API key is blocked/leaked/revoked or not found
            if (reply.StartsWith("Lỗi API Gemini") || reply.StartsWith("Lỗi kết nối AI") || reply.Contains("key") || reply.Contains("leaked") || reply.Contains("not found"))
            {
                reply = GenerateLocalResponse(question, totalRevenue, totalOrdersCount, pendingOrdersCount, completedOrdersCount, lowStockText, lowStockProductsList.Count);
            }

            // Add AI response message to database
            _context.ChatMessages.Add(new ChatMessage
            {
                Sender = "AI",
                Message = reply,
                Timestamp = DateTime.Now
            });
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        private string GenerateLocalResponse(string question, decimal totalRevenue, int totalOrders, int pendingOrders, int completedOrders, string lowStockText, int lowStockCount)
        {
            string q = question.ToLower();
            
            if (q.Contains("doanh thu") || q.Contains("tiền") || q.Contains("bán được"))
            {
                return $@"📊 **Báo cáo doanh thu cửa hàng:**

Hiện tại, hệ thống ghi nhận tổng doanh thu hoàn thành (đã thanh toán thành công) là:
💰 **{totalRevenue.ToString("N0")} đ**

Hệ thống đang hoạt động ổn định. Nếu bạn cần phân tích chuyên sâu hơn, hãy kiểm tra mục **Báo cáo doanh thu** trong menu Hệ thống.";
            }
            
            if (q.Contains("đơn hàng") || q.Contains("don hang") || q.Contains("bán hàng"))
            {
                return $@"📦 **Báo cáo tình trạng đơn hàng:**

Tổng số đơn hàng trên hệ thống: **{totalOrders} đơn**
- ⏳ Đơn hàng chờ xử lý (mới/đã xác nhận): **{pendingOrders} đơn**
- ✅ Đơn hàng đã hoàn thành: **{completedOrders} đơn**

Bạn nên nhanh chóng duyệt các đơn hàng chờ xử lý để đảm bảo tiến độ giao hàng cho khách nhé! 🚚";
            }
            
            if (q.Contains("tồn kho") || q.Contains("kho") || q.Contains("hết hàng") || q.Contains("cảnh báo"))
            {
                string stockWarning = lowStockCount > 0 
                    ? $"Hiện đang có **{lowStockCount} sản phẩm** sắp hết hàng (số lượng tồn kho từ 5 trở xuống):\n\n{lowStockText}\n⚠️ **Khuyến nghị:** Bạn nên nhanh chóng lập phiếu nhập hàng cho các sản phẩm này."
                    : "🎉 Tuyệt vời! Hiện tại không có sản phẩm nào sắp hết hàng (tất cả đều tồn trên 5 sản phẩm).";

                return $@"⚠️ **Cảnh báo tồn kho:**

{stockWarning}";
            }

            if (q.Contains("đề xuất") || q.Contains("phân tích") || q.Contains("khuyên") || q.Contains("tư vấn"))
            {
                string stockAdvice = lowStockCount > 0 
                    ? $"đặc biệt cần chú ý nhập thêm mặt hàng sắp hết hàng (hiện có {lowStockCount} sản phẩm tồn kho thấp)" 
                    : "kho hàng hiện tại rất dồi dào";
                
                return $@"💡 **Phân tích nhanh & Đề xuất kinh doanh:**

- **Doanh thu:** Cửa hàng đạt mức doanh thu hoàn thành **{totalRevenue.ToString("N0")} đ** từ **{completedOrders} đơn hàng**.
- **Vận hành:** Có **{pendingOrders} đơn hàng** đang chờ xử lý. Cần phân bổ nhân sự duyệt đơn sớm trong ngày.
- **Tồn kho:** {stockAdvice}.
- **Khuyến nghị:** 
  1. Duyệt nhanh các đơn hàng mới để tối ưu thời gian giao hàng.
  2. Tạo yêu cầu nhập kho cho các sản phẩm cảnh báo tồn kho thấp.
  3. Chạy thêm chương trình khuyến mãi để kích cầu đối với các dòng sản phẩm tồn kho cao.";
            }

            // General greeting fallback
            return $@"👋 Xin chào! Tôi là **NovaTech AI Assistant**. 

Hiện tại dịch vụ kết nối đám mây đang tạm thời chuyển sang chế độ offline (Local Mode). Tuy nhiên, tôi vẫn có thể hỗ trợ bạn truy vấn dữ liệu thời gian thực của cửa hàng:
- 📊 **Doanh thu** (Hỏi về doanh thu hôm nay...)
- 📦 **Đơn hàng** (Hỏi về số đơn hàng, đơn chờ xử lý...)
- ⚠️ **Tồn kho thấp** (Hỏi về sản phẩm sắp hết hàng...)
- 💡 **Đề xuất** (Yêu cầu phân tích & tư vấn...)

Bạn có thể bấm vào các **Gợi ý nhanh** bên dưới hoặc gõ trực tiếp câu hỏi nhé!";
        }

        [HttpPost]
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
    }
}
