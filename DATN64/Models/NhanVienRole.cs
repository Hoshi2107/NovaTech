using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("NhanVienRole")]
    public class NhanVienRole
    {
        [Key]
        public int Id { get; set; }

        public int MaNhanVien { get; set; }

        public int RoleId { get; set; }
    }
}
