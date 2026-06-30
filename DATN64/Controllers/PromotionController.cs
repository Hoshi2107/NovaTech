using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System;
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
        public IActionResult Index(string keyword, string status, int page = 1, int pageSize = 20)
        {
            if (page < 1)
            {
                page = 1;
            }

            var allowedPageSizes = new[] { 10, 20, 50 };

            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 20;
            }

            keyword = (keyword ?? "").Trim();
            status = (status ?? "").Trim();

            var now = DateTime.Now;

            var query = _context.Vouchers.AsQueryable();

            // Tìm kiếm theo mã voucher
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(v =>
                    v.MaCode != null &&
                    v.MaCode.Contains(keyword)
                );
            }

            // Lọc trạng thái voucher
            if (status == "Đang hiệu lực")
            {
                query = query.Where(v =>
                    (v.NgayBatDau == null || v.NgayBatDau <= now) &&
                    (v.NgayKetThuc == null || v.NgayKetThuc > now) &&
                    (v.SoLuong == null || v.SoLuong > 0)
                );
            }
            else if (status == "Hết hạn")
            {
                query = query.Where(v =>
                    v.NgayKetThuc != null &&
                    v.NgayKetThuc <= now
                );
            }
            else if (status == "Hết số lượng")
            {
                query = query.Where(v =>
                    v.SoLuong != null &&
                    v.SoLuong <= 0
                );
            }

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var vouchers = query
                .OrderByDescending(v => v.NgayBatDau)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Keyword = keyword;
            ViewBag.StatusFilter = status;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(vouchers);
        }

        [HttpPost]
        [HasPermission("View_Promotion")]
        public IActionResult Create(Voucher v)
        {
            if (v == null)
            {
                TempData["ToastMessage"] = "Dữ liệu voucher không hợp lệ!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(v.MaCode))
            {
                TempData["ToastMessage"] = "Vui lòng nhập mã voucher!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            var existedCode = _context.Vouchers
                .Any(x => x.MaCode == v.MaCode);

            if (existedCode)
            {
                TempData["ToastMessage"] = "Mã voucher này đã tồn tại!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            v.MaCode = v.MaCode.Trim().ToUpper();
            v.NgayBatDau = DateTime.Now;
            v.NgayKetThuc = DateTime.Now.AddDays(10);

            _context.Vouchers.Add(v);
            _context.SaveChanges();

            TempData["ToastMessage"] = "Thêm mã giảm giá khuyến mãi thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}