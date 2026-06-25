using System;
using System.Collections.Generic;

namespace DATN64.ViewModels
{
    public class OrderDetailViewModel
    {
        public int MaDonHang { get; set; }

        public DateTime NgayDat { get; set; }

        public decimal TongTien { get; set; }

        public string TrangThai { get; set; } = "";

        public string PhuongThucThanhToan { get; set; } = "";

        public string KenhBan { get; set; } = "Cửa hàng";

        public string TenKhachHang { get; set; } = "";

        public string? SoDienThoaiKhachHang { get; set; }

        public string? EmailKhachHang { get; set; }

        public string? DiaChiKhachHang { get; set; }

        public string? TenNhanVien { get; set; }

        public List<OrderDetailItemViewModel> Items { get; set; } = new List<OrderDetailItemViewModel>();
    }

    public class OrderDetailItemViewModel
    {
        public int MaSanPham { get; set; }

        public string TenSanPham { get; set; } = "";

        public string? HinhAnh { get; set; }

        public int SoLuong { get; set; }

        public decimal DonGia { get; set; }

        public decimal ThanhTien
        {
            get
            {
                return SoLuong * DonGia;
            }
        }
    }
}