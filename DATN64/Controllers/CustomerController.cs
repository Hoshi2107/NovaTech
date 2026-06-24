using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;
using System;

namespace DATN64.Controllers
{
    [HasPermission("View_Customer")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var customers = _context.KhachHangs.ToList();
            return View(customers);
        }

        [HttpPost]
        [HasPermission("Create_Customer")]
        public IActionResult Create(KhachHang c)
        {
            c.DiemTichLuy = 10;
            
            _context.KhachHangs.Add(c);
            _context.SaveChanges();
            TempData["ToastMessage"] = "Thêm khách hàng thành công!";
            return RedirectToAction("Index");
        }
    }
}
