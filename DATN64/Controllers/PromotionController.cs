using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;

namespace DATN64.Controllers
{
    public class PromotionController : Controller
    {
        [HasPermission("View_Promotion")]
        public IActionResult Index()
        {
            var vouchers = MockDataService.Instance.Vouchers.ToList();
            return View(vouchers);
        }

        [HttpPost]
        [HasPermission("View_Promotion")]
        public IActionResult Create(MockDataService.Voucher v)
        {
            v.Id = MockDataService.Instance.Vouchers.Max(voc => voc.Id) + 1;
            v.StartDate = System.DateTime.Now;
            v.EndDate = System.DateTime.Now.AddDays(10);
            v.Status = "Đang diễn ra";

            MockDataService.Instance.Vouchers.Add(v);
            TempData["ToastMessage"] = "Thêm mã giảm giá khuyến mãi thành công!";
            return RedirectToAction("Index");
        }
    }
}
