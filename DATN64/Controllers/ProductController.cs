using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DATN64.Models;
using DATN64.Helpers;
using System;
using System.Linq;

namespace DATN64.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // ----------------- PRODUCTS -----------------
        [HasPermission("View_Product")]
        public IActionResult Index(string search, int? category, int? brand)
        {
            var products = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .Include(p => p.NhaCungCap)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.TenSanPham.Contains(search) || p.MaSanPham.ToString().Contains(search));
            }
            if (category.HasValue)
            {
                products = products.Where(p => p.MaDanhMuc == category.Value);
            }
            if (brand.HasValue)
            {
                products = products.Where(p => p.MaThuongHieu == brand.Value);
            }

            ViewBag.Categories = _context.DanhMucs.ToList();
            ViewBag.Brands = _context.ThuongHieus.ToList();
            ViewBag.Suppliers = _context.NhaCungCaps.ToList();

            return View(products.ToList());
        }

        [HttpPost]
        [HasPermission("Create_Product")]
        public IActionResult Create(SanPham p)
        {
            if (string.IsNullOrEmpty(p.HinhAnh))
            {
                p.HinhAnh = "https://images.unsplash.com/photo-1531297484001-80022131f5a1?q=80&w=300";
            }
            _context.SanPhams.Add(p);
            _context.SaveChanges();

            TempData["ToastMessage"] = "Thêm sản phẩm thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Edit_Product")]
        public IActionResult Edit(SanPham p)
        {
            var target = _context.SanPhams.FirstOrDefault(prod => prod.MaSanPham == p.MaSanPham);
            if (target != null)
            {
                target.TenSanPham = p.TenSanPham;
                target.MaDanhMuc = p.MaDanhMuc;
                target.MaThuongHieu = p.MaThuongHieu;
                target.MaNCC = p.MaNCC;
                target.GiaNhap = p.GiaNhap;
                target.GiaBan = p.GiaBan;
                target.SoLuongTon = p.SoLuongTon;
                target.MoTa = p.MoTa;
                target.TrangThai = p.TrangThai;
                if (!string.IsNullOrEmpty(p.HinhAnh))
                {
                    target.HinhAnh = p.HinhAnh;
                }
                _context.SaveChanges();
            }

            TempData["ToastMessage"] = "Cập nhật sản phẩm thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Delete_Product")]
        public IActionResult Delete(int id)
        {
            var target = _context.SanPhams.FirstOrDefault(p => p.MaSanPham == id);
            if (target != null)
            {
                _context.SanPhams.Remove(target);
                _context.SaveChanges();
            }

            TempData["ToastMessage"] = "Đã xóa sản phẩm khỏi hệ thống!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        // ----------------- CATEGORIES -----------------
        [HasPermission("View_Product")]
        public IActionResult Categories()
        {
            return View(_context.DanhMucs.ToList());
        }

        [HttpPost]
        [HasPermission("Create_Product")]
        public IActionResult CreateCategory(DanhMuc c)
        {
            _context.DanhMucs.Add(c);
            _context.SaveChanges();
            TempData["ToastMessage"] = "Thêm danh mục mới thành công!";
            return RedirectToAction("Categories");
        }

        // ----------------- BRANDS -----------------
        [HasPermission("View_Product")]
        public IActionResult Brands()
        {
            return View(_context.ThuongHieus.ToList());
        }

        [HttpPost]
        [HasPermission("Create_Product")]
        public IActionResult CreateBrand(ThuongHieu b)
        {
            _context.ThuongHieus.Add(b);
            _context.SaveChanges();
            TempData["ToastMessage"] = "Thêm thương hiệu mới thành công!";
            return RedirectToAction("Brands");
        }

        // ----------------- SUPPLIERS -----------------
        [HasPermission("View_Product")]
        public IActionResult Suppliers()
        {
            return View(_context.NhaCungCaps.ToList());
        }

        [HttpPost]
        [HasPermission("Create_Product")]
        public IActionResult CreateSupplier(NhaCungCap s)
        {
            _context.NhaCungCaps.Add(s);
            _context.SaveChanges();
            TempData["ToastMessage"] = "Thêm nhà cung cấp thành công!";
            return RedirectToAction("Suppliers");
        }

        [HttpPost]
        [HasPermission("Edit_Product")]
        public IActionResult UpdateDiscount(int id, decimal discountPrice, int hours)
        {
            // Simplified since SanPham might not have OriginalPrice or DiscountExpiry
            var target = _context.SanPhams.FirstOrDefault(prod => prod.MaSanPham == id);
            if (target != null)
            {
                if (discountPrice > 0 && hours > 0)
                {
                    target.GiaBan = discountPrice;
                    // Note: Flash sale mechanism should be handled differently, 
                    // this just updates the price directly for now.
                }
                _context.SaveChanges();
            }

            TempData["ToastMessage"] = "Cập nhật giảm giá sản phẩm thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }
    }
}
