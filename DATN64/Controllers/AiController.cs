using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using System.Linq;

namespace DATN64.Controllers
{
    public class AiController : Controller
    {
        public IActionResult Index()
        {
            var history = MockDataService.Instance.ChatHistory.ToList();
            return View(history);
        }

        [HttpPost]
        public IActionResult AskAi(string question)
        {
            if (string.IsNullOrEmpty(question)) return RedirectToAction("Index");

            // Add user message
            MockDataService.Instance.ChatHistory.Add(new MockDataService.ChatMessage
            {
                Sender = "User",
                Message = question,
                Timestamp = System.DateTime.Now
            });

            // Simulate AI smart response
            string reply = "Tôi đã tiếp nhận câu hỏi của bạn. Hệ thống ERP NovaTech đang hiển thị doanh thu tháng này là " + 
                MockDataService.Instance.Orders.Where(o => o.Status == "Hoàn thành").Sum(o => o.Total).ToString("N0") + 
                " đ và có " + MockDataService.Instance.Products.Count(p => p.Stock <= 3) + " sản phẩm sắp hết hàng cần bạn xử lý.";
            
            if (question.Contains("kho", System.StringComparison.OrdinalIgnoreCase))
            {
                reply = "Báo cáo kho hiện có: " + string.Join(", ", MockDataService.Instance.Products.Select(p => $"{p.Name} (Tồn: {p.Stock})"));
            }
            else if (question.Contains("khuyến mãi", System.StringComparison.OrdinalIgnoreCase))
            {
                reply = "Mã voucher hoạt động tốt nhất hiện nay là: NOVATECH10 (Giảm 10% đơn từ 500k).";
            }

            MockDataService.Instance.ChatHistory.Add(new MockDataService.ChatMessage
            {
                Sender = "AI",
                Message = reply,
                Timestamp = System.DateTime.Now.AddSeconds(1)
            });

            return RedirectToAction("Index");
        }
    }
}
