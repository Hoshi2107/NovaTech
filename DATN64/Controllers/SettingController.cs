using Microsoft.AspNetCore.Mvc;
using DATN64.Helpers;

namespace DATN64.Controllers
{
    [HasPermission("View_Setting")]
    public class SettingController : Controller
    {
        // Simple static config for Settings since no DB model was asked for this
        private static string _shopName = "Siêu thị NovaTech";
        private static string _shopAddress = "123 Đường Điện Biên Phủ, TP.HCM";
        private static string _shopEmail = "contact@novatech.vn";
        private static string _shopHotline = "1900 1000";
        private static string _systemConfig = "Bật bảo mật 2 lớp; Gửi email khi có đơn hàng mới;";

        public IActionResult Index()
        {
            ViewBag.ShopName = _shopName;
            ViewBag.ShopAddress = _shopAddress;
            ViewBag.ShopEmail = _shopEmail;
            ViewBag.ShopHotline = _shopHotline;
            ViewBag.SystemConfig = _systemConfig;

            return View();
        }

        [HttpPost]
        [HasPermission("Edit_Setting")]
        public IActionResult Save(string shopName, string shopAddress, string shopEmail, string shopHotline, string systemConfig)
        {
            _shopName = shopName;
            _shopAddress = shopAddress;
            _shopEmail = shopEmail;
            _shopHotline = shopHotline;
            _systemConfig = systemConfig;

            TempData["ToastMessage"] = "Lưu cấu hình hệ thống thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}
