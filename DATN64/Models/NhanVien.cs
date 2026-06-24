using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("NhanVien")]
    public class NhanVien
    {
        [Key]
        public int MaNhanVien { get; set; }

        [Required]
        [StringLength(150)]
        public string HoTen { get; set; } = "";

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? SoDienThoai { get; set; }

        [Required]
        [StringLength(255)]
        public string MatKhau { get; set; } = "";

        [StringLength(50)]
        public string? VaiTro { get; set; }

        [StringLength(50)]
        public string? TrangThai { get; set; }
    }
}
