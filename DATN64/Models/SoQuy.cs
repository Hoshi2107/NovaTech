using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("SoQuy")]
    public class SoQuy
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string MaGiaoDich { get; set; } = ""; // PT-0001, PC-0001

        [Required]
        [StringLength(10)]
        public string LoaiGiaoDich { get; set; } = "Thu"; // Thu / Chi

        [Required]
        [StringLength(100)]
        public string NhomGiaoDich { get; set; } = "Bán hàng"; // Bán hàng, Trả nợ NCC, Trả lương, Vận hành, Khác

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; }

        public DateTime NgayGiaoDich { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string PhuongThucThanhToan { get; set; } = "Chuyển khoản"; // Tiền mặt / Chuyển khoản

        [StringLength(255)]
        public string? DoiTuongGiaoDich { get; set; } // Tên khách mua hoặc NCC nhận tiền

        public string? GhiChu { get; set; }

        public int MaNhanVien { get; set; } // Người lập phiếu

        public int? MaDonHang { get; set; } // Chỉ điền khi LoaiGiaoDich = "Thu" từ đơn hàng

        [ForeignKey("MaNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }

        [ForeignKey("MaDonHang")]
        public virtual DonHang? DonHang { get; set; }
    }
}
