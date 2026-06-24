using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("ThuongHieu")]
    public class ThuongHieu
    {
        [Key]
        public int MaThuongHieu { get; set; }

        [Required]
        [StringLength(100)]
        public string TenThuongHieu { get; set; } = "";

        [StringLength(500)]
        public string? MoTa { get; set; }
    }
}
