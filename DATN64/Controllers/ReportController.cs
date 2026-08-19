using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // ─────────────────────────────────────────────────────────
        //  BÁO CÁO BIẾN ĐỘNG GIÁ NHẬP KHO THEO THỜI GIAN
        // ─────────────────────────────────────────────────────────
        public IActionResult ImportPriceHistory(int? productId, int? supplierId, DateTime? tuNgay, DateTime? denNgay, string? search, int page = 1, int pageSize = 10)
        {
            EnsureImportHistorySeeded();

            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var products = _context.SanPhams.OrderBy(p => p.TenSanPham).ToList();
            var suppliers = _context.NhaCungCaps.OrderBy(s => s.TenNCC).ToList();

            var query = from ct in _context.ChiTietPhieuNhaps
                        join pn in _context.PhieuNhaps on ct.MaPhieuNhap equals pn.MaPhieuNhap
                        join sp in _context.SanPhams on ct.MaSanPham equals sp.MaSanPham
                        join ncc in _context.NhaCungCaps on pn.MaNCC equals ncc.MaNCC
                        join nv in _context.NhanViens on pn.MaNhanVien equals nv.MaNhanVien into nvGroup
                        from nv in nvGroup.DefaultIfEmpty()
                        select new
                        {
                            ct.MaChiTiet,
                            ct.MaPhieuNhap,
                            pn.NgayNhap,
                            ct.MaSanPham,
                            sp.TenSanPham,
                            sp.SKU,
                            sp.HinhAnh,
                            pn.MaNCC,
                            ncc.TenNCC,
                            ct.SoLuong,
                            ct.GiaNhap,
                            sp.GiaBan,
                            ThanhTien = ct.SoLuong * ct.GiaNhap,
                            NguoiNhap = nv != null ? nv.HoTen : "Thủ kho"
                        };

            if (productId.HasValue && productId.Value > 0)
                query = query.Where(x => x.MaSanPham == productId.Value);

            if (supplierId.HasValue && supplierId.Value > 0)
                query = query.Where(x => x.MaNCC == supplierId.Value);

            if (tuNgay.HasValue)
                query = query.Where(x => x.NgayNhap >= tuNgay.Value.Date);

            if (denNgay.HasValue)
                query = query.Where(x => x.NgayNhap <= denNgay.Value.Date.AddDays(1).AddSeconds(-1));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.TenSanPham.ToLower().Contains(s) || (x.SKU != null && x.SKU.ToLower().Contains(s)) || x.TenNCC.ToLower().Contains(s));
            }

            var rawRows = query.OrderBy(x => x.NgayNhap).ToList();

            // Tính biến động giá so với lần nhập trước theo từng sản phẩm
            var lastPriceMap = new Dictionary<int, decimal>();
            var rowsWithFluctuation = new List<ImportPriceHistoryRowDto>();

            foreach (var item in rawRows)
            {
                decimal diffVND = 0;
                double diffPercent = 0;

                if (lastPriceMap.TryGetValue(item.MaSanPham, out decimal prevPrice) && prevPrice > 0)
                {
                    diffVND = item.GiaNhap - prevPrice;
                    diffPercent = (double)((diffVND / prevPrice) * 100);
                }

                lastPriceMap[item.MaSanPham] = item.GiaNhap;

                rowsWithFluctuation.Add(new ImportPriceHistoryRowDto
                {
                    MaPhieuNhap = item.MaPhieuNhap,
                    NgayNhap = item.NgayNhap,
                    MaSanPham = item.MaSanPham,
                    TenSanPham = item.TenSanPham,
                    SKU = item.SKU ?? "",
                    HinhAnh = item.HinhAnh ?? "",
                    TenNCC = item.TenNCC,
                    SoLuongNhap = item.SoLuong,
                    GiaNhap = item.GiaNhap,
                    GiaBan = item.GiaBan,
                    ThanhTien = item.ThanhTien,
                    BienDongGiaVND = diffVND,
                    BienDongGiaPercent = Math.Round(diffPercent, 1),
                    NguoiNhap = item.NguoiNhap
                });
            }

            // Đảo lại theo thứ tự mới nhất lên đầu để hiển thị bảng
            var displayRows = rowsWithFluctuation.OrderByDescending(r => r.NgayNhap).ToList();

            // Phân trang
            var pagedRows = displayRows.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Chart data logic thông minh:
            List<PriceChartPointDto> chartData;
            if (productId.HasValue && productId.Value > 0)
            {
                // Khi chọn 1 SP cụ thể: Vẽ biến động giá nhập từng đợt của SP đó
                chartData = rowsWithFluctuation
                    .OrderBy(r => r.NgayNhap)
                    .Select(r => new PriceChartPointDto
                    {
                        NgayLabel = r.NgayNhap?.ToString("dd/MM/yyyy") ?? "",
                        TenSanPham = r.TenSanPham,
                        GiaNhap = r.GiaNhap,
                        SoLuong = r.SoLuongNhap,
                        TongTien = r.ThanhTien
                    })
                    .ToList();
            }
            else
            {
                // Khi xem Tất cả SP: Gom nhóm theo Ngày để vẽ Tổng tiền vốn (Cột) & Tổng số lượng (Đường)
                chartData = rowsWithFluctuation
                    .GroupBy(r => r.NgayNhap.HasValue ? r.NgayNhap.Value.ToString("dd/MM/yyyy") : "")
                    .Select(g => new PriceChartPointDto
                    {
                        NgayLabel = g.Key,
                        TenSanPham = "Tổng nhập",
                        GiaNhap = g.Average(x => x.GiaNhap),
                        SoLuong = g.Sum(x => x.SoLuongNhap),
                        TongTien = g.Sum(x => x.ThanhTien)
                    })
                    .ToList();
            }

            var vm = new ImportPriceHistoryViewModel
            {
                SelectedProductId = productId,
                SelectedSupplierId = supplierId,
                TuNgay = tuNgay,
                DenNgay = denNgay,
                Search = search,
                Page = page,
                PageSize = pageSize,
                ProductsList = products,
                SuppliersList = suppliers,
                Rows = displayRows,
                PagedRows = pagedRows,
                ChartData = chartData,
                TongSoLanNhap = displayRows.Count,
                TongSoLuongNhap = displayRows.Sum(r => r.SoLuongNhap),
                TongGiaTriNhap = displayRows.Sum(r => r.ThanhTien),
                GiaNhapHienTai = displayRows.FirstOrDefault()?.GiaNhap ?? 0,
                GiaNhapCaoNhat = displayRows.Any() ? displayRows.Max(r => r.GiaNhap) : 0,
                GiaNhapThapNhat = displayRows.Any() ? displayRows.Min(r => r.GiaNhap) : 0,
                GiaNhapTrungBinh = displayRows.Any() ? displayRows.Average(r => r.GiaNhap) : 0
            };

            return View(vm);
        }

        // ─────────────────────────────────────────────────────────
        //  XUẤT EXCEL BIẾN ĐỘNG GIÁ NHẬP KHO
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        [HasPermission("Export_Report")]
        public IActionResult ExportImportPriceHistoryExcel(int? productId, int? supplierId, DateTime? tuNgay, DateTime? denNgay, string? search)
        {
            EnsureImportHistorySeeded();

            var query = from ct in _context.ChiTietPhieuNhaps
                        join pn in _context.PhieuNhaps on ct.MaPhieuNhap equals pn.MaPhieuNhap
                        join sp in _context.SanPhams on ct.MaSanPham equals sp.MaSanPham
                        join ncc in _context.NhaCungCaps on pn.MaNCC equals ncc.MaNCC
                        join nv in _context.NhanViens on pn.MaNhanVien equals nv.MaNhanVien into nvGroup
                        from nv in nvGroup.DefaultIfEmpty()
                        select new
                        {
                            ct.MaChiTiet,
                            ct.MaPhieuNhap,
                            pn.NgayNhap,
                            ct.MaSanPham,
                            sp.TenSanPham,
                            sp.SKU,
                            pn.MaNCC,
                            ncc.TenNCC,
                            ct.SoLuong,
                            ct.GiaNhap,
                            sp.GiaBan,
                            ThanhTien = ct.SoLuong * ct.GiaNhap,
                            NguoiNhap = nv != null ? nv.HoTen : "Thủ kho"
                        };

            if (productId.HasValue && productId.Value > 0)
                query = query.Where(x => x.MaSanPham == productId.Value);

            if (supplierId.HasValue && supplierId.Value > 0)
                query = query.Where(x => x.MaNCC == supplierId.Value);

            if (tuNgay.HasValue)
                query = query.Where(x => x.NgayNhap >= tuNgay.Value.Date);

            if (denNgay.HasValue)
                query = query.Where(x => x.NgayNhap <= denNgay.Value.Date.AddDays(1).AddSeconds(-1));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.TenSanPham.ToLower().Contains(s) || (x.SKU != null && x.SKU.ToLower().Contains(s)) || x.TenNCC.ToLower().Contains(s));
            }

            var rawRows = query.OrderBy(x => x.NgayNhap).ToList();
            var lastPriceMap = new Dictionary<int, decimal>();
            var rows = new List<ImportPriceHistoryRowDto>();

            foreach (var item in rawRows)
            {
                decimal diffVND = 0;
                double diffPercent = 0;

                if (lastPriceMap.TryGetValue(item.MaSanPham, out decimal prevPrice) && prevPrice > 0)
                {
                    diffVND = item.GiaNhap - prevPrice;
                    diffPercent = (double)((diffVND / prevPrice) * 100);
                }

                lastPriceMap[item.MaSanPham] = item.GiaNhap;

                rows.Add(new ImportPriceHistoryRowDto
                {
                    MaPhieuNhap = item.MaPhieuNhap,
                    NgayNhap = item.NgayNhap,
                    MaSanPham = item.MaSanPham,
                    TenSanPham = item.TenSanPham,
                    SKU = item.SKU ?? "",
                    TenNCC = item.TenNCC,
                    SoLuongNhap = item.SoLuong,
                    GiaNhap = item.GiaNhap,
                    GiaBan = item.GiaBan,
                    ThanhTien = item.ThanhTien,
                    BienDongGiaVND = diffVND,
                    BienDongGiaPercent = Math.Round(diffPercent, 1),
                    NguoiNhap = item.NguoiNhap
                });
            }

            rows = rows.OrderByDescending(r => r.NgayNhap).ToList();

            using var workbook = new XLWorkbook();
            
            // Sheet 1: Lịch sử nhập hàng & Biến động giá
            var ws1 = workbook.Worksheets.Add("Biến động giá nhập");

            // Title Banner
            ws1.Cell(1, 1).Value = "BÁO CÁO CHI TIẾT BIẾN ĐỘNG GIÁ NHẬP KHO THEO THỜI GIAN";
            ws1.Range(1, 1, 1, 10).Merge();
            ws1.Cell(1, 1).Style.Font.Bold = true;
            ws1.Cell(1, 1).Style.Font.FontSize = 14;
            ws1.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws1.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
            ws1.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] headers = { "Mã Phiếu", "Ngày nhập", "Tên sản phẩm", "Mã SKU", "Nhà cung cấp", "SL Nhập", "Đơn giá nhập (VNĐ)", "Giá bán niêm yết", "Thành tiền (VNĐ)", "Biến động giá", "Người nhập" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws1.Cell(3, i + 1).Value = headers[i];
                ws1.Cell(3, i + 1).Style.Font.Bold = true;
                ws1.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0284c7");
                ws1.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
                ws1.Cell(3, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                int row = i + 4;
                ws1.Cell(row, 1).Value = "PN" + r.MaPhieuNhap.ToString("D6");
                ws1.Cell(row, 2).Value = r.NgayNhap?.ToString("dd/MM/yyyy HH:mm") ?? "";
                ws1.Cell(row, 3).Value = r.TenSanPham;
                ws1.Cell(row, 4).Value = r.SKU;
                ws1.Cell(row, 5).Value = r.TenNCC;
                ws1.Cell(row, 6).Value = r.SoLuongNhap;
                ws1.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                
                ws1.Cell(row, 7).Value = (double)r.GiaNhap;
                ws1.Cell(row, 7).Style.NumberFormat.Format = "#,##0";

                ws1.Cell(row, 8).Value = (double)r.GiaBan;
                ws1.Cell(row, 8).Style.NumberFormat.Format = "#,##0";

                ws1.Cell(row, 9).Value = (double)r.ThanhTien;
                ws1.Cell(row, 9).Style.NumberFormat.Format = "#,##0";

                if (r.BienDongGiaVND > 0)
                {
                    ws1.Cell(row, 10).Value = $"▲ +{r.BienDongGiaVND:N0}đ (+{r.BienDongGiaPercent}%)";
                    ws1.Cell(row, 10).Style.Font.FontColor = XLColor.FromHtml("#dc2626"); // Đỏ (tăng giá vốn)
                }
                else if (r.BienDongGiaVND < 0)
                {
                    ws1.Cell(row, 10).Value = $"▼ {r.BienDongGiaVND:N0}đ ({r.BienDongGiaPercent}%)";
                    ws1.Cell(row, 10).Style.Font.FontColor = XLColor.FromHtml("#16a34a"); // Xanh (giảm giá vốn)
                }
                else
                {
                    ws1.Cell(row, 10).Value = "— Ổn định";
                    ws1.Cell(row, 10).Style.Font.FontColor = XLColor.Gray;
                }

                ws1.Cell(row, 11).Value = r.NguoiNhap;

                if (i % 2 == 1)
                    ws1.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
            }

            ws1.Columns().AdjustToContents();

            // Sheet 2: Tổng hợp theo sản phẩm
            var ws2 = workbook.Worksheets.Add("Tổng hợp theo SP");
            string[] headers2 = { "Mã SP", "Tên sản phẩm", "SKU", "Số lần nhập", "Tổng SL nhập", "Giá nhập hiện tại", "Giá thấp nhất", "Giá cao nhất", "Giá trung bình", "Tổng tiền vốn nhập" };
            for (int i = 0; i < headers2.Length; i++)
            {
                ws2.Cell(1, i + 1).Value = headers2[i];
                ws2.Cell(1, i + 1).Style.Font.Bold = true;
                ws2.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#059669");
                ws2.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            var productGroups = rows.GroupBy(r => r.MaSanPham).ToList();
            int rIdx = 2;
            foreach (var g in productGroups)
            {
                var first = g.First();
                ws2.Cell(rIdx, 1).Value = g.Key;
                ws2.Cell(rIdx, 2).Value = first.TenSanPham;
                ws2.Cell(rIdx, 3).Value = first.SKU;
                ws2.Cell(rIdx, 4).Value = g.Count();
                ws2.Cell(rIdx, 5).Value = g.Sum(x => x.SoLuongNhap);
                ws2.Cell(rIdx, 6).Value = (double)first.GiaNhap;
                ws2.Cell(rIdx, 6).Style.NumberFormat.Format = "#,##0";
                ws2.Cell(rIdx, 7).Value = (double)g.Min(x => x.GiaNhap);
                ws2.Cell(rIdx, 7).Style.NumberFormat.Format = "#,##0";
                ws2.Cell(rIdx, 8).Value = (double)g.Max(x => x.GiaNhap);
                ws2.Cell(rIdx, 8).Style.NumberFormat.Format = "#,##0";
                ws2.Cell(rIdx, 9).Value = (double)g.Average(x => x.GiaNhap);
                ws2.Cell(rIdx, 9).Style.NumberFormat.Format = "#,##0";
                ws2.Cell(rIdx, 10).Value = (double)g.Sum(x => x.ThanhTien);
                ws2.Cell(rIdx, 10).Style.NumberFormat.Format = "#,##0";
                rIdx++;
            }
            ws2.Columns().AdjustToContents();

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, System.IO.SeekOrigin.Begin);

            string fileName = $"BaoCaoBienDongGiaNhap_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ─────────────────────────────────────────────────────────
        //  MÔ PHỎNG NHẬP HÀNG MỚI (CẬP NHẬT BIẾN ĐỘNG THEO THỜI GIAN THỰC)
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public IActionResult SimulateNewImport(int? productId, int? supplierId, int? quantity, decimal? customPrice, decimal? percentChange)
        {
            try
            {
                var product = productId.HasValue 
                    ? _context.SanPhams.Find(productId.Value) 
                    : _context.SanPhams.OrderBy(x => Guid.NewGuid()).FirstOrDefault();

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để nhập hàng.";
                    return RedirectToAction("ImportPriceHistory");
                }

                // Tìm giá nhập gần nhất của sản phẩm
                var lastImport = _context.ChiTietPhieuNhaps
                    .Include(c => c.PhieuNhap)
                    .Where(c => c.MaSanPham == product.MaSanPham)
                    .OrderByDescending(c => c.PhieuNhap != null ? c.PhieuNhap.NgayNhap : DateTime.MinValue)
                    .FirstOrDefault();

                decimal currentPrice = lastImport != null ? lastImport.GiaNhap : product.GiaNhap;
                if (currentPrice <= 0) currentPrice = 12000000m;

                decimal newPrice = currentPrice;
                if (customPrice.HasValue && customPrice.Value > 0)
                {
                    newPrice = customPrice.Value;
                }
                else if (percentChange.HasValue)
                {
                    newPrice = Math.Round((currentPrice * (1 + percentChange.Value / 100m)) / 10000m) * 10000m;
                }
                else
                {
                    var rand = new Random();
                    var pct = rand.Next(-6, 12);
                    if (pct == 0) pct = rand.Next(2, 6);
                    newPrice = Math.Round((currentPrice * (1 + pct / 100m)) / 10000m) * 10000m;
                }

                if (newPrice <= 0) newPrice = 100000m;

                int qty = (quantity.HasValue && quantity.Value > 0) ? quantity.Value : (new Random().Next(15, 60));
                int suppId = (supplierId.HasValue && supplierId.Value > 0)
                    ? supplierId.Value 
                    : (product.MaNCC > 0 ? product.MaNCC : (_context.NhaCungCaps.Select(n => n.MaNCC).FirstOrDefault()));

                var emp = _context.NhanViens.FirstOrDefault(e => e.VaiTro == "Admin" || e.VaiTro == "Quản lý kho");
                int empId = emp != null ? emp.MaNhanVien : 1;

                var pn = new PhieuNhap
                {
                    MaNCC = suppId > 0 ? suppId : 1,
                    MaNhanVien = empId,
                    NgayNhap = DateTime.Now
                };
                _context.PhieuNhaps.Add(pn);
                _context.SaveChanges();

                var ctpn = new ChiTietPhieuNhap
                {
                    MaPhieuNhap = pn.MaPhieuNhap,
                    MaSanPham = product.MaSanPham,
                    SoLuong = qty,
                    GiaNhap = newPrice
                };
                _context.ChiTietPhieuNhaps.Add(ctpn);

                // Cập nhật giá vốn hiện tại và số lượng tồn cho sản phẩm
                product.GiaNhap = newPrice;
                product.SoLuongTon += qty;
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Đã nhập hàng thành công: [{product.TenSanPham}] — Đơn giá: {newPrice:N0} đ, SL: {qty}!";
                return RedirectToAction("ImportPriceHistory", new { productId = product.MaSanPham });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi nhập hàng: " + ex.Message;
                return RedirectToAction("ImportPriceHistory", new { productId });
            }
        }

        // ─────────────────────────────────────────────────────────
        //  KHỞI TẠO LẠI TOÀN BỘ LỊCH SỬ BIẾN ĐỘNG GIÁ LIÊN TỤC
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public IActionResult ResetAndReseedImportHistory()
        {
            try
            {
                _context.Database.ExecuteSqlRaw("DELETE FROM dbo.ChiTietPhieuNhap; DELETE FROM dbo.PhieuNhap;");

                _hasRefreshedFluctuationSeed = false;
                EnsureImportHistorySeeded(force: true);

                TempData["SuccessMessage"] = "Đã làm mới và đồng bộ toàn bộ dữ liệu biến động giá nhập kho theo dòng thời gian cho tất cả sản phẩm!";
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Lỗi khi làm mới: " + msg;
            }

            return RedirectToAction("ImportPriceHistory");
        }

        // ─────────────────────────────────────────────────────────
        //  HELPER: SEED DỮ LIỆU LỊCH SỬ NHẬP HÀNG BIẾN ĐỘNG GIÁ
        // ─────────────────────────────────────────────────────────
        private static bool _hasRefreshedFluctuationSeed = false;

        private void EnsureImportHistorySeeded(bool force = false)
        {
            if (_hasRefreshedFluctuationSeed && !force) return;

            try
            {
                int existingCount = _context.ChiTietPhieuNhaps.Count();
                if (existingCount < 60 || force)
                {
                    var products = _context.SanPhams.ToList();
                    var suppliers = _context.NhaCungCaps.ToList();
                    var employees = _context.NhanViens.ToList();

                    if (products.Any())
                    {
                        var defaultSupp = suppliers.FirstOrDefault()?.MaNCC ?? 1;
                        var defaultEmp = employees.FirstOrDefault()?.MaNhanVien ?? 1;

                        // 8 đợt nhập theo dòng thời gian từ 70 ngày trước tới nay
                        var timeOffsets = new int[] { -70, -58, -45, -32, -22, -14, -6, -1 };
                        var factors = new decimal[] { 0.90m, 0.94m, 1.02m, 1.08m, 1.04m, 0.97m, 1.06m, 1.01m };
                        var baseQuantities = new int[] { 15, 25, 20, 35, 40, 30, 50, 45 };

                        var rand = new Random(42);

                        foreach (var prod in products)
                        {
                            decimal basePrice = prod.GiaNhap > 0 ? prod.GiaNhap : 12000000m;
                            int suppId = prod.MaNCC > 0 ? prod.MaNCC : defaultSupp;

                            for (int i = 0; i < timeOffsets.Length; i++)
                            {
                                var offsetDay = timeOffsets[i];
                                var importDate = DateTime.Now.AddDays(offsetDay)
                                    .Date.AddHours(rand.Next(8, 17))
                                    .AddMinutes(rand.Next(1, 59))
                                    .AddSeconds(rand.Next(1, 59));

                                decimal factor = factors[i % factors.Length];
                                decimal jitter = (decimal)(rand.Next(-15, 15)) / 1000m;
                                decimal price = Math.Round((basePrice * (factor + jitter)) / 10000m) * 10000m;
                                if (price <= 0) price = 100000m;

                                int qty = baseQuantities[i % baseQuantities.Length] + rand.Next(-5, 12);
                                if (qty <= 0) qty = 10;

                                var emp = employees.Count > 0 ? employees[rand.Next(employees.Count)] : null;
                                int empId = emp?.MaNhanVien ?? defaultEmp;

                                var pn = new PhieuNhap
                                {
                                    MaNCC = suppId,
                                    MaNhanVien = empId,
                                    NgayNhap = importDate
                                };
                                _context.PhieuNhaps.Add(pn);

                                var ctpn = new ChiTietPhieuNhap
                                {
                                    PhieuNhap = pn,
                                    MaSanPham = prod.MaSanPham,
                                    SoLuong = qty,
                                    GiaNhap = price
                                };
                                _context.ChiTietPhieuNhaps.Add(ctpn);
                            }
                        }
                        _context.SaveChanges();
                    }
                }

                // Đồng bộ mọi giao dịch Nhập kho đã duyệt từ InventoryTransactions sang PhieuNhap
                var approvedImports = _context.InventoryTransactions
                    .Where(t => t.Type == "Nhập kho" && t.TrangThai == "Đã duyệt")
                    .ToList();

                if (approvedImports.Any())
                {
                    bool hasNewSync = false;
                    foreach (var tx in approvedImports)
                    {
                        int pId = 0;
                        if (!int.TryParse(tx.ProductSKU, out pId) || pId <= 0)
                        {
                            var p = _context.SanPhams.FirstOrDefault(x => x.TenSanPham == tx.ProductName);
                            if (p != null) pId = p.MaSanPham;
                        }

                        if (pId > 0)
                        {
                            var product = _context.SanPhams.Find(pId);
                            if (product != null)
                            {
                                decimal cost = product.GiaNhap;
                                if (tx.Note != null && tx.Note.Contains("Đơn giá nhập:"))
                                {
                                    var match = System.Text.RegularExpressions.Regex.Match(tx.Note, @"Đơn giá nhập:\s*([\d\.,]+)");
                                    if (match.Success)
                                    {
                                        var numStr = match.Groups[1].Value.Replace(".", "").Replace(",", "");
                                        if (decimal.TryParse(numStr, out var parsedCost) && parsedCost > 0)
                                        {
                                            cost = parsedCost;
                                        }
                                    }
                                }

                                var txDate = tx.NgayDuyet ?? tx.Date;
                                var minDate = txDate.AddMinutes(-2);
                                var maxDate = txDate.AddMinutes(2);
                                var exists = _context.ChiTietPhieuNhaps
                                    .Any(c => c.MaSanPham == pId && c.SoLuong == tx.QuantityChange && c.PhieuNhap != null && c.PhieuNhap.NgayNhap >= minDate && c.PhieuNhap.NgayNhap <= maxDate);

                                if (!exists)
                                {
                                    var pn = new PhieuNhap
                                    {
                                        MaNCC = product.MaNCC > 0 ? product.MaNCC : 1,
                                        MaNhanVien = 1,
                                        NgayNhap = txDate
                                    };
                                    _context.PhieuNhaps.Add(pn);

                                    var ctpn = new ChiTietPhieuNhap
                                    {
                                        PhieuNhap = pn,
                                        MaSanPham = pId,
                                        SoLuong = tx.QuantityChange,
                                        GiaNhap = cost
                                    };
                                    _context.ChiTietPhieuNhaps.Add(ctpn);
                                    hasNewSync = true;
                                }
                            }
                        }
                    }
                    if (hasNewSync)
                    {
                        _context.SaveChanges();
                    }
                }

                _hasRefreshedFluctuationSeed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in EnsureImportHistorySeeded: " + ex.Message);
            }
        }
    }
}
