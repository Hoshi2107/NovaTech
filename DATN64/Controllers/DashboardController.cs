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

            var rolesString = HttpContext.Session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            var canAccessERP = roles.Any(r => r == "Super Admin" || r == "Admin" || r == "Quản lý cửa hàng" || r == "Nhân viên kho" || r == "Quản lý kho" || r == "Kế toán" || r == "Marketing");

            if (!canAccessERP)
            {
                if (roles.Contains("Khách hàng"))
                {
                    return RedirectToAction("Index", "Online");
                }
                return RedirectToAction("Selection", "Portal");
            }

            // Summarizing statistics
            var orders = _context.DonHangs.ToList();
            var products = _context.SanPhams.ToList();
            var customers = _context.KhachHangs.ToList();

            ViewBag.TotalRevenue = orders.Where(o => o.TrangThai == "Hoàn thành").Sum(o => o.TongTien ?? 0);
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalProducts = products.Count;
            ViewBag.TotalCustomers = customers.Count;

            ViewBag.PendingOrders = orders.Count(o => o.TrangThai == "Đơn mới" || o.TrangThai == "Đã xác nhận");
            ViewBag.CompletedOrders = orders.Count(o => o.TrangThai == "Hoàn thành");

            // 1. Calculate today's revenue
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1).AddTicks(-1);
            ViewBag.TodayRevenue = orders
                .Where(o => o.TrangThai == "Hoàn thành" && o.NgayDat >= todayStart && o.NgayDat <= todayEnd)
                .Sum(o => o.TongTien ?? 0);

            // 2. Average orders per customer
            double avgOrders = customers.Count > 0 ? (double)orders.Count / customers.Count : 0;
            ViewBag.AvgOrdersPerCustomer = avgOrders;

            // 3. Real logic for best sellers (TopProducts)
            var topProductsData = _context.ChiTietDonHangs
                .GroupBy(ct => ct.MaSanPham)
                .Select(g => new
                {
                    MaSanPham = g.Key,
                    OrderCount = g.Count(),
                    QuantitySold = g.Sum(ct => ct.SoLuong)
                })
                .OrderByDescending(g => g.QuantitySold)
                .ToList();

            var topProductsWithSales = new List<TopProductViewModel>();
            var addedProductIds = new HashSet<int>();

            foreach (var item in topProductsData)
            {
                var p = products.FirstOrDefault(prod => prod.MaSanPham == item.MaSanPham);
                if (p != null)
                {
                    topProductsWithSales.Add(new TopProductViewModel
                    {
                        SanPham = p,
                        OrderCount = item.OrderCount,
                        QuantitySold = item.QuantitySold
                    });
                    addedProductIds.Add(p.MaSanPham);
                }
            }

            if (topProductsWithSales.Count < 5)
            {
                var remainingProducts = products
                    .Where(p => !addedProductIds.Contains(p.MaSanPham))
                    .Take(5 - topProductsWithSales.Count)
                    .ToList();

                foreach (var p in remainingProducts)
                {
                    topProductsWithSales.Add(new TopProductViewModel
                    {
                        SanPham = p,
                        OrderCount = 0,
                        QuantitySold = 0
                    });
                }
            }

            ViewBag.TopProducts = topProductsWithSales.Take(5).ToList();

            // 4. Low stock items
            ViewBag.LowStockProducts = products.Where(p => p.SoLuongTon <= 3).ToList();

            // Recent system notification alerts
            ViewBag.RecentNotifications = _context.SystemNotifications.OrderByDescending(n => n.Timestamp).Take(3).ToList();

            // 5. Real logic for top customers
            var topCustomersData = orders
                .Where(o => o.MaKhachHang != null)
                .GroupBy(o => o.MaKhachHang)
                .Select(g => new
                {
                    MaKhachHang = g.Key!.Value,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TongTien ?? 0)
                })
                .OrderByDescending(g => g.OrderCount)
                .ToList();

            var topCustomersWithSales = new List<TopCustomerViewModel>();
            var addedCustomerIds = new HashSet<int>();

            foreach (var item in topCustomersData)
            {
                var c = customers.FirstOrDefault(cust => cust.MaKhachHang == item.MaKhachHang);
                if (c != null)
                {
                    topCustomersWithSales.Add(new TopCustomerViewModel
                    {
                        KhachHang = c,
                        OrderCount = item.OrderCount,
                        TotalSpent = item.TotalSpent
                    });
                    addedCustomerIds.Add(c.MaKhachHang);
                }
            }

            if (topCustomersWithSales.Count < 5)
            {
                var remainingCustomers = customers
                    .Where(c => !addedCustomerIds.Contains(c.MaKhachHang))
                    .Take(5 - topCustomersWithSales.Count)
                    .ToList();

                foreach (var c in remainingCustomers)
                {
                    topCustomersWithSales.Add(new TopCustomerViewModel
                    {
                        KhachHang = c,
                        OrderCount = 0,
                        TotalSpent = 0
                    });
                }
            }

            ViewBag.TopCustomers = topCustomersWithSales.Take(5).ToList();

            // 6. Last 7 days chart data
            var chartLabels = new List<string>();
            var chartRevenue = new List<double>();
            var chartOrderCounts = new List<int>();

            for (int i = 6; i >= 0; i--)
            {
                var date = todayStart.AddDays(-i);
                string dayLabel = date.ToString("dd/MM");
                string dayOfWeek = date.DayOfWeek switch
                {
                    DayOfWeek.Monday => "T2",
                    DayOfWeek.Tuesday => "T3",
                    DayOfWeek.Wednesday => "T4",
                    DayOfWeek.Thursday => "T5",
                    DayOfWeek.Friday => "T6",
                    DayOfWeek.Saturday => "T7",
                    DayOfWeek.Sunday => "CN",
                    _ => ""
                };
                chartLabels.Add($"{dayOfWeek} ({dayLabel})");

                var ordersOnDay = orders.Where(o => o.NgayDat.HasValue && o.NgayDat.Value.Date == date).ToList();
                double revenueOnDay = (double)ordersOnDay.Where(o => o.TrangThai == "Hoàn thành").Sum(o => o.TongTien ?? 0);
                chartRevenue.Add(revenueOnDay);
                chartOrderCounts.Add(ordersOnDay.Count);
            }

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartRevenue = chartRevenue;
            ViewBag.ChartOrderCounts = chartOrderCounts;

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
