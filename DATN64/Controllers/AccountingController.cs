using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using DATN64.Models.ViewModels;
using DATN64.Helpers;

namespace DATN64.Controllers
{
    [HasPermission("View_Accounting")]
    public class AccountingController : Controller
    {
        private readonly AppDbContext _context;

        public AccountingController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Accounting
        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
        {
            // 1. Chạy cơ chế Tự động đồng bộ trước để đảm bảo số liệu kế toán mới nhất
            await AutoSyncDataAsync();

            var today = DateTime.Today;
            var defaultTuNgay = tuNgay ?? new DateTime(today.Year, today.Month, 1);
            var defaultDenNgay = denNgay ?? today.AddDays(1).AddSeconds(-1);

            var vm = new AccountingDashboardViewModel
            {
                TuNgay = defaultTuNgay,
                DenNgay = defaultDenNgay
            };

            // 2. Lấy danh sách giao dịch Sổ Quỹ
            var queryGiaoDich = _context.SoQuys
                .Include(s => s.NhanVien)
                .Include(s => s.DonHang)
                .AsQueryable();

            if (tuNgay.HasValue)
            {
                queryGiaoDich = queryGiaoDich.Where(s => s.NgayGiaoDich >= tuNgay.Value);
            }
            if (denNgay.HasValue)
            {
                // Đến hết ngày được chọn
                var denNgayEnd = denNgay.Value.Date.AddDays(1).AddSeconds(-1);
                queryGiaoDich = queryGiaoDich.Where(s => s.NgayGiaoDich <= denNgayEnd);
            }

            vm.GiaoDichs = await queryGiaoDich
                .OrderByDescending(s => s.NgayGiaoDich)
                .ToListAsync();

            // Tổng hợp tiền Thu / Chi / Số Dư (bỏ phiếu đã hủy)
            var allSoQuys = await _context.SoQuys.ToListAsync();
            vm.TongThu = allSoQuys.Where(s => s.LoaiGiaoDich == "Thu" && s.TrangThai != "Đã hủy").Sum(s => s.SoTien);
            vm.TongChi = allSoQuys.Where(s => s.LoaiGiaoDich == "Chi" && s.TrangThai != "Đã hủy").Sum(s => s.SoTien);
            vm.SoDuQuy = vm.TongThu - vm.TongChi;

            // 3. Lấy thông tin Công nợ Nhà cung cấp
            // Nhóm theo NCC để ra báo cáo tổng hợp
            var congNos = await _context.CongNoNCCs
                .Include(c => c.NhaCungCap)
                .Include(c => c.PhieuNhap)
                .ToListAsync();

            vm.CongNoSummaries = congNos
                .GroupBy(c => c.MaNCC)
                .Select(g => {
                    var ncc = g.First().NhaCungCap;
                    return new CongNoNccSummaryDto
                    {
                        MaNCC = g.Key,
                        TenNCC = ncc?.TenNCC ?? $"Nhà cung cấp #{g.Key}",
                        SoDienThoai = ncc?.SoDienThoai ?? "",
                        TongNo = g.Sum(x => x.TongTien),
                        DaThanhToan = g.Sum(x => x.DaThanhToan),
                        ConNo = g.Sum(x => x.TongTien - x.DaThanhToan),
                        SoPhieuNoActive = g.Count(x => x.TrangThai != "DaHoanTat")
                    };
                })
                .Where(x => x.TongNo > 0)
                .ToList();

            vm.TongNoNCC = vm.CongNoSummaries.Sum(x => x.ConNo);

            // Chi tiết các phiếu nợ
            vm.ChiTietCongNos = congNos
                .Where(c => c.TrangThai != "DaHoanTat")
                .OrderByDescending(c => c.NgayTao)
                .ToList();

            // 4. Tính toán Báo cáo kết quả kinh doanh (P&L) trong khoảng thời gian lọc
            var filterTu = defaultTuNgay;
            var filterDen = defaultDenNgay;

            // A. Doanh thu thuần (Đơn hàng hoàn thành - tất cả kênh)
            var completedOrdersForPL = await _context.DonHangs
                .Include(d => d.KhachHang)
                .Where(d => d.TrangThai == "Hoàn thành" && d.NgayDat >= filterTu && d.NgayDat <= filterDen)
                .ToListAsync();

            vm.ReportPL.DoanhThuThuan = completedOrdersForPL.Sum(d => d.TongTien ?? 0);

            // Phân tách doanh thu theo kênh
            var tikTokOrders = completedOrdersForPL.Where(d => DetermineChannel(d) == "TikTok Shop").ToList();
            var banLeOrders = completedOrdersForPL.Where(d => DetermineChannel(d) == "Bán lẻ tại quầy").ToList();
            var onlineOrders = completedOrdersForPL.Where(d => DetermineChannel(d) == "Online / Web").ToList();

            vm.ReportPL.DoanhThuTikTok = tikTokOrders.Sum(d => d.TongTien ?? 0);
            vm.ReportPL.DoanhThuOnline = onlineOrders.Sum(d => d.TongTien ?? 0);
            vm.ReportPL.DoanhThuBanLe = banLeOrders.Sum(d => d.TongTien ?? 0);

            // Danh sách kênh cho biểu đồ
            vm.RevenueByChannel = new List<RevenueChannelDto>
            {
                new RevenueChannelDto { TenKenh = "TikTok Shop", DoanhThu = vm.ReportPL.DoanhThuTikTok, SoDonHang = tikTokOrders.Count, MauSac = "#ff2d55", Icon = "fa-brands fa-tiktok" },
                new RevenueChannelDto { TenKenh = "Bán lẻ tại quầy", DoanhThu = vm.ReportPL.DoanhThuBanLe, SoDonHang = banLeOrders.Count, MauSac = "#0ea5e9", Icon = "fa-solid fa-store" },
                new RevenueChannelDto { TenKenh = "Online / Web", DoanhThu = vm.ReportPL.DoanhThuOnline, SoDonHang = onlineOrders.Count, MauSac = "#8b5cf6", Icon = "fa-solid fa-globe" },
            };

            // B. Giá vốn hàng bán (COGS)
            var cogsQuery = from ctdh in _context.ChiTietDonHangs
                            join dh in _context.DonHangs on ctdh.MaDonHang equals dh.MaDonHang
                            join sp in _context.SanPhams on ctdh.MaSanPham equals sp.MaSanPham
                            where dh.TrangThai == "Hoàn thành" && dh.NgayDat >= filterTu && dh.NgayDat <= filterDen
                            select ctdh.SoLuong * sp.GiaNhap;
            vm.ReportPL.GiaVonHangBan = await cogsQuery.SumAsync();

            vm.ReportPL.LoiNhuanGop = vm.ReportPL.DoanhThuThuan - vm.ReportPL.GiaVonHangBan;

            // C. Chi phí nhân sự (Chấm công)
            var timekeepings = await _context.ChamCongs
                .Where(cc => cc.TrangThai == "Hoàn thành" && cc.NgayCham >= filterTu && cc.NgayCham <= filterDen)
                .ToListAsync();
            var nhanViens = await _context.NhanViens.ToListAsync();

            decimal chiPhiLuong = 0;
            foreach (var cc in timekeepings)
            {
                var nv = nhanViens.FirstOrDefault(e => e.MaNhanVien == cc.MaNhanVien);
                if (nv != null && nv.LuongTheoGio.HasValue)
                {
                    chiPhiLuong += (decimal)(cc.TongGioLam ?? 0) * nv.LuongTheoGio.Value;
                }
            }
            vm.ReportPL.ChiPhiLuong = chiPhiLuong;

            // D. Chi phí vận hành khác (Các khoản chi trong sổ quỹ ngoài lương)
            vm.ReportPL.ChiPhiVanhHanhKhac = await _context.SoQuys
                .Where(s => s.LoaiGiaoDich == "Chi" 
                         && s.NhomGiaoDich != "Trả lương"
                         && s.NgayGiaoDich >= filterTu && s.NgayGiaoDich <= filterDen)
                .SumAsync(s => s.SoTien);

            vm.ReportPL.TongChiPhi = vm.ReportPL.ChiPhiLuong + vm.ReportPL.ChiPhiVanhHanhKhac;
            vm.ReportPL.LoiNhuanRong = vm.ReportPL.LoiNhuanGop - vm.ReportPL.TongChiPhi;

            // 5. Chuẩn bị dữ liệu biểu đồ dòng tiền (7 ngày gần nhất)
            var last7Days = DateTime.Today.AddDays(-6);
            var last7DaysList = Enumerable.Range(0, 7)
                .Select(i => last7Days.AddDays(i))
                .ToList();

            var chartData = new List<CashFlowChartDto>();
            foreach (var day in last7DaysList)
            {
                var dayStart = day.Date;
                var dayEnd = day.Date.AddDays(1).AddSeconds(-1);

                var thuTrongNgay = allSoQuys
                    .Where(s => s.LoaiGiaoDich == "Thu" && s.TrangThai != "Đã hủy" && s.NgayGiaoDich >= dayStart && s.NgayGiaoDich <= dayEnd)
                    .Sum(s => s.SoTien);

                var chiTrongNgay = allSoQuys
                    .Where(s => s.LoaiGiaoDich == "Chi" && s.TrangThai != "Đã hủy" && s.NgayGiaoDich >= dayStart && s.NgayGiaoDich <= dayEnd)
                    .Sum(s => s.SoTien);

                chartData.Add(new CashFlowChartDto
                {
                    NgayLabel = day.ToString("dd/MM"),
                    Thu = thuTrongNgay,
                    Chi = chiTrongNgay
                });
            }
            vm.CashFlowChartData = chartData;

            // 6. Biểu đồ P&L theo tháng (12 tháng gần nhất)
            var allCompletedOrders = await _context.DonHangs
                .Where(d => d.TrangThai == "Hoàn thành")
                .ToListAsync();
            var allCogs = await (from ctdh in _context.ChiTietDonHangs
                                 join dh in _context.DonHangs on ctdh.MaDonHang equals dh.MaDonHang
                                 join sp in _context.SanPhams on ctdh.MaSanPham equals sp.MaSanPham
                                 where dh.TrangThai == "Hoàn thành"
                                 select new { dh.NgayDat, Cogs = ctdh.SoLuong * sp.GiaNhap }).ToListAsync();

            var monthlyPL = new List<MonthlyPLChartDto>();
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);

                var rev = allCompletedOrders
                    .Where(d => d.NgayDat >= monthStart && d.NgayDat <= monthEnd)
                    .Sum(d => d.TongTien ?? 0);

                var cogs = allCogs
                    .Where(x => x.NgayDat >= monthStart && x.NgayDat <= monthEnd)
                    .Sum(x => x.Cogs);

                monthlyPL.Add(new MonthlyPLChartDto
                {
                    ThangLabel = $"T{monthStart.Month}/{monthStart.Year}",
                    DoanhThu = rev,
                    GiaVon = cogs,
                    LoiNhuan = rev - cogs
                });
            }
            vm.MonthlyPLChartData = monthlyPL;

            // Lấy danh sách Nhà cung cấp để hiển thị form trả nợ
            ViewBag.NhaCungCaps = await _context.NhaCungCaps.ToListAsync();

            return View(vm);
        }

        // POST: Accounting/CreateTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransaction(string loaiGiaoDich, string nhomGiaoDich, decimal soTien, string phuongThucThanhToan, string doiTuongGiaoDich, string? ghiChu)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.Email == userEmail);
            if (nhanVien == null)
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để lập phiếu!";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            if (soTien <= 0)
            {
                TempData["ToastMessage"] = "Số tiền giao dịch phải lớn hơn 0 đ.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            // Tạo mã giao dịch tự động
            string prefix = loaiGiaoDich == "Thu" ? "PT" : "PC";
            int count = await _context.SoQuys.CountAsync(s => s.LoaiGiaoDich == loaiGiaoDich) + 1;
            string maGiaoDich = $"{prefix}-{count:D5}";

            var soQuy = new SoQuy
            {
                MaGiaoDich = maGiaoDich,
                LoaiGiaoDich = loaiGiaoDich,
                NhomGiaoDich = nhomGiaoDich,
                SoTien = soTien,
                PhuongThucThanhToan = phuongThucThanhToan,
                DoiTuongGiaoDich = doiTuongGiaoDich,
                GhiChu = ghiChu,
                MaNhanVien = nhanVien.MaNhanVien,
                NgayGiaoDich = DateTime.Now
            };

            _context.SoQuys.Add(soQuy);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = $"Đã lập thành công phiếu {loaiGiaoDich.ToLower()} {maGiaoDich} trị giá {soTien:N0} đ.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // POST: Accounting/VoidTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoidTransaction(int id, string lyDoHuy)
        {
            var soQuy = await _context.SoQuys.FindAsync(id);
            if (soQuy == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy phiếu giao dịch!";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            if (soQuy.TrangThai == "Đã hủy")
            {
                TempData["ToastMessage"] = "Phiếu này đã được hủy trước đó rồi!";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            soQuy.TrangThai = "Đã hủy";
            soQuy.GhiChu = $"[HỦY PHIẾU - {DateTime.Now:dd/MM/yyyy HH:mm}] {lyDoHuy}. {soQuy.GhiChu}";

            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = $"Đã void thành công phiếu {soQuy.MaGiaoDich}. Số dư quỹ đã được điều chỉnh.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // POST: Accounting/PaySupplierDebt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaySupplierDebt(int congNoId, decimal paymentAmount, string phuongThucThanhToan, string? ghiChu)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.Email == userEmail);
            if (nhanVien == null)
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập trước khi thực hiện thanh toán!";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            var congNo = await _context.CongNoNCCs
                .Include(c => c.NhaCungCap)
                .FirstOrDefaultAsync(c => c.Id == congNoId);

            if (congNo == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy khoản công nợ cần trả!";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            decimal maximumPayable = congNo.TongTien - congNo.DaThanhToan;
            if (paymentAmount <= 0 || paymentAmount > maximumPayable)
            {
                TempData["ToastMessage"] = $"Số tiền thanh toán không hợp lệ! Tối đa có thể trả: {maximumPayable:N0} đ.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            // 1. Cập nhật số tiền đã trả trên hóa đơn công nợ
            congNo.DaThanhToan += paymentAmount;
            if (congNo.DaThanhToan >= congNo.TongTien)
            {
                congNo.TrangThai = "DaHoanTat";
            }
            else
            {
                congNo.TrangThai = "ThanhToanMotPhan";
            }

            // 2. Tạo phiếu chi tiền mặt / chuyển khoản tương ứng trong Sổ Quỹ
            int count = await _context.SoQuys.CountAsync(s => s.LoaiGiaoDich == "Chi") + 1;
            string maGiaoDich = $"PC-{count:D5}";

            var phieuChi = new SoQuy
            {
                MaGiaoDich = maGiaoDich,
                LoaiGiaoDich = "Chi",
                NhomGiaoDich = "Trả nợ NCC",
                SoTien = paymentAmount,
                PhuongThucThanhToan = phuongThucThanhToan,
                DoiTuongGiaoDich = congNo.NhaCungCap?.TenNCC ?? "Nhà cung cấp",
                GhiChu = string.IsNullOrEmpty(ghiChu) ? $"Trả nợ hóa đơn nhập kho #{congNo.MaPhieuNhap}" : ghiChu,
                MaNhanVien = nhanVien.MaNhanVien,
                NgayGiaoDich = DateTime.Now,
                CongNoId = congNo.Id
            };

            _context.SoQuys.Add(phieuChi);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = $"Đã chi trả {paymentAmount:N0} đ cho nhà cung cấp {congNo.NhaCungCap?.TenNCC}. Tạo phiếu chi {maGiaoDich}.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // GET: Accounting/GetCongNoDetail/5
        [HttpGet]
        public async Task<IActionResult> GetCongNoDetail(int id)
        {
            var congNo = await _context.CongNoNCCs
                .Include(c => c.NhaCungCap)
                .Include(c => c.PhieuNhap)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (congNo == null)
                return NotFound();

            // Lấy chi tiết hàng hóa trong phiếu nhập
            var chiTiets = await _context.ChiTietPhieuNhaps
                .Include(ct => ct.SanPham)
                .Where(ct => ct.MaPhieuNhap == congNo.MaPhieuNhap)
                .ToListAsync();

            // Lịch sử thanh toán từ Sổ Quỹ liên quan đến công nợ này
            var lichSuThanhToan = await _context.SoQuys
                .Where(s => s.CongNoId == congNo.Id && s.TrangThai != "Đã hủy")
                .OrderByDescending(s => s.NgayGiaoDich)
                .Select(s => new {
                    s.MaGiaoDich,
                    NgayThanhToan = s.NgayGiaoDich.ToString("dd/MM/yyyy HH:mm"),
                    s.SoTien,
                    s.PhuongThucThanhToan,
                    s.GhiChu
                })
                .ToListAsync();

            return Json(new
            {
                id = congNo.Id,
                tenNCC = congNo.NhaCungCap?.TenNCC ?? $"NCC #{congNo.MaNCC}",
                soDienThoai = congNo.NhaCungCap?.SoDienThoai ?? "",
                maPhieuNhap = $"PN-{congNo.MaPhieuNhap:D5}",
                ngayNhap = congNo.NgayTao.ToString("dd/MM/yyyy"),
                hanThanhToan = congNo.HanThanhToan?.ToString("dd/MM/yyyy") ?? "—",
                quaHan = congNo.HanThanhToan.HasValue && congNo.HanThanhToan < DateTime.Today,
                tongTien = congNo.TongTien,
                daThanhToan = congNo.DaThanhToan,
                conNo = congNo.TongTien - congNo.DaThanhToan,
                trangThai = congNo.TrangThai,
                chiTietHang = chiTiets.Select(ct => new
                {
                    tenSanPham = ct.SanPham?.TenSanPham ?? $"SP #{ct.MaSanPham}",
                    soLuong = ct.SoLuong,
                    giaNhap = ct.GiaNhap,
                    thanhTien = ct.SoLuong * ct.GiaNhap
                }),
                lichSuThanhToan
            });
        }

        // GET: Accounting/GetSoQuyDetail/5
        [HttpGet]
        public async Task<IActionResult> GetSoQuyDetail(int id)
        {
            var soQuy = await _context.SoQuys
                .Include(s => s.NhanVien)
                .Include(s => s.DonHang)
                    .ThenInclude(d => d != null ? d.KhachHang : null)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (soQuy == null)
                return NotFound();

            string? tenKhachHang = null;
            if (soQuy.DonHang?.KhachHang != null)
                tenKhachHang = soQuy.DonHang.KhachHang.HoTen;

            return Json(new
            {
                id = soQuy.Id,
                maGiaoDich = soQuy.MaGiaoDich,
                loaiGiaoDich = soQuy.LoaiGiaoDich,
                nhomGiaoDich = soQuy.NhomGiaoDich,
                soTien = soQuy.SoTien,
                ngayGiaoDich = soQuy.NgayGiaoDich.ToString("dd/MM/yyyy HH:mm:ss"),
                phuongThucThanhToan = soQuy.PhuongThucThanhToan,
                doiTuongGiaoDich = soQuy.DoiTuongGiaoDich,
                ghiChu = soQuy.GhiChu,
                nguoiLap = soQuy.NhanVien?.HoTen ?? "—",
                trangThai = soQuy.TrangThai,
                maDonHang = soQuy.MaDonHang,
                tenKhachHang
            });
        }

        // 6. Cơ chế Tự động đồng bộ hóa doanh thu và nhập kho gối đầu vào Kế toán
        private async Task AutoSyncDataAsync()
        {
            // A. Đồng bộ Đơn Hàng Hoàn Thành vào Sổ Quỹ
            var completedOrders = await _context.DonHangs
                .Include(d => d.KhachHang)
                .Where(d => d.TrangThai == "Hoàn thành")
                .ToListAsync();

            var syncedOrderIds = await _context.SoQuys
                .Where(s => s.MaDonHang.HasValue)
                .Select(s => s.MaDonHang!.Value)
                .ToListAsync();

            var newOrdersToSync = completedOrders
                .Where(d => !syncedOrderIds.Contains(d.MaDonHang))
                .ToList();

            if (newOrdersToSync.Any())
            {
                int count = await _context.SoQuys.CountAsync(s => s.LoaiGiaoDich == "Thu");
                foreach (var order in newOrdersToSync)
                {
                    count++;
                    var name = order.KhachHang?.HoTen ?? "Khách lẻ";
                    // Phân loại kênh bán hàng dựa vào dấu hiệu trong GhiChu / PhuongThucThanhToan
                    var channel = DetermineChannel(order);
                    string nhom = channel == "TikTok Shop" ? "TikTok Shop" : (channel == "Online / Web" ? "Doanh thu Web" : "Bán hàng");
                    var sq = new SoQuy
                    {
                        MaGiaoDich = $"PT-{count:D5}",
                        LoaiGiaoDich = "Thu",
                        NhomGiaoDich = nhom,
                        SoTien = order.TongTien ?? 0,
                        NgayGiaoDich = order.NgayDat ?? DateTime.Now,
                        PhuongThucThanhToan = order.PhuongThucThanhToan ?? "Chuyển khoản",
                        DoiTuongGiaoDich = name,
                        GhiChu = channel == "TikTok Shop"
                            ? $"Thu tiền đơn TikTok Shop #{order.MaDonHang}"
                            : (channel == "Online / Web" ? $"Thu tiền đơn hàng Web #{order.MaDonHang}" : $"Thu tiền đơn hàng #{order.MaDonHang} từ hệ thống bán hàng"),
                        MaNhanVien = order.MaNhanVien ?? 1,
                        MaDonHang = order.MaDonHang
                    };
                    _context.SoQuys.Add(sq);
                }
                await _context.SaveChangesAsync();
            }

            // B. Đồng bộ Phiếu Nhập Kho đã duyệt từ InventoryTransaction vào Công nợ NCC
            // Lấy các giao dịch nhập kho đã được phê duyệt thành công
            var approvedImports = await _context.InventoryTransactions
                .Where(t => t.Type == "Nhập kho" && t.TrangThai == "Đã duyệt")
                .ToListAsync();

            // Lấy danh sách các giao dịch nhập kho đã được đồng bộ thông qua mô tả / ghi chú của phiếu nhập hoặc mapping
            // Ở đây, để đơn giản, ta kiểm tra xem đã có PhieuNhap nào có cùng thời điểm và thông tin chưa.
            // Để chắc chắn và tránh trùng lặp, ta sẽ lưu thông tin mapping hoặc so khớp. 
            // Ở đây, vì database hiện tại không có cột TransactionId trong PhieuNhap, ta sẽ kiểm tra xem có PhieuNhap nào có ngày nhập trùng khít với giao dịch kho hay không.
            var existingPhieuNhaps = await _context.PhieuNhaps.ToListAsync();
            var products = await _context.SanPhams.ToListAsync();

            foreach (var tx in approvedImports)
            {
                // Kiểm tra xem đã đồng bộ giao dịch này chưa dựa trên so khớp thời gian gần đúng (trên cùng phút) và SKU sản phẩm
                bool isSynced = existingPhieuNhaps.Any(pn => 
                    Math.Abs(((pn.NgayNhap ?? DateTime.MinValue) - tx.Date).TotalSeconds) < 5 
                    && _context.ChiTietPhieuNhaps.Any(ct => ct.MaPhieuNhap == pn.MaPhieuNhap && ct.MaSanPham.ToString() == tx.ProductSKU)
                );

                if (!isSynced)
                {
                    // Lấy sản phẩm để biết đơn giá nhập và nhà cung cấp
                    int prodId = 0;
                    if (int.TryParse(tx.ProductSKU, out prodId))
                    {
                        var product = products.FirstOrDefault(p => p.MaSanPham == prodId);
                        if (product != null)
                        {
                            // Tạo PhieuNhap gối đầu
                            var pn = new PhieuNhap
                            {
                                MaNCC = product.MaNCC,
                                MaNhanVien = 1, // Admin mặc định phê duyệt hệ thống
                                NgayNhap = tx.Date
                            };

                            _context.PhieuNhaps.Add(pn);
                            await _context.SaveChangesAsync(); // Lưu để lấy MaPhieuNhap

                            // Tạo ChiTietPhieuNhap
                            var ctpn = new ChiTietPhieuNhap
                            {
                                MaPhieuNhap = pn.MaPhieuNhap,
                                MaSanPham = product.MaSanPham,
                                SoLuong = tx.QuantityChange,
                                GiaNhap = product.GiaNhap
                            };
                            _context.ChiTietPhieuNhaps.Add(ctpn);

                            // Tạo Công nợ Nhà cung cấp tương ứng
                            var congNo = new CongNoNCC
                            {
                                MaNCC = product.MaNCC,
                                MaPhieuNhap = pn.MaPhieuNhap,
                                TongTien = tx.QuantityChange * product.GiaNhap,
                                DaThanhToan = 0,
                                NgayTao = tx.Date,
                                HanThanhToan = tx.Date.AddDays(30), // Hạn thanh toán mặc định là 30 ngày
                                TrangThai = "ChuaThanhToan"
                            };
                            _context.CongNoNCCs.Add(congNo);

                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
        }

        private string DetermineChannel(DonHang order)
        {
            var note = order.GhiChu ?? "";
            var payment = order.PhuongThucThanhToan ?? "";
            var address = order.KhachHang?.DiaChi ?? "";

            // ---- TikTok Shop detection ----
            if (note.Contains("[TikTokShop#", StringComparison.OrdinalIgnoreCase) || 
                note.Contains("[TikTok", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("TikTok", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("tiktok", StringComparison.OrdinalIgnoreCase))
            {
                return "TikTok Shop";
            }

            // ---- POS / Counter Sales detection ----
            if (note.Contains("POS", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("tại quầy", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("cửa hàng", StringComparison.OrdinalIgnoreCase))
            {
                return "Bán lẻ tại quầy";
            }

            // ---- Online / Web detection ----
            // 1. Explicit markers in note
            bool hasOnlineNote = 
                note.Contains("Địa chỉ giao hàng", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("Voucher sử dụng", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("giao hàng", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("website", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("online", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("web", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("đặt hàng online", StringComparison.OrdinalIgnoreCase) ||
                note.Contains("đơn hàng web", StringComparison.OrdinalIgnoreCase);

            // 2. Payment method markers
            bool hasOnlinePayment =
                payment.Contains("online", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("website", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("VNPay", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("MoMo", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("ZaloPay", StringComparison.OrdinalIgnoreCase) ||
                payment.Contains("COD", StringComparison.OrdinalIgnoreCase);

            // 3. Has a delivery address set (retail walk-in orders don't have shipping addresses)
            bool hasShippingAddress = !string.IsNullOrWhiteSpace(address);

            if (hasOnlineNote || hasOnlinePayment || hasShippingAddress)
            {
                return "Online / Web";
            }

            return "Bán lẻ tại quầy";
        }
    }
}
