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
    }

    public class CashFlowChartDto
    {
        public string NgayLabel { get; set; } = "";
        public decimal Thu { get; set; }
        public decimal Chi { get; set; }
    }
}
