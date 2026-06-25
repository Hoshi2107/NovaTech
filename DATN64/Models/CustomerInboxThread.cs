using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("CustomerInboxThread")]
    public class CustomerInboxThread
    {
        [Key]
        public int Id { get; set; }

        public int CustomerId { get; set; } = 0;

        [StringLength(150)]
        public string CustomerName { get; set; } = "";

        [StringLength(20)]
        public string? CustomerPhone { get; set; }

        [StringLength(50)]
        public string Channel { get; set; } = "Store";

        [StringLength(255)]
        public string Subject { get; set; } = "Chat hỗ trợ NovaTech";

        [StringLength(50)]
        public string Status { get; set; } = "Unread";

        [StringLength(50)]
        public string Priority { get; set; } = "Medium";

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public ICollection<CustomerInboxMessage> Messages { get; set; } = new List<CustomerInboxMessage>();
    }
}