using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using System.Linq;

namespace DATN64.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            // Summarizing statistics
            var orders = _context.DonHangs.ToList();
            var products = _context.SanPhams.ToList();
            var customers = _context.KhachHangs.ToList();

            ViewBag.TotalRevenue = orders.Where(o => o.TrangThai == "Hoàn thành").Sum(o => o.TongTien);
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalProducts = products.Count;
            ViewBag.TotalCustomers = customers.Count;

            ViewBag.PendingOrders = orders.Count(o => o.TrangThai == "Đơn mới" || o.TrangThai == "Đã xác nhận");
            ViewBag.CompletedOrders = orders.Count(o => o.TrangThai == "Hoàn thành");

            // Best sellers & low stock items
            ViewBag.TopProducts = products.OrderByDescending(p => p.SoLuongTon).Take(3).ToList(); // Mock logic for best sellers
            ViewBag.LowStockProducts = products.Where(p => p.SoLuongTon <= 3).ToList();

            // Recent system notification alerts
            ViewBag.RecentNotifications = _context.SystemNotifications.OrderByDescending(n => n.Timestamp).Take(3).ToList();

            // Top customers
            ViewBag.TopCustomers = customers.OrderByDescending(c => c.DiemTichLuy).Take(5).ToList();

            return View();
        }

        public IActionResult MarkAllRead()
        {
            var notifications = _context.SystemNotifications.Where(n => !n.IsRead).ToList();
            foreach (var n in notifications)
            {
                n.IsRead = true;
            }
            _context.SaveChanges();
            
            TempData["ToastMessage"] = "Đã đánh dấu đọc tất cả thông báo!";
            return RedirectToAction("Index");
        }
    }
}
