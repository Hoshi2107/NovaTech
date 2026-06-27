using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("CongNoNCC")]
    public class CongNoNCC
    {
        [Key]
        public int Id { get; set; }

        public int MaNCC { get; set; }

        public int MaPhieuNhap { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DaThanhToan { get; set; } = 0;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal ConNo { get; private set; } // SQL Server tự tính toán

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? HanThanhToan { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = "ChuaThanhToan"; // ChuaThanhToan, ThanhToanMotPhan, DaHoanTat

        [ForeignKey("MaNCC")]
        public virtual NhaCungCap? NhaCungCap { get; set; }

        [ForeignKey("MaPhieuNhap")]
        public virtual PhieuNhap? PhieuNhap { get; set; }
    }
}
