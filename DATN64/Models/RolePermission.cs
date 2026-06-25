using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("RolePermission")]
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string PermissionName { get; set; } = "";
    }
}
