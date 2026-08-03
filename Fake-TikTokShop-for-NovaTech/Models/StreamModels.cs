using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FakeTikTokShop.Models
{
    [Table("Orders")]
    public class StreamOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string OrderId { get; set; } = ""; // E.g., SS-10294812
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
        public virtual ICollection<StreamOrderItem> OrderItems { get; set; } = new List<StreamOrderItem>();
    }

    [Table("OrderItems")]
    public class StreamOrderItem
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
        public virtual StreamOrder? Order { get; set; }
    }

    [Table("ProductCaches")]
    public class StreamProductCache
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

    [Table("LivestreamProducts")]
    public class StreamLivestreamProduct
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string Sku { get; set; } = "";
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int Stock { get; set; }
        public bool IsPinned { get; set; } = false;
        public int SalesCount { get; set; } = 0;
    }

    [Table("WebhookLogs")]
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

    [Table("Settings")]
    public class StreamShopSettings
    {
        [Key]
        public int Id { get; set; }
        public string NovaTechBaseUrl { get; set; } = "http://localhost:5018";
        public bool AutoPushWebhook { get; set; } = true;
    }

    public class StreamDbContext : DbContext
    {
        public StreamDbContext(DbContextOptions<StreamDbContext> options) : base(options)
        {
        }

        public DbSet<StreamOrder> Orders { get; set; }
        public DbSet<StreamOrderItem> OrderItems { get; set; }
        public DbSet<StreamProductCache> ProductCaches { get; set; }
        public DbSet<WebhookLog> WebhookLogs { get; set; }
        public DbSet<StreamShopSettings> Settings { get; set; }
        public DbSet<StreamLivestreamProduct> LivestreamProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StreamOrder>()
                .HasMany(o => o.OrderItems)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
