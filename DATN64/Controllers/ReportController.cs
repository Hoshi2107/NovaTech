using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Models.ViewModels;
using DATN64.Helpers;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using ClosedXML.Excel;

namespace DATN64.Controllers
{
    [HasPermission("View_Report")]
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────
        //  DASHBOARD
        // ─────────────────────────────────────────────────────────
        public IActionResult Index()
        {
            var today     = DateTime.Today;
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);
            var last7Days = today.AddDays(-6);

            // Chỉ tính đơn "Hoàn thành" cho doanh thu
            var completedOrders = _context.DonHangs
                .Where(d => d.TrangThai == "Hoàn thành")
                .ToList();

            var vm = new ReportDashboardViewModel
            {
                DoanhThuHomNay = completedOrders
                    .Where(d => d.NgayDat.HasValue && d.NgayDat.Value.Date == today)
                    .Sum(d => d.TongTien ?? 0),

                DoanhThuThang = completedOrders
                    .Where(d => d.NgayDat.HasValue && d.NgayDat.Value >= firstOfMonth)
                    .Sum(d => d.TongTien ?? 0),

                TongDonHang  = _context.DonHangs.Count(),
                DonHoanThanh = _context.DonHangs.Count(d => d.TrangThai == "Hoàn thành"),
                DonHuy       = _context.DonHangs.Count(d => d.TrangThai == "Đã hủy"),
                TongKhachHang = _context.KhachHangs.Count(),
            };

            // Top 10 sản phẩm bán chạy — chỉ tính đơn "Hoàn thành"
            // Dùng LINQ Join thủ công theo yêu cầu
            var topProducts = (
                from ct in _context.ChiTietDonHangs
                join dh in _context.DonHangs
                    on ct.MaDonHang equals dh.MaDonHang
                join sp in _context.SanPhams
                    on ct.MaSanPham equals sp.MaSanPham
                where dh.TrangThai == "Hoàn thành"
                group new { ct, sp } by new { ct.MaSanPham, sp.TenSanPham } into g
                orderby g.Sum(x => x.ct.SoLuong) descending
                select new TopProductDto
                {
                    TenSanPham     = g.Key.TenSanPham,
                    TongSoLuongBan = g.Sum(x => x.ct.SoLuong),
                    DoanhThu       = g.Sum(x => x.ct.SoLuong * x.ct.DonGia)
                }
            ).Take(10).ToList();
            vm.TopProducts = topProducts;

            // Doanh thu 7 ngày gần nhất
            var revenue7 = (
                from dh in _context.DonHangs
                where dh.TrangThai == "Hoàn thành"
                   && dh.NgayDat.HasValue
                   && dh.NgayDat.Value.Date >= last7Days
                group dh by dh.NgayDat!.Value.Date into g
                orderby g.Key
                select new RevenueChartDto
                {
                    NgayLabel = g.Key.ToString("dd/MM"),
                    DoanhThu  = g.Sum(x => x.TongTien ?? 0)
                }
            ).ToList();

            // Điền ngày 0 doanh thu cho ngày không có đơn
            var allDays = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-6 + i))
                .Select(d => new RevenueChartDto
                {
                    NgayLabel = d.ToString("dd/MM"),
                    DoanhThu  = revenue7.FirstOrDefault(r => r.NgayLabel == d.ToString("dd/MM"))?.DoanhThu ?? 0
                }).ToList();
            vm.Revenue7Days = allDays;

            // Sản phẩm sắp hết (tồn kho <= 5)
            vm.LowStockProducts = _context.SanPhams
                .Where(sp => sp.SoLuongTon <= 5)
                .OrderBy(sp => sp.SoLuongTon)
                .Take(20)
                .Select(sp => new ProductLowStockDto
                {
                    TenSanPham = sp.TenSanPham,
                    SoLuongTon = sp.SoLuongTon,
                    TrangThai  = sp.TrangThai ?? ""
                }).ToList();

            return View(vm);
        }

        // ─────────────────────────────────────────────────────────
        //  BÁO CÁO BÁN HÀNG
        // ─────────────────────────────────────────────────────────
        public IActionResult SaleReport(DateTime? tuNgay, DateTime? denNgay, string? trangThai)
        {
            // Default: tháng hiện tại (đổi from/to → dateFrom/dateTo tránh xung đột keyword LINQ)
            var dateFrom = tuNgay ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var dateTo   = denNgay.HasValue ? denNgay.Value.Date.AddDays(1).AddSeconds(-1) : DateTime.Today.Date.AddDays(1).AddSeconds(-1);

            // LINQ Join để lấy thông tin khách hàng
            var query =
                from dh in _context.DonHangs
                join kh in _context.KhachHangs
                    on dh.MaKhachHang equals kh.MaKhachHang into khGroup
                from kh in khGroup.DefaultIfEmpty()  // LEFT JOIN
                where dh.NgayDat.HasValue
                   && dh.NgayDat.Value >= dateFrom
                   && dh.NgayDat.Value <= dateTo
                select new { dh, kh };

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(x => x.dh.TrangThai == trangThai);

            var rows = query
                .OrderByDescending(x => x.dh.NgayDat)
                .Select(x => new SaleReportRowDto
                {
                    MaDonHang    = x.dh.MaDonHang,
                    NgayDat      = x.dh.NgayDat,
                    TenKhachHang = x.kh != null ? x.kh.HoTen : "Khách lẻ",
                    TongTien     = x.dh.TongTien ?? 0,
                    TrangThai    = x.dh.TrangThai ?? "",
                    PhuongThuc   = x.dh.PhuongThucThanhToan ?? ""
                }).ToList();

            var vm = new SaleReportViewModel
            {
                TuNgay          = dateFrom,
                DenNgay         = dateTo.Date,
                TrangThaiFilter = trangThai,
                Rows            = rows,
                TongDonHang     = rows.Count,
                TongDoanhThu    = rows.Where(r => r.TrangThai == "Hoàn thành").Sum(r => r.TongTien),
                TrungBinhDonHang = rows.Any(r => r.TrangThai == "Hoàn thành")
                    ? rows.Where(r => r.TrangThai == "Hoàn thành").Average(r => r.TongTien)
                    : 0
            };

            return View(vm);
        }

        // ─────────────────────────────────────────────────────────
        //  XUẤT EXCEL (giới hạn 20.000 dòng)
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        [HasPermission("Export_Report")]
        public IActionResult ExportExcel(DateTime? tuNgay, DateTime? denNgay, string? trangThai)
        {
            var dateFrom = tuNgay ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var dateTo   = denNgay.HasValue ? denNgay.Value.Date.AddDays(1).AddSeconds(-1) : DateTime.Today.Date.AddDays(1).AddSeconds(-1);

            var query =
                from dh in _context.DonHangs
                join kh in _context.KhachHangs
                    on dh.MaKhachHang equals kh.MaKhachHang into khGroup
                from kh in khGroup.DefaultIfEmpty()
                where dh.NgayDat.HasValue
                   && dh.NgayDat.Value >= dateFrom
                   && dh.NgayDat.Value <= dateTo
                select new { dh, kh };

            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(x => x.dh.TrangThai == trangThai);

            // Giới hạn 20.000 dòng để tránh hết RAM
            var rows = query
                .OrderByDescending(x => x.dh.NgayDat)
                .Take(20000)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Báo cáo bán hàng");

            // Header
            string[] headers = { "Mã ĐH", "Ngày đặt", "Khách hàng", "Tổng tiền", "Trạng thái", "Thanh toán" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0284c7");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            // Data rows
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                int row = i + 2;
                ws.Cell(row, 1).Value = r.dh.MaDonHang;
                ws.Cell(row, 2).Value = r.dh.NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws.Cell(row, 3).Value = r.kh?.HoTen ?? "Khách lẻ";
                ws.Cell(row, 4).Value = (double)(r.dh.TongTien ?? 0);
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 5).Value = r.dh.TrangThai ?? "";
                ws.Cell(row, 6).Value = r.dh.PhuongThucThanhToan ?? "";

                if (i % 2 == 1)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
            }

            ws.Columns().AdjustToContents();

            // Ghi chú nếu bị giới hạn
            if (rows.Count == 20000)
            {
                ws.Cell(rows.Count + 3, 1).Value = "⚠ Dữ liệu bị giới hạn tối đa 20.000 dòng.";
                ws.Cell(rows.Count + 3, 1).Style.Font.FontColor = XLColor.Red;
            }

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, System.IO.SeekOrigin.Begin);

            string fileName = $"BaoCaoBanHang_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ─────────────────────────────────────────────────────────
        //  BÁO CÁO KHO
        // ─────────────────────────────────────────────────────────
        public IActionResult InventoryReport()
        {
            var products = _context.SanPhams.ToList();

            var items = products
                .OrderBy(sp => sp.SoLuongTon)
                .Select(sp => new InventoryItemDto
                {
                    MaSanPham  = sp.MaSanPham,
                    TenSanPham = sp.TenSanPham,
                    SoLuongTon = sp.SoLuongTon,
                    GiaNhap    = sp.GiaNhap,
                    GiaBan     = sp.GiaBan,
                    GiaTriVon  = sp.SoLuongTon * sp.GiaNhap,   // Giá vốn
                    TrangThai  = sp.TrangThai ?? ""
                }).ToList();

            // Lịch sử nhập hàng gần nhất (10 phiếu, LINQ Join thủ công)
            var recentImports = (
                from pn in _context.PhieuNhaps
                join ncc in _context.NhaCungCaps
                    on pn.MaNCC equals ncc.MaNCC
                join ct in _context.ChiTietPhieuNhaps
                    on pn.MaPhieuNhap equals ct.MaPhieuNhap into ctGroup
                orderby pn.NgayNhap descending
                select new ImportHistoryDto
                {
                    MaPhieuNhap = pn.MaPhieuNhap,
                    NgayNhap    = pn.NgayNhap,
                    NhaCungCap  = ncc.TenNCC,
                    TongSoLuong = _context.ChiTietPhieuNhaps
                                    .Where(c => c.MaPhieuNhap == pn.MaPhieuNhap)
                                    .Sum(c => c.SoLuong),
                    TongGiaTri  = _context.ChiTietPhieuNhaps
                                    .Where(c => c.MaPhieuNhap == pn.MaPhieuNhap)
                                    .Sum(c => c.SoLuong * c.GiaNhap)
                }
            ).Take(10).ToList();

            var vm = new InventoryReportViewModel
            {
                TongSKU           = items.Count,
                SoLuongDuHang     = items.Count(x => x.SoLuongTon > 5),
                SoLuongSapHet     = items.Count(x => x.SoLuongTon > 0 && x.SoLuongTon <= 5),
                SoLuongHetHang    = items.Count(x => x.SoLuongTon == 0),
                TongGiaTriTonKho  = items.Sum(x => x.GiaTriVon),
                Items             = items,
                RecentImports     = recentImports
            };

            return View(vm);
        }
    }
}
