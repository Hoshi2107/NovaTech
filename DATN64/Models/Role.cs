using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("Role")]
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = "";

        [StringLength(255)]
        public string? Description { get; set; }
    }
}
