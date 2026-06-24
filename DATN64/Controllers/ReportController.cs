using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_Report")]
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var orders = _context.DonHangs.Where(o => o.TrangThai == "Hoàn thành").ToList();
            
            ViewBag.RevenueCuaHang = orders.Sum(o => o.TongTien); // Simplified
            ViewBag.RevenueWebsite = orders.Sum(o => o.TongTien); // Simplified
            ViewBag.RevenueTikTok = 0;

            return View();
        }
    }
}
