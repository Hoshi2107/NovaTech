using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System;
using System.Linq;

namespace DATN64.Controllers
{
    public class ProductController : Controller
    {
        // ----------------- PRODUCTS -----------------
        [HasPermission("View_Product")]
        public IActionResult Index(string search, string category, string brand)
        {
            var products = MockDataService.Instance.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || p.SKU.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }
            if (!string.IsNullOrEmpty(brand))
            {
                products = products.Where(p => p.Brand == brand);
            }

            ViewBag.Categories = MockDataService.Instance.Categories.ToList();
            ViewBag.Brands = MockDataService.Instance.Brands.ToList();
            ViewBag.Suppliers = MockDataService.Instance.Suppliers.ToList();

            return View(products.ToList());
        }

        [HttpPost]
        [HasPermission("Create_Product")]
        public IActionResult Create(MockDataService.Product p)
        {
            p.Id = MockDataService.Instance.Products.Max(prod => prod.Id) + 1;
            p.Image = string.IsNullOrEmpty(p.Image) ? "https://images.unsplash.com/photo-1531297484001-80022131f5a1?q=80&w=300" : p.Image;
            p.Images = new List<string> { p.Image };
            MockDataService.Instance.Products.Add(p);

            TempData["ToastMessage"] = "Thêm sản phẩm thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Edit_Product")]
        public IActionResult Edit(MockDataService.Product p)
        {
            var target = MockDataService.Instance.Products.FirstOrDefault(prod => prod.Id == p.Id);
            if (target != null)
            {
                target.Name = p.Name;
                target.SKU = p.SKU;
                target.Barcode = p.Barcode;
                target.Brand = p.Brand;
                target.Category = p.Category;
                target.Supplier = p.Supplier;
                target.ImportPrice = p.ImportPrice;
                target.Price = p.Price;
                target.Stock = p.Stock;
                target.Specifications = p.Specifications;
                target.Status = p.Status;
                if (!string.IsNullOrEmpty(p.Image))
                {
                    target.Image = p.Image;
                }
            }

            TempData["ToastMessage"] = "Cập nhật sản phẩm thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Delete_Product")]
        public IActionResult Delete(int id)
        {
            var target = MockDataService.Instance.Products.FirstOrDefault(p => p.Id == id);
            if (target != null)
            {
                MockDataService.Instance.Products.Remove(target);
            }

            TempData["ToastMessage"] = "Đã xóa sản phẩm khỏi hệ thống!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        // ----------------- CATEGORIES -----------------
        [HasPermission("View_Product")]
        public IActionResult Categories()
        {
            return View(MockDataService.Instance.Categories.ToList());
        }

        [HttpPost]
        [HasPermission("Create_Product")]
        public IActionResult CreateCategory(MockDataService.Category c)
        {
            c.Id = MockDataService.Instance.Categories.Max(cat => cat.Id) + 1;
            MockDataService.Instance.Categories.Add(c);
            TempData["ToastMessage"] = "Thêm danh mục mới thành công!";
            return RedirectToAction("Categories");
        }

        // ----------------- BRANDS -----------------
        [HasPermission("View_Product")]
        public IActionResult Brands()
        {
            return View(MockDataService.Instance.Brands.ToList());
        }

        [HttpPost]
        [HasPermission("Create_Product")]
        public IActionResult CreateBrand(MockDataService.Brand b)
        {
            b.Id = MockDataService.Instance.Brands.Max(br => br.Id) + 1;
            MockDataService.Instance.Brands.Add(b);
            TempData["ToastMessage"] = "Thêm thương hiệu mới thành công!";
            return RedirectToAction("Brands");
        }

        // ----------------- SUPPLIERS -----------------
        [HasPermission("View_Product")]
        public IActionResult Suppliers()
        {
            return View(MockDataService.Instance.Suppliers.ToList());
        }

        [HttpPost]
        [HasPermission("Create_Product")]
        public IActionResult CreateSupplier(MockDataService.Supplier s)
        {
            s.Id = MockDataService.Instance.Suppliers.Max(sup => sup.Id) + 1;
            MockDataService.Instance.Suppliers.Add(s);
            TempData["ToastMessage"] = "Thêm nhà cung cấp thành công!";
            return RedirectToAction("Suppliers");
        }

        [HttpPost]
        [HasPermission("Edit_Product")]
        public IActionResult UpdateDiscount(int id, decimal discountPrice, int hours)
        {
            var target = MockDataService.Instance.Products.FirstOrDefault(prod => prod.Id == id);
            if (target != null)
            {
                if (discountPrice > 0 && hours > 0)
                {
                    if (target.OriginalPrice == 0)
                    {
                        target.OriginalPrice = target.Price;
                    }
                    target.Price = discountPrice;
                    target.DiscountExpiry = DateTime.Now.AddHours(hours);
                    target.DiscountRate = (int)Math.Round((1 - (discountPrice / target.OriginalPrice)) * 100);
                }
                else
                {
                    // Remove discount
                    if (target.OriginalPrice > 0)
                    {
                        target.Price = target.OriginalPrice;
                        target.OriginalPrice = 0;
                    }
                    target.DiscountExpiry = null;
                    target.DiscountRate = 0;
                }
            }

            TempData["ToastMessage"] = "Cập nhật giảm giá sản phẩm thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }
    }
}
