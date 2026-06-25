using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("CauHinh")]
    public class CauHinh
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string TenCuaHang { get; set; } = "NovaTech";

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? SoDienThoai { get; set; }

        [StringLength(255)]
        public string? DiaChi { get; set; }

        [StringLength(500)]
        public string? Logo { get; set; }
    }
}
