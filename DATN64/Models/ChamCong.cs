using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("ChamCong")]
    public class ChamCong
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaNhanVien { get; set; }

        [Required]
        public DateTime NgayCham { get; set; } = DateTime.Today;

        public DateTime? GioVao { get; set; }

        public DateTime? GioRa { get; set; }

        public double? TongGioLam { get; set; }

        [StringLength(255)]
        public string? GhiChu { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; } = "Dang lam";
    }
}