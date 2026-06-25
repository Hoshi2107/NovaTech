using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using DATN64.Helpers;
using System;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_Inventory")]
    public class InventoryController : Controller
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Import()
        {
            return RedirectToAction("Index", new { tab = "import" });
        }

        public IActionResult Export()
        {
            return RedirectToAction("Index", new { tab = "export" });
        }
    }
}
