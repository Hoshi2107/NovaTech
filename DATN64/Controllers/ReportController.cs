using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_Report")]
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            var orders = MockDataService.Instance.Orders.Where(o => o.Status == "Hoàn thành").ToList();
            
            ViewBag.RevenueCuaHang = orders.Where(o => o.Channel == "Cửa hàng").Sum(o => o.Total);
            ViewBag.RevenueWebsite = orders.Where(o => o.Channel == "Website").Sum(o => o.Total);
            ViewBag.RevenueTikTok = orders.Where(o => o.Channel == "TikTok Shop").Sum(o => o.Total);

            return View();
        }
    }
}
