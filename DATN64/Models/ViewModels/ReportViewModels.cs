using System;
using System.Collections.Generic;

namespace DATN64.Models.ViewModels
{
    // ──────────────────────────────────────────────────────
    //  DASHBOARD
    // ──────────────────────────────────────────────────────
    public class ReportDashboardViewModel
    {
        public decimal DoanhThuHomNay     { get; set; }
        public decimal DoanhThuThang      { get; set; }
        public int     TongDonHang        { get; set; }
        public int     DonHoanThanh       { get; set; }
        public int     DonHuy             { get; set; }
        public int     TongKhachHang      { get; set; }

        public List<ProductLowStockDto> LowStockProducts { get; set; } = new();
        public List<RevenueChartDto>    Revenue7Days     { get; set; } = new();
        public List<TopProductDto>      TopProducts      { get; set; } = new();
    }

    public class ProductLowStockDto
    {
        public string TenSanPham  { get; set; } = "";
        public int    SoLuongTon  { get; set; }
        public string TrangThai   { get; set; } = "";
    }

    public class RevenueChartDto
    {
        public string  NgayLabel { get; set; } = "";
        public decimal DoanhThu  { get; set; }
    }

    public class TopProductDto
    {
        public string  TenSanPham       { get; set; } = "";
        public int     TongSoLuongBan   { get; set; }
        public decimal DoanhThu         { get; set; }
    }

    // ──────────────────────────────────────────────────────
    //  BÁO CÁO BÁN HÀNG
    // ──────────────────────────────────────────────────────
    public class SaleReportViewModel
    {
        public decimal TongDoanhThu      { get; set; }
        public int     TongDonHang       { get; set; }
        public decimal TrungBinhDonHang  { get; set; }

        public DateTime? TuNgay          { get; set; }
        public DateTime? DenNgay         { get; set; }
        public string?   TrangThaiFilter { get; set; }

        public List<SaleReportRowDto> Rows { get; set; } = new();
    }

    public class SaleReportRowDto
    {
        public int      MaDonHang    { get; set; }
        public DateTime? NgayDat     { get; set; }
        public string   TenKhachHang { get; set; } = "Khách lẻ";
        public decimal  TongTien     { get; set; }
        public string   TrangThai    { get; set; } = "";
        public string   PhuongThuc   { get; set; } = "";
    }

    // ──────────────────────────────────────────────────────
    //  BÁO CÁO KHO
    // ──────────────────────────────────────────────────────
    public class InventoryReportViewModel
    {
        public int     TongSKU            { get; set; }
        public int     SoLuongDuHang      { get; set; }
        public int     SoLuongSapHet      { get; set; }
        public int     SoLuongHetHang     { get; set; }
        public decimal TongGiaTriTonKho   { get; set; }

        public List<InventoryItemDto>   Items         { get; set; } = new();
        public List<ImportHistoryDto>   RecentImports { get; set; } = new();
    }

    public class InventoryItemDto
    {
        public int     MaSanPham  { get; set; }
        public string  TenSanPham { get; set; } = "";
        public int     SoLuongTon { get; set; }
        public decimal GiaNhap    { get; set; }
        public decimal GiaBan     { get; set; }
        public decimal GiaTriVon  { get; set; }
        public string  TrangThai  { get; set; } = "";
    }

    public class ImportHistoryDto
    {
        public int      MaPhieuNhap { get; set; }
        public DateTime? NgayNhap   { get; set; }
        public string   NhaCungCap  { get; set; } = "";
        public int      TongSoLuong { get; set; }
        public decimal  TongGiaTri  { get; set; }
    }

    // ──────────────────────────────────────────────────────
    //  BÁO CÁO BIẾN ĐỘNG GIÁ NHẬP KHO THEO THỜI GIAN
    // ──────────────────────────────────────────────────────
    public class ImportPriceHistoryViewModel
    {
        public int? SelectedProductId { get; set; }
        public int? SelectedSupplierId { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string? Search { get; set; }

        public int TongSoLanNhap { get; set; }
        public int TongSoLuongNhap { get; set; }
        public decimal TongGiaTriNhap { get; set; }
        public decimal GiaNhapHienTai { get; set; }
        public decimal GiaNhapCaoNhat { get; set; }
        public decimal GiaNhapThapNhat { get; set; }
        public decimal GiaNhapTrungBinh { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TongSoLanNhap / (PageSize > 0 ? PageSize : 10)));

        public List<ImportPriceHistoryRowDto> Rows { get; set; } = new();
        public List<ImportPriceHistoryRowDto> PagedRows { get; set; } = new();
        public List<PriceChartPointDto> ChartData { get; set; } = new();
        public List<SanPham> ProductsList { get; set; } = new();
        public List<NhaCungCap> SuppliersList { get; set; } = new();
    }

    public class ImportPriceHistoryRowDto
    {
        public int MaPhieuNhap { get; set; }
        public DateTime? NgayNhap { get; set; }
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; } = "";
        public string SKU { get; set; } = "";
        public string HinhAnh { get; set; } = "";
        public string TenNCC { get; set; } = "";
        public int SoLuongNhap { get; set; }
        public decimal GiaNhap { get; set; }
        public decimal GiaBan { get; set; }
        public decimal ThanhTien { get; set; }
        public decimal BienDongGiaVND { get; set; }
        public double BienDongGiaPercent { get; set; }
        public string NguoiNhap { get; set; } = "";
    }

    public class PriceChartPointDto
    {
        public string NgayLabel { get; set; } = "";
        public string TenSanPham { get; set; } = "";
        public decimal GiaNhap { get; set; }
        public int SoLuong { get; set; }
        public decimal TongTien { get; set; }
    }
}
