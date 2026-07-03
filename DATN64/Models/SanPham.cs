using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("SanPham")]
    public class SanPham
    {
        [Key]
        public int MaSanPham { get; set; }

        [Required]
        [StringLength(255)]
        public string TenSanPham { get; set; } = "";

        [StringLength(100)]
        public string? SKU { get; set; }

        public int MaDanhMuc { get; set; }
        public int MaThuongHieu { get; set; }
        public int MaNCC { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaNhap { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaBan { get; set; }

        public int SoLuongTon { get; set; } = 0;

        public string? MoTa { get; set; }

        [StringLength(500)]
        public string? HinhAnh { get; set; }

        [StringLength(50)]
        public string? TrangThai { get; set; }

        [ForeignKey("MaDanhMuc")]
        public virtual DanhMuc? DanhMuc { get; set; }

        [ForeignKey("MaThuongHieu")]
        public virtual ThuongHieu? ThuongHieu { get; set; }

        [ForeignKey("MaNCC")]
        public virtual NhaCungCap? NhaCungCap { get; set; }
    }
}
