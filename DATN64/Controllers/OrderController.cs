using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_Order")]
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            var orders = MockDataService.Instance.Orders.OrderByDescending(o => o.OrderDate).ToList();
            return View(orders);
        }

        public IActionResult Detail(int id)
        {
            var order = MockDataService.Instance.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [HasPermission("Approve_Order")]
        public IActionResult UpdateStatus(int orderId, string status)
        {
            var order = MockDataService.Instance.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order != null)
            {
                order.Status = status;
                TempData["ToastMessage"] = $"Đã cập nhật trạng thái đơn hàng sang: {status}";
                TempData["ToastType"] = "success";
            }
            return RedirectToAction("Detail", new { id = orderId });
        }
    }
}
