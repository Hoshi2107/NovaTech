using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("KhachHang")]
    public class KhachHang
    {
        [Key]
        public int MaKhachHang { get; set; }

        [Required]
        [StringLength(150)]
        public string HoTen { get; set; } = "";

        [StringLength(20)]
        public string? SoDienThoai { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? DiaChi { get; set; }

        [StringLength(255)]
        public string? MatKhau { get; set; }

        public int DiemTichLuy { get; set; } = 0;
    }
}
