using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN64.Models
{
    [Table("InventoryTransaction")]
    public class InventoryTransaction
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Type { get; set; } = "Nhập kho";
        public string ProductSKU { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int QuantityChange { get; set; }
        public string Creator { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;
        public string Note { get; set; } = "";
    }

    [Table("SystemNotification")]
    public class SystemNotification
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "Thông tin";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }

    [Table("TikTokShopConfig")]
    public class TikTokShopConfig
    {
        [Key]
        public int Id { get; set; }
        public bool IsConnected { get; set; } = false;
        public string ShopName { get; set; } = "";
        public string ShopId { get; set; } = "";
        public DateTime LastSyncTime { get; set; } = DateTime.Now;
        public string SyncStatus { get; set; } = "Chưa kết nối";
    }

    [Table("TikTokSyncLog")]
    public class TikTokSyncLog
    {
        [Key]
        public int Id { get; set; }
        public string Type { get; set; } = "Sản phẩm";
        public string Message { get; set; } = "";
        public string Status { get; set; } = "Thành công";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    [Table("ChatMessage")]
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }
        public string Sender { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
