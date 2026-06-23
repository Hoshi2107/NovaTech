using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;

namespace DATN64.Controllers
{
    [HasPermission("View_Setting")]
    public class SettingController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.ShopName = MockDataService.Instance.ShopName;
            ViewBag.ShopAddress = MockDataService.Instance.ShopAddress;
            ViewBag.ShopEmail = MockDataService.Instance.ShopEmail;
            ViewBag.ShopHotline = MockDataService.Instance.ShopHotline;
            ViewBag.SystemConfig = MockDataService.Instance.SystemConfig;

            return View();
        }

        [HttpPost]
        [HasPermission("Edit_Setting")]
        public IActionResult Save(string shopName, string shopAddress, string shopEmail, string shopHotline, string systemConfig)
        {
            MockDataService.Instance.ShopName = shopName;
            MockDataService.Instance.ShopAddress = shopAddress;
            MockDataService.Instance.ShopEmail = shopEmail;
            MockDataService.Instance.ShopHotline = shopHotline;
            MockDataService.Instance.SystemConfig = systemConfig;

            TempData["ToastMessage"] = "Lưu cấu hình hệ thống thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}
