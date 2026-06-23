using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_Inventory")]
    public class InventoryController : Controller
    {
        public IActionResult Index()
        {
            var products = MockDataService.Instance.Products.ToList();
            ViewBag.Transactions = MockDataService.Instance.InventoryTransactions.OrderByDescending(t => t.Date).ToList();
            return View(products);
        }

        public IActionResult Import()
        {
            ViewBag.Products = MockDataService.Instance.Products.ToList();
            return View();
        }

        [HttpPost]
        [HasPermission("Import_Inventory")]
        public IActionResult SubmitImport(int productId, int quantity, string note)
        {
            var prod = MockDataService.Instance.Products.FirstOrDefault(p => p.Id == productId);
            if (prod != null && quantity > 0)
            {
                prod.Stock += quantity;
                
                var tx = new MockDataService.InventoryTransaction
                {
                    Id = MockDataService.Instance.InventoryTransactions.Max(t => t.Id) + 1,
                    Code = "GRN-" + (MockDataService.Instance.InventoryTransactions.Count + 1).ToString("D3"),
                    Type = "Nhập kho",
                    ProductSKU = prod.SKU,
                    ProductName = prod.Name,
                    QuantityChange = quantity,
                    Creator = HttpContext.Session.GetString("UserName") ?? "Thủ kho",
                    Date = DateTime.Now,
                    Note = note ?? "Nhập bổ sung tồn kho"
                };

                MockDataService.Instance.InventoryTransactions.Add(tx);
                TempData["ToastMessage"] = $"Nhập kho thành công {quantity} sản phẩm!";
                TempData["ToastType"] = "success";
            }
            return RedirectToAction("Index");
        }

        public IActionResult Export()
        {
            ViewBag.Products = MockDataService.Instance.Products.ToList();
            return View();
        }

        [HttpPost]
        [HasPermission("Export_Inventory")]
        public IActionResult SubmitExport(int productId, int quantity, string note)
        {
            var prod = MockDataService.Instance.Products.FirstOrDefault(p => p.Id == productId);
            if (prod != null && quantity > 0)
            {
                if (prod.Stock < quantity)
                {
                    TempData["ToastMessage"] = "Số lượng xuất kho vượt quá tồn kho khả dụng!";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Index");
                }

                prod.Stock -= quantity;

                var tx = new MockDataService.InventoryTransaction
                {
                    Id = MockDataService.Instance.InventoryTransactions.Max(t => t.Id) + 1,
                    Code = "GIN-" + (MockDataService.Instance.InventoryTransactions.Count + 1).ToString("D3"),
                    Type = "Xuất kho",
                    ProductSKU = prod.SKU,
                    ProductName = prod.Name,
                    QuantityChange = -quantity,
                    Creator = HttpContext.Session.GetString("UserName") ?? "Thủ kho",
                    Date = DateTime.Now,
                    Note = note ?? "Xuất hủy hoặc chuyển hàng"
                };

                MockDataService.Instance.InventoryTransactions.Add(tx);
                TempData["ToastMessage"] = $"Đã xuất kho {quantity} sản phẩm!";
                TempData["ToastType"] = "success";
            }
            return RedirectToAction("Index");
        }
    }
}
