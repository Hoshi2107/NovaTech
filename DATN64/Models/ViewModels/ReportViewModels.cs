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
        public int     TongKhachHang      { get; set; }   // Tổng KH (không có NgayTao nên không tính "mới")

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
        public int     SoLuongDuHang      { get; set; }   // SoLuongTon > 5
        public int     SoLuongSapHet      { get; set; }   // 0 < SoLuongTon <= 5
        public int     SoLuongHetHang     { get; set; }   // SoLuongTon == 0
        public decimal TongGiaTriTonKho   { get; set; }   // SUM(SoLuongTon * GiaNhap)

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
        public decimal GiaTriVon  { get; set; }  // SoLuongTon * GiaNhap
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
}
