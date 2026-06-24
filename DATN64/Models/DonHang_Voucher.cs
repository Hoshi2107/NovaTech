using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("DonHang_Voucher")]
    public class DonHang_Voucher
    {
        [Key, Column(Order = 0)]
        public int MaDonHang { get; set; }

        [Key, Column(Order = 1)]
        public int MaVoucher { get; set; }

        [ForeignKey("MaDonHang")]
        public virtual DonHang? DonHang { get; set; }

        [ForeignKey("MaVoucher")]
        public virtual Voucher? Voucher { get; set; }
    }
}
