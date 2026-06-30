using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("YeuThich")]
    public class YeuThich
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaKhachHang { get; set; }

        [ForeignKey("MaKhachHang")]
        public KhachHang? KhachHang { get; set; }

        [Required]
        public int MaSanPham { get; set; }

        [ForeignKey("MaSanPham")]
        public SanPham? SanPham { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
