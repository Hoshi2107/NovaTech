using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace DATN64.Controllers
{
    public class OnlineController : Controller
    {
        public class CartItem
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = "";
            public string Image { get; set; } = "";
            public decimal Price { get; set; }
            public decimal OriginalPrice { get; set; }
            public int Quantity { get; set; }
            public bool IsDiscounted { get; set; }
            public decimal Total => Price * Quantity;
        }

        public IActionResult Index()
        {
            var products = MockDataService.Instance.Products.Where(p => p.Status == "Đang bán").ToList();
            return View(products);
        }

        public IActionResult ProductsList()
        {
            var products = MockDataService.Instance.Products.Where(p => p.Status == "Đang bán").ToList();
            return View(products);
        }

        public IActionResult Detail(int id)
        {
            var p = MockDataService.Instance.Products.FirstOrDefault(prod => prod.Id == id);
            if (p == null) return NotFound();
            return View(p);
        }

        // ----------------- CART MANAGEMENT -----------------
        private List<CartItem> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson)) return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        public IActionResult Cart()
        {
            var cart = GetCartFromSession();
            ViewBag.Vouchers = MockDataService.Instance.Vouchers.Where(v => v.Status == "Đang diễn ra" && v.EndDate > DateTime.Now).ToList();
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int id, int qty = 1)
        {
            var p = MockDataService.Instance.Products.FirstOrDefault(prod => prod.Id == id);
            if (p == null) return NotFound();

            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.ProductId == id);

            bool isDiscounted = p.DiscountExpiry.HasValue && p.DiscountExpiry.Value > DateTime.Now;

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Image = p.Image,
                    Price = p.Price,
                    OriginalPrice = isDiscounted ? p.OriginalPrice : p.Price,
                    Quantity = qty,
                    IsDiscounted = isDiscounted
                });
            }
            else
            {
                item.Quantity += qty;
            }

            SaveCartToSession(cart);

            return Json(new { success = true, cartCount = cart.Sum(i => i.Quantity) });
        }

        [HttpPost]
        public IActionResult UpdateCart(int id, int qty)
        {
            if (qty <= 0) return RemoveFromCart(id);

            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                item.Quantity = qty;
                SaveCartToSession(cart);
            }

            return Json(new { success = true, cartCount = cart.Sum(i => i.Quantity), itemTotal = item?.Total.ToString("N0") });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartToSession(cart);
            }

            return RedirectToAction("Cart");
        }

        [HttpPost]
        public IActionResult ApplyVoucher(string code, decimal currentSubtotal)
        {
            var voucher = MockDataService.Instance.Vouchers.FirstOrDefault(v => 
                v.Code.Equals(code, StringComparison.OrdinalIgnoreCase) && 
                v.Status == "Đang diễn ra" && 
                v.EndDate > DateTime.Now && 
                v.StartDate <= DateTime.Now);

            if (voucher == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn!" });
            }

            var cart = GetCartFromSession();
            decimal subtotal = cart.Sum(i => i.Total);

            if (subtotal < voucher.MinOrderValue)
            {
                return Json(new { success = false, message = $"Đơn hàng tối thiểu phải đạt {voucher.MinOrderValue.ToString("N0")} đ để dùng mã này!" });
            }

            // Rule: "mã giảm thì sẽ không áp dụng cho sản phẩm đang được giảm giá"
            // Filter products in cart that are not discounted (regular priced)
            decimal eligibleSubtotal = 0;
            foreach (var item in cart)
            {
                var prod = MockDataService.Instance.Products.FirstOrDefault(p => p.Id == item.ProductId);
                bool isCurrentlyDiscounted = prod != null && prod.DiscountExpiry.HasValue && prod.DiscountExpiry.Value > DateTime.Now;
                
                if (!isCurrentlyDiscounted)
                {
                    eligibleSubtotal += item.Total;
                }
            }

            if (eligibleSubtotal == 0)
            {
                return Json(new { 
                    success = true, 
                    discount = 0, 
                    message = "Mã hợp lệ, nhưng số tiền giảm là 0 đ vì tất cả sản phẩm trong giỏ hàng đều đang được giảm giá Flash Sale!" 
                });
            }

            decimal discount = 0;
            if (voucher.Type == "Giảm %")
            {
                discount = eligibleSubtotal * (voucher.Value / 100);
            }
            else if (voucher.Type == "Giảm tiền")
            {
                discount = Math.Min(voucher.Value, eligibleSubtotal);
            }

            return Json(new { 
                success = true, 
                discount = discount, 
                message = $"Áp dụng mã thành công! Đã giảm giá trên các sản phẩm không khuyến mãi (Tổng tiền xét giảm: {eligibleSubtotal.ToString("N0")} đ)" 
            });
        }

        [HttpPost]
        public IActionResult Checkout(string customerName, string customerPhone, string customerAddress, string paymentMethod, string voucherCode, decimal discountVal)
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0)
            {
                TempData["ToastMessage"] = "Giỏ hàng rỗng!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Cart");
            }

            // Create Order
            var order = new MockDataService.Order
            {
                Id = MockDataService.Instance.Orders.Max(o => o.Id) + 1,
                OrderCode = "ORD-WEB-" + new Random().Next(1000, 9999),
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerAddress = customerAddress,
                OrderDate = DateTime.Now,
                Status = "Đơn mới",
                Channel = "Website",
                PaymentMethod = paymentMethod,
                Discount = discountVal,
                Items = cart.Select(i => new MockDataService.OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.Name,
                    Image = i.Image,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            MockDataService.Instance.Orders.Add(order);

            // Add Notification
            MockDataService.Instance.Notifications.Add(new MockDataService.SystemNotification
            {
                Id = MockDataService.Instance.Notifications.Max(n => n.Id) + 1,
                Title = "Đơn hàng Website mới",
                Message = $"Khách hàng {customerName} vừa đặt đơn hàng {order.OrderCode} giá trị {order.Total.ToString("N0")} đ.",
                Type = "Đơn mới",
                Timestamp = DateTime.Now,
                IsRead = false
            });

            // Clear Cart
            HttpContext.Session.Remove("Cart");

            TempData["ToastMessage"] = $"Đặt hàng thành công! Mã đơn hàng của bạn: {order.OrderCode}";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}
