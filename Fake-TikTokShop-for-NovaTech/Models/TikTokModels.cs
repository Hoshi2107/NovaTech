using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FakeTikTokShop.Models
{
    public class TikTokOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string OrderId { get; set; } = ""; // E.g., TT-10294812
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string? Note { get; set; }
        public decimal TotalPrice { get; set; }
        public string PaymentMethod { get; set; } = "COD"; // COD, Bank Transfer
        public string Status { get; set; } = "Awaiting Shipment"; // Awaiting Shipment, Shipped, Delivered, Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string SyncStatus { get; set; } = "Pending"; // Success, Failed, Pending
        public string? WebhookErrorMessage { get; set; }
        public virtual ICollection<TikTokOrderItem> OrderItems { get; set; } = new List<TikTokOrderItem>();
    }

    public class TikTokOrderItem
    {
        [Key]
        public int Id { get; set; }
        public string OrderId { get; set; } = "";
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductSku { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }

        [ForeignKey("OrderId")]
        public virtual TikTokOrder? Order { get; set; }
    }

    public class TikTokProductCache
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string Sku { get; set; } = "";
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int Stock { get; set; }
    }

    public class WebhookLog
    {
        [Key]
        public int Id { get; set; }
        public string OrderId { get; set; } = "";
        public string ActionType { get; set; } = "Create"; // Create, UpdateStatus
        public string Payload { get; set; } = "";
        public int? HttpStatus { get; set; }
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class TikTokShopSettings
    {
        [Key]
        public int Id { get; set; }
        public string NovaTechBaseUrl { get; set; } = "http://localhost:5018";
        public bool AutoPushWebhook { get; set; } = true;
    }

    public class TikTokDbContext : DbContext
    {
        public TikTokDbContext(DbContextOptions<TikTokDbContext> options) : base(options)
        {
        }

        public DbSet<TikTokOrder> Orders { get; set; }
        public DbSet<TikTokOrderItem> OrderItems { get; set; }
        public DbSet<TikTokProductCache> ProductCaches { get; set; }
        public DbSet<WebhookLog> WebhookLogs { get; set; }
        public DbSet<TikTokShopSettings> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TikTokOrder>()
                .HasMany(o => o.OrderItems)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
