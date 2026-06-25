using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("CustomerInboxMessage")]
    public class CustomerInboxMessage
    {
        [Key]
        public int Id { get; set; }

        public int ThreadId { get; set; }

        [ForeignKey(nameof(ThreadId))]
        public CustomerInboxThread? Thread { get; set; }

        [StringLength(50)]
        public string Sender { get; set; } = "customer";

        public string Text { get; set; } = "";

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        public bool IsAutoReply { get; set; } = false;
    }
}