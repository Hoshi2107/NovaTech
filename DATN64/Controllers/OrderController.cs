using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DATN64.ViewModels;
using DATN64.Models;
using DATN64.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_Order")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var orders = _context.DonHangs
                .Include(o => o.KhachHang)
                .OrderByDescending(o => o.NgayDat)
                .ToList();

            return View(orders);
        }

        public IActionResult Detail(int id)
        {
            var order = _context.DonHangs
                .Include(d => d.KhachHang)
                .Include(d => d.NhanVien)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefault(d => d.MaDonHang == id);

            if (order == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy đơn hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            var vm = new OrderDetailViewModel
            {
                MaDonHang = order.MaDonHang,
                NgayDat = order.NgayDat ?? DateTime.Now,
                TongTien = order.TongTien ?? 0,
                TrangThai = order.TrangThai ?? "Đơn mới",
                PhuongThucThanhToan = order.PhuongThucThanhToan ?? "Chưa cập nhật",
                KenhBan = GetOrderChannel(order),

                TenKhachHang = order.KhachHang?.HoTen ?? "Khách vãng lai",
                SoDienThoaiKhachHang = order.KhachHang?.SoDienThoai,
                EmailKhachHang = order.KhachHang?.Email,
                DiaChiKhachHang = order.KhachHang?.DiaChi,
                TenNhanVien = order.NhanVien?.HoTen,

                Items = (order.ChiTietDonHangs ?? new List<ChiTietDonHang>())
                    .Select(ct => new OrderDetailItemViewModel
                    {
                        MaSanPham = ct.MaSanPham,
                        TenSanPham = ct.SanPham != null ? ct.SanPham.TenSanPham : "Sản phẩm",
                        HinhAnh = ct.SanPham != null ? ct.SanPham.HinhAnh : null,
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, string trangThai)
        {
            var order = _context.DonHangs
                .FirstOrDefault(d => d.MaDonHang == id);

            if (order == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy đơn hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(trangThai))
            {
                TempData["ToastMessage"] = "Vui lòng chọn trạng thái.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var oldStatus = order.TrangThai ?? "";
            var newStatus = trangThai.Trim();

            if (newStatus == "Đã hủy" && oldStatus != "Đã hủy")
            {
                var orderDetails = _context.ChiTietDonHangs
                    .Where(ct => ct.MaDonHang == id)
                    .ToList();

                foreach (var detail in orderDetails)
                {
                    var product = _context.SanPhams
                        .FirstOrDefault(p => p.MaSanPham == detail.MaSanPham);

                    if (product != null)
                    {
                        product.SoLuongTon += detail.SoLuong;
                    }
                }

                TempData["ToastMessage"] = "Đã hủy đơn hàng. Hàng đã được hoàn về kho.";
                TempData["ToastType"] = "warning";
            }
            else
            {
                TempData["ToastMessage"] = "Cập nhật trạng thái đơn hàng thành công.";
                TempData["ToastType"] = "success";
            }

            order.TrangThai = newStatus;
            _context.SaveChanges();

            return RedirectToAction(nameof(Detail), new { id });
        }

        private string GetOrderChannel(DonHang order)
        {
            var note = order.GhiChu ?? "";
            var payment = order.PhuongThucThanhToan ?? "";

            if (
                note.Contains("Địa chỉ giao hàng", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("Voucher sử dụng", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("giao hàng", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("website", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("online", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("online", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("website", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("TikTok", StringComparison.OrdinalIgnoreCase)
            )
            {
                return "Website";
            }

            if (
                note.Contains("POS", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("tại quầy", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("cửa hàng", StringComparison.OrdinalIgnoreCase)
            )
            {
                return "Cửa hàng";
            }

            return "Cửa hàng";
        }
    }
}