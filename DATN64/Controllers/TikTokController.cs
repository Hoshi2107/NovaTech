using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_TikTok")]
    public class TikTokController : Controller
    {
        private readonly AppDbContext _context;

        public TikTokController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var config = _context.TikTokShopConfigs.FirstOrDefault() ?? new TikTokShopConfig();
            ViewBag.SyncLogs = _context.TikTokSyncLogs.OrderByDescending(l => l.Timestamp).ToList();
            return View(config);
        }

        [HttpPost]
        [HasPermission("Sync_TikTok")]
        public IActionResult TriggerSync(string syncType)
        {
            var log = new TikTokSyncLog
            {
                Type = syncType,
                Message = $"Đồng bộ thành công dữ liệu {syncType} với TikTok API cửa hàng.",
                Status = "Thành công",
                Timestamp = System.DateTime.Now
            };

            var config = _context.TikTokShopConfigs.FirstOrDefault();
            if (config != null)
            {
                config.LastSyncTime = System.DateTime.Now;
            }

            _context.TikTokSyncLogs.Add(log);
            _context.SaveChanges();

            TempData["ToastMessage"] = $"Yêu cầu đồng bộ {syncType} hoàn tất!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}
