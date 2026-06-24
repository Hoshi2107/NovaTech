using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_Order")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var orders = _context.DonHangs
                .Include(o => o.KhachHang)
                .OrderByDescending(o => o.NgayDat)
                .ToList();
            return View(orders);
        }

        public IActionResult Detail(int id)
        {
            var order = _context.DonHangs
                .Include(o => o.KhachHang)
                .Include(o => o.ChiTietDonHangs)
                    .ThenInclude(c => c.SanPham)
                .FirstOrDefault(o => o.MaDonHang == id);
            
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [HasPermission("Approve_Order")]
        public IActionResult UpdateStatus(int orderId, string status)
        {
            var order = _context.DonHangs.FirstOrDefault(o => o.MaDonHang == orderId);
            if (order != null)
            {
                order.TrangThai = status;
                _context.SaveChanges();
                TempData["ToastMessage"] = $"Đã cập nhật trạng thái đơn hàng sang: {status}";
                TempData["ToastType"] = "success";
            }
            return RedirectToAction("Detail", new { id = orderId });
        }
    }
}
