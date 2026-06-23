using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using System.Linq;

namespace DATN64.Controllers
{
    public class POSController : Controller
    {
        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var rolesString = HttpContext.Session.GetString("UserRoles") ?? "";
            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!roles.Contains("Super Admin") && !roles.Contains("Quản lý cửa hàng") && !roles.Contains("Nhân viên bán hàng"))
            {
                return RedirectToAction("Selection", "Portal");
            }

            var products = MockDataService.Instance.Products.Where(p => p.Status == "Đang bán").ToList();
            var customers = MockDataService.Instance.Customers.ToList();
            
            ViewBag.Customers = customers;
            ViewBag.SellerName = HttpContext.Session.GetString("UserName") ?? "Nhân viên";

            return View(products);
        }

        [HttpPost]
        public IActionResult CreateOrderPOS(string customerName, string customerPhone, string paymentMethod, List<int> productIds, List<int> quantities)
        {
            if (productIds == null || productIds.Count == 0)
            {
                TempData["ToastMessage"] = "Vui lòng chọn ít nhất 1 sản phẩm!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index");
            }

            var newOrder = new MockDataService.Order
            {
                Id = MockDataService.Instance.Orders.Max(o => o.Id) + 1,
                OrderCode = "ORD-POS" + (MockDataService.Instance.Orders.Count + 1).ToString("D3"),
                CustomerName = string.IsNullOrEmpty(customerName) ? "Khách Hàng Vãng Lai" : customerName,
                CustomerPhone = customerPhone ?? "",
                CustomerAddress = "Bán tại quầy",
                OrderDate = System.DateTime.Now,
                Status = "Hoàn thành",
                Channel = "Cửa hàng",
                PaymentMethod = paymentMethod,
                Items = new List<MockDataService.OrderItem>()
            };

            for (int i = 0; i < productIds.Count; i++)
            {
                var prod = MockDataService.Instance.Products.FirstOrDefault(p => p.Id == productIds[i]);
                if (prod != null)
                {
                    int qty = quantities[i];
                    prod.Stock -= qty; // Cập nhật kho
                    
                    newOrder.Items.Add(new MockDataService.OrderItem
                    {
                        ProductId = prod.Id,
                        ProductName = prod.Name,
                        SKU = prod.SKU,
                        Image = prod.Image,
                        Quantity = qty,
                        Price = prod.Price
                    });
                }
            }

            MockDataService.Instance.Orders.Add(newOrder);

            // Gửi thông báo
            MockDataService.Instance.Notifications.Add(new MockDataService.SystemNotification
            {
                Id = MockDataService.Instance.Notifications.Max(n => n.Id) + 1,
                Title = "Đơn POS mới",
                Message = $"Đơn hàng {newOrder.OrderCode} trị giá {newOrder.Total:N0} đ vừa được thanh toán tại quầy.",
                Type = "Đơn mới",
                Timestamp = System.DateTime.Now
            });

            TempData["ToastMessage"] = $"Đã thanh toán đơn hàng {newOrder.OrderCode} thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}
