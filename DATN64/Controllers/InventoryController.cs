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
            var products = _context.SanPhams.ToList();
            ViewBag.Transactions = _context.InventoryTransactions.OrderByDescending(t => t.Date).ToList();
            return View(products);
        }

        public IActionResult Import()
        {
            ViewBag.Products = _context.SanPhams.ToList();
            return View();
        }

        [HttpPost]
        [HasPermission("Import_Inventory")]
        public IActionResult SubmitImport(int productId, int quantity, string note)
        {
            var prod = _context.SanPhams.FirstOrDefault(p => p.MaSanPham == productId);
            if (prod != null && quantity > 0)
            {
                prod.SoLuongTon += quantity;
                
                var tx = new InventoryTransaction
                {
                    Code = "GRN-" + (_context.InventoryTransactions.Count() + 1).ToString("D3"),
                    Type = "Nhập kho",
                    ProductSKU = prod.MaSanPham.ToString(),
                    ProductName = prod.TenSanPham,
                    QuantityChange = quantity,
                    Creator = HttpContext.Session.GetString("UserName") ?? "Thủ kho",
                    Date = DateTime.Now,
                    Note = note ?? "Nhập bổ sung tồn kho"
                };

                _context.InventoryTransactions.Add(tx);
                _context.SaveChanges();

                TempData["ToastMessage"] = $"Nhập kho thành công {quantity} sản phẩm!";
                TempData["ToastType"] = "success";
            }
            return RedirectToAction("Index");
        }

        public IActionResult Export()
        {
            ViewBag.Products = _context.SanPhams.ToList();
            return View();
        }

        [HttpPost]
        [HasPermission("Export_Inventory")]
        public IActionResult SubmitExport(int productId, int quantity, string note)
        {
            var prod = _context.SanPhams.FirstOrDefault(p => p.MaSanPham == productId);
            if (prod != null && quantity > 0)
            {
                if (prod.SoLuongTon < quantity)
                {
                    TempData["ToastMessage"] = "Số lượng xuất kho vượt quá tồn kho khả dụng!";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Index");
                }

                prod.SoLuongTon -= quantity;

                var tx = new InventoryTransaction
                {
                    Code = "GIN-" + (_context.InventoryTransactions.Count() + 1).ToString("D3"),
                    Type = "Xuất kho",
                    ProductSKU = prod.MaSanPham.ToString(),
                    ProductName = prod.TenSanPham,
                    QuantityChange = -quantity,
                    Creator = HttpContext.Session.GetString("UserName") ?? "Thủ kho",
                    Date = DateTime.Now,
                    Note = note ?? "Xuất hủy hoặc chuyển hàng"
                };

                _context.InventoryTransactions.Add(tx);
                _context.SaveChanges();

                TempData["ToastMessage"] = $"Đã xuất kho {quantity} sản phẩm!";
                TempData["ToastType"] = "success";
            }
            return RedirectToAction("Index");
        }
    }
}
