using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_TikTok")]
    public class TikTokController : Controller
    {
        public IActionResult Index()
        {
            var config = MockDataService.Instance.TikTokConfig;
            ViewBag.SyncLogs = MockDataService.Instance.TikTokSyncLogs.OrderByDescending(l => l.Timestamp).ToList();
            return View(config);
        }

        [HttpPost]
        [HasPermission("Sync_TikTok")]
        public IActionResult TriggerSync(string syncType)
        {
            // Simulating Sync operation
            var log = new MockDataService.TikTokSyncLog
            {
                Id = MockDataService.Instance.TikTokSyncLogs.Max(l => l.Id) + 1,
                Type = syncType,
                Message = $"Đồng bộ thành công dữ liệu {syncType} với TikTok API cửa hàng.",
                Status = "Thành công",
                Timestamp = System.DateTime.Now
            };

            MockDataService.Instance.TikTokSyncLogs.Add(log);
            MockDataService.Instance.TikTokConfig.LastSyncTime = System.DateTime.Now;

            TempData["ToastMessage"] = $"Yêu cầu đồng bộ {syncType} hoàn tất!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}
