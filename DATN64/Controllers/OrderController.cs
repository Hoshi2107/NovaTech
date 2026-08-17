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

        public IActionResult Index(string? keyword, string? status, string? channel, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;

            var allowedPageSizes = new[] { 10, 20, 50 };
            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 20;
            }

            keyword = (keyword ?? string.Empty).Trim();
            status = (status ?? string.Empty).Trim();
            channel = (channel ?? string.Empty).Trim();

            var query = _context.DonHangs
                .Include(o => o.KhachHang)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                if (int.TryParse(keyword, out var orderId))
                {
                    query = query.Where(o =>
                        o.MaDonHang == orderId ||
                        (
                            o.KhachHang != null &&
                            (
                                o.KhachHang.HoTen.Contains(keyword) ||
                                (o.KhachHang.SoDienThoai != null && o.KhachHang.SoDienThoai.Contains(keyword)) ||
                                (o.KhachHang.Email != null && o.KhachHang.Email.Contains(keyword))
                            )
                        )
                    );
                }
                else
                {
                    query = query.Where(o =>
                        o.KhachHang != null &&
                        (
                            o.KhachHang.HoTen.Contains(keyword) ||
                            (o.KhachHang.SoDienThoai != null && o.KhachHang.SoDienThoai.Contains(keyword)) ||
                            (o.KhachHang.Email != null && o.KhachHang.Email.Contains(keyword))
                        )
                    );
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Hoàn thành")
                {
                    query = query.Where(o => o.TrangThai == "Hoàn thành" || o.TrangThai == "Đã giao");
                }
                else
                {
                    query = query.Where(o => o.TrangThai == status);
                }
            }

            /*
             * Phân loại kênh bán:
             * - TikTok Shop: đơn có TikTok / TikTok Shop / [TikTokShop#] / [StreamSim#]
             * - Website: đơn có giao hàng / voucher / website / online nhưng KHÔNG phải TikTok
             * - Cửa hàng: đơn không thuộc Website và không thuộc TikTok Shop
             */

            if (channel == "TikTok Shop")
            {
                query = query.Where(o =>
                    (o.GhiChu != null &&
                        (
                            o.GhiChu.Contains("[TikTokShop#") ||
                            o.GhiChu.Contains("[StreamSim#") ||
                            o.GhiChu.Contains("TikTok Shop") ||
                            o.GhiChu.Contains("TikTok") ||
                            o.GhiChu.Contains("Livestream")
                        )
                    ) ||
                    (o.PhuongThucThanhToan != null &&
                        (
                            o.PhuongThucThanhToan.Contains("TikTok Shop") ||
                            o.PhuongThucThanhToan.Contains("TikTok") ||
                            o.PhuongThucThanhToan.Contains("Stream") ||
                            o.PhuongThucThanhToan.Contains("Livestream")
                        )
                    )
                );
            }
            else if (channel == "Website")
            {
                query = query.Where(o =>
                    !(
                        (o.GhiChu != null &&
                            (
                                o.GhiChu.Contains("[TikTokShop#") ||
                                o.GhiChu.Contains("[StreamSim#") ||
                                o.GhiChu.Contains("TikTok Shop") ||
                                o.GhiChu.Contains("TikTok") ||
                                o.GhiChu.Contains("Livestream")
                            )
                        ) ||
                        (o.PhuongThucThanhToan != null &&
                            (
                                o.PhuongThucThanhToan.Contains("TikTok Shop") ||
                                o.PhuongThucThanhToan.Contains("TikTok") ||
                                o.PhuongThucThanhToan.Contains("Stream") ||
                                o.PhuongThucThanhToan.Contains("Livestream")
                            )
                        )
                    )
                    &&
                    (
                        (o.GhiChu != null &&
                            (
                                o.GhiChu.Contains("Địa chỉ giao hàng") ||
                                o.GhiChu.Contains("Voucher sử dụng") ||
                                o.GhiChu.Contains("giao hàng") ||
                                o.GhiChu.Contains("website") ||
                                o.GhiChu.Contains("online")
                            )
                        ) ||
                        (o.PhuongThucThanhToan != null &&
                            (
                                o.PhuongThucThanhToan.Contains("online") ||
                                o.PhuongThucThanhToan.Contains("website")
                            )
                        )
                    )
                );
            }
            else if (channel == "Cửa hàng")
            {
                query = query.Where(o =>
                    !(
                        (o.GhiChu != null &&
                            (
                                o.GhiChu.Contains("[TikTokShop#") ||
                                o.GhiChu.Contains("[StreamSim#") ||
                                o.GhiChu.Contains("TikTok Shop") ||
                                o.GhiChu.Contains("TikTok") ||
                                o.GhiChu.Contains("Livestream") ||
                                o.GhiChu.Contains("Địa chỉ giao hàng") ||
                                o.GhiChu.Contains("Voucher sử dụng") ||
                                o.GhiChu.Contains("giao hàng") ||
                                o.GhiChu.Contains("website") ||
                                o.GhiChu.Contains("online")
                            )
                        ) ||
                        (o.PhuongThucThanhToan != null &&
                            (
                                o.PhuongThucThanhToan.Contains("TikTok Shop") ||
                                o.PhuongThucThanhToan.Contains("TikTok") ||
                                o.PhuongThucThanhToan.Contains("Stream") ||
                                o.PhuongThucThanhToan.Contains("Livestream") ||
                                o.PhuongThucThanhToan.Contains("online") ||
                                o.PhuongThucThanhToan.Contains("website")
                            )
                        )
                    )
                );
            }

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var orders = query
                .OrderByDescending(o => o.NgayDat)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            foreach (var order in orders)
            {
                order.TrangThai = NormalizeOrderStatus(order.TrangThai);
            }

            ViewBag.Keyword = keyword;
            ViewBag.StatusFilter = status;
            ViewBag.ChannelFilter = channel;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

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
                TrangThai = NormalizeOrderStatus(order.TrangThai),
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
                .Include(d => d.KhachHang)
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

            var oldStatus = NormalizeOrderStatus(order.TrangThai);
            var newStatus = NormalizeOrderStatus(trangThai.Trim());
            var orderChannel = GetOrderChannel(order);

            if (oldStatus == "Hoàn thành")
            {
                TempData["ToastMessage"] = "Đơn hàng đã hoàn thành nên không thể cập nhật trạng thái nữa.";
                TempData["ToastType"] = "info";
                return RedirectToAction(nameof(Detail), new { id });
            }

            order.TrangThai = newStatus;

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

                _context.SaveChanges();
                return RedirectToAction(nameof(Detail), new { id });
            }

            if (newStatus == "Hoàn thành")
            {
                if (!HasCompletedChannelMarker(order.GhiChu))
                {
                    order.GhiChu = AppendCompletedChannelMarker(order.GhiChu, orderChannel);
                }

                if (HasAddedLoyaltyPoint(order))
                {
                    TempData["ToastMessage"] = $"Đơn đã hoàn thành từ kênh {orderChannel}. Đơn hàng đã được cộng điểm trước đó.";
                    TempData["ToastType"] = "info";

                    _context.SaveChanges();
                    return RedirectToAction(nameof(Detail), new { id });
                }

                if (order.KhachHang == null)
                {
                    TempData["ToastMessage"] = $"Đơn đã hoàn thành từ kênh {orderChannel} nhưng không có khách hàng thật nên không cộng điểm.";
                    TempData["ToastType"] = "warning";

                    _context.SaveChanges();
                    return RedirectToAction(nameof(Detail), new { id });
                }

                var tongTien = order.TongTien ?? 0;
                var diemCong = (int)(tongTien / 10000);

                if (diemCong <= 0)
                {
                    TempData["ToastMessage"] = $"Đơn đã hoàn thành từ kênh {orderChannel} nhưng tổng tiền chưa đủ để cộng điểm.";
                    TempData["ToastType"] = "warning";

                    _context.SaveChanges();
                    return RedirectToAction(nameof(Detail), new { id });
                }

                order.KhachHang.DiemTichLuy += diemCong;
                order.GhiChu = AppendLoyaltyPointMarker(order.GhiChu);

                _context.SaveChanges();

                TempData["ToastMessage"] = $"Đơn đã hoàn thành từ kênh {orderChannel}. Đã cộng {diemCong:N0} điểm cho khách hàng.";
                TempData["ToastType"] = "success";

                return RedirectToAction(nameof(Detail), new { id });
            }

            _context.SaveChanges();

            TempData["ToastMessage"] = "Cập nhật trạng thái đơn hàng thành công.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Detail), new { id });
        }

        private bool HasAddedLoyaltyPoint(DonHang order)
        {
            return !string.IsNullOrWhiteSpace(order.GhiChu)
                && order.GhiChu.Contains("[DA_CONG_DIEM]", StringComparison.OrdinalIgnoreCase);
        }

        private string AppendLoyaltyPointMarker(string? ghiChu)
        {
            if (string.IsNullOrWhiteSpace(ghiChu))
            {
                return "[DA_CONG_DIEM]";
            }

            if (ghiChu.Contains("[DA_CONG_DIEM]", StringComparison.OrdinalIgnoreCase))
            {
                return ghiChu;
            }

            return ghiChu + " [DA_CONG_DIEM]";
        }

        private bool HasCompletedChannelMarker(string? ghiChu)
        {
            return !string.IsNullOrWhiteSpace(ghiChu)
                && ghiChu.Contains("[NHAN_HANG_", StringComparison.OrdinalIgnoreCase);
        }

        private string AppendCompletedChannelMarker(string? ghiChu, string channel)
        {
            var marker = channel switch
            {
                "TikTok Shop" => "[NHAN_HANG_TIKTOK_SHOP]",
                "Website" => "[NHAN_HANG_WEBSITE]",
                "Cửa hàng" => "[NHAN_HANG_CUA_HANG]",
                _ => "[NHAN_HANG_KHAC]"
            };

            var text = channel switch
            {
                "TikTok Shop" => "Khách đã nhận hàng từ TikTok Shop",
                "Website" => "Khách đã nhận hàng từ Website",
                "Cửa hàng" => "Khách đã nhận tại cửa hàng",
                _ => "Khách đã nhận hàng"
            };

            if (string.IsNullOrWhiteSpace(ghiChu))
            {
                return $"{marker} {text}.";
            }

            if (ghiChu.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return ghiChu;
            }

            return $"{ghiChu} {marker} {text}.";
        }

        private string NormalizeOrderStatus(string? status)
        {
            status = (status ?? "Đơn mới").Trim();

            if (status == "Đã giao")
            {
                return "Hoàn thành";
            }

            return status;
        }

        private string GetOrderChannel(DonHang order)
        {
            var note = order.GhiChu ?? "";
            var payment = order.PhuongThucThanhToan ?? "";

            if (
                note.Contains("[TikTokShop#", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("[StreamSim#", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("TikTok Shop", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("TikTok", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("Livestream", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("TikTok Shop", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("TikTok", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("Stream", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("Livestream", StringComparison.OrdinalIgnoreCase)
            )
            {
                return "TikTok Shop";
            }

            if (
                note.Contains("Địa chỉ giao hàng", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("Voucher sử dụng", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("giao hàng", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("website", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("online", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("online", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("website", StringComparison.OrdinalIgnoreCase)
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

        public IActionResult ShippingLabels(string? keyword, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;

            var query = _context.DonHangs
                .Include(o => o.KhachHang)
                .Where(o => o.TrangThai == "Đã xác nhận" || o.TrangThai == "Đã duyệt")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                if (int.TryParse(keyword, out var orderId))
                {
                    query = query.Where(o =>
                        o.MaDonHang == orderId ||
                        (
                            o.KhachHang != null &&
                            (
                                o.KhachHang.HoTen.Contains(keyword) ||
                                (o.KhachHang.SoDienThoai != null && o.KhachHang.SoDienThoai.Contains(keyword))
                            )
                        )
                    );
                }
                else
                {
                    query = query.Where(o =>
                        o.KhachHang != null &&
                        (
                            o.KhachHang.HoTen.Contains(keyword) ||
                            (o.KhachHang.SoDienThoai != null && o.KhachHang.SoDienThoai.Contains(keyword))
                        )
                    );
                }
            }

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var orders = query
                .OrderByDescending(o => o.NgayDat)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(orders);
        }

        public IActionResult PrintLabels(string ids)
        {
            if (string.IsNullOrEmpty(ids))
            {
                return Content("<script>alert('Vui lòng chọn ít nhất 1 đơn hàng để in!'); window.close();</script>", "text/html; charset=utf-8");
            }

            var idList = ids.Split(',')
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            var orders = _context.DonHangs
                .Include(o => o.KhachHang)
                .Include(o => o.ChiTietDonHangs)
                    .ThenInclude(ct => ct.SanPham)
                .Where(o => idList.Contains(o.MaDonHang))
                .ToList();

            var storeConfig = _context.CauHinhs.FirstOrDefault() ?? new CauHinh
            {
                TenCuaHang = "Siêu thị NovaTech",
                Email = "contact@novatech.vn",
                SoDienThoai = "1900 1000",
                DiaChi = "123 Đường Điện Biên Phủ, TP.HCM"
            };

            ViewBag.StoreConfig = storeConfig;

            return View(orders);
        }
    }
}