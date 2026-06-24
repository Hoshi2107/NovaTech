using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;

namespace DATN64.Controllers
{
    public class PromotionController : Controller
    {
        private readonly AppDbContext _context;

        public PromotionController(AppDbContext context)
        {
            _context = context;
        }

        [HasPermission("View_Promotion")]
        public IActionResult Index()
        {
            var vouchers = _context.Vouchers.ToList();
            return View(vouchers);
        }

        [HttpPost]
        [HasPermission("View_Promotion")]
        public IActionResult Create(Voucher v)
        {
            v.NgayBatDau = System.DateTime.Now;
            v.NgayKetThuc = System.DateTime.Now.AddDays(10);

            _context.Vouchers.Add(v);
            _context.SaveChanges();
            TempData["ToastMessage"] = "Thêm mã giảm giá khuyến mãi thành công!";
            return RedirectToAction("Index");
        }
    }
}
