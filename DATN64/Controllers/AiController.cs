using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using System.Linq;

namespace DATN64.Controllers
{
    public class AiController : Controller
    {
        private readonly AppDbContext _context;

        public AiController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var history = _context.ChatMessages.ToList();
            return View(history);
        }

        [HttpPost]
        public IActionResult AskAi(string question)
        {
            if (string.IsNullOrEmpty(question)) return RedirectToAction("Index");

            // Add user message
            _context.ChatMessages.Add(new ChatMessage
            {
                Sender = "User",
                Message = question,
                Timestamp = System.DateTime.Now
            });

            // Simulate AI smart response
            string reply = "Tôi đã tiếp nhận câu hỏi của bạn. Hệ thống ERP NovaTech đang ghi nhận tổng doanh thu là " + 
                _context.DonHangs.Where(o => o.TrangThai == "Hoàn thành").Sum(o => o.TongTien ?? 0).ToString("N0") + 
                " đ và có " + _context.SanPhams.Count(p => p.SoLuongTon <= 3) + " sản phẩm sắp hết hàng.";
            
            if (question.Contains("kho", System.StringComparison.OrdinalIgnoreCase))
            {
                var sp = _context.SanPhams.Select(p => $"{p.TenSanPham} (Tồn: {p.SoLuongTon})").Take(5).ToList();
                reply = "Báo cáo kho hiện có (Top 5): " + string.Join(", ", sp);
            }

            _context.ChatMessages.Add(new ChatMessage
            {
                Sender = "AI",
                Message = reply,
                Timestamp = System.DateTime.Now.AddSeconds(1)
            });

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
