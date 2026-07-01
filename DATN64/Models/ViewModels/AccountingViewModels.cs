using System;
using System.Collections.Generic;

namespace DATN64.Models.ViewModels
{
    public class AccountingDashboardViewModel
    {
        // Summary Cards
        public decimal TongThu { get; set; }
        public decimal TongChi { get; set; }
        public decimal SoDuQuy { get; set; }
        public decimal TongNoNCC { get; set; }

        // Filter parameters
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }

        // Sổ Quỹ Data
        public List<SoQuy> GiaoDichs { get; set; } = new();

        // Công nợ NCC Data
        public List<CongNoNccSummaryDto> CongNoSummaries { get; set; } = new();
        public List<CongNoNCC> ChiTietCongNos { get; set; } = new();

        // Báo cáo kết quả kinh doanh (P&L)
        public PandLReportDto ReportPL { get; set; } = new();
        
        // Chart data
        public List<CashFlowChartDto> CashFlowChartData { get; set; } = new();

        // Monthly P&L Chart (12 months)
        public List<MonthlyPLChartDto> MonthlyPLChartData { get; set; } = new();

        // Doanh thu phân theo kênh
        public List<RevenueChannelDto> RevenueByChannel { get; set; } = new();
    }

    public class CongNoNccSummaryDto
    {
        public int MaNCC { get; set; }
        public string TenNCC { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public decimal TongNo { get; set; }
        public decimal DaThanhToan { get; set; }
        public decimal ConNo { get; set; }
        public int SoPhieuNoActive { get; set; }
    }

    public class PandLReportDto
    {
        public decimal DoanhThuThuan { get; set; }
        public decimal GiaVonHangBan { get; set; }
        public decimal LoiNhuanGop { get; set; }
        public decimal ChiPhiLuong { get; set; }
        public decimal ChiPhiVanhHanhKhac { get; set; }
        public decimal TongChiPhi { get; set; }
        public decimal LoiNhuanRong { get; set; }

        // Phân tách doanh thu theo kênh bán hàng
        public decimal DoanhThuTikTok { get; set; }
        public decimal DoanhThuBanLe { get; set; }
        public decimal DoanhThuOnline { get; set; }
    }

    public class RevenueChannelDto
    {
        public string TenKenh { get; set; } = "";
        public decimal DoanhThu { get; set; }
        public int SoDonHang { get; set; }
        public string MauSac { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class CashFlowChartDto
    {
        public string NgayLabel { get; set; } = "";
        public decimal Thu { get; set; }
        public decimal Chi { get; set; }
    }

    public class MonthlyPLChartDto
    {
        public string ThangLabel { get; set; } = ""; // "T1/2025"
        public decimal DoanhThu { get; set; }
        public decimal GiaVon { get; set; }
        public decimal LoiNhuan { get; set; }
    }
}
