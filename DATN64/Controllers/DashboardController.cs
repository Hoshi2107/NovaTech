using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using System.Linq;

namespace DATN64.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            // Summarizing statistics
            var orders = MockDataService.Instance.Orders;
            var products = MockDataService.Instance.Products;
            var customers = MockDataService.Instance.Customers;

            ViewBag.TotalRevenue = orders.Where(o => o.Status == "Hoàn thành").Sum(o => o.Total);
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalProducts = products.Count;
            ViewBag.TotalCustomers = customers.Count;

            ViewBag.PendingOrders = orders.Count(o => o.Status == "Đơn mới" || o.Status == "Đã xác nhận");
            ViewBag.CompletedOrders = orders.Count(o => o.Status == "Hoàn thành");

            // Best sellers & low stock items
            ViewBag.TopProducts = products.OrderByDescending(p => p.Stock).Take(3).ToList(); // Mock logic
            ViewBag.LowStockProducts = products.Where(p => p.Stock <= 3).ToList();

            // Recent system notification alerts
            ViewBag.RecentNotifications = MockDataService.Instance.Notifications.OrderByDescending(n => n.Timestamp).Take(3).ToList();

            return View();
        }

        public IActionResult MarkAllRead()
        {
            foreach (var n in MockDataService.Instance.Notifications)
            {
                n.IsRead = true;
            }
            TempData["ToastMessage"] = "Đã đánh dấu đọc tất cả thông báo!";
            return RedirectToAction("Index");
        }
    }
}
