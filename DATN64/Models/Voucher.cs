using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("Voucher")]
    public class Voucher
    {
        [Key]
        public int MaVoucher { get; set; }

        [StringLength(50)]
        public string? MaCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GiaTri { get; set; }

        public int? SoLuong { get; set; }

        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
    }
}
