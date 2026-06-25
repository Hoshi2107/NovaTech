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
        public string TrangThai { get; set; } = "Đã duyệt";

        // New properties for warehouse transactions
        public int? MaKho { get; set; }
        public int? MaKhoNguon { get; set; }
        public int? MaKhoDich { get; set; }
        public int? SoLuongTruoc { get; set; }
        public int? SoLuongSau { get; set; }
    }

    [Table("KhoHang")]
    public class KhoHang
    {
        [Key]
        public int MaKho { get; set; }
        public string TenKho { get; set; } = "";
        public string? MoTa { get; set; }
        public bool TrangThai { get; set; } = true;
        public DateTime NgayTao { get; set; } = DateTime.Now;
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

    public class TopProductViewModel
    {
        public SanPham SanPham { get; set; } = null!;
        public int OrderCount { get; set; }
        public int QuantitySold { get; set; }
    }

    public class TopCustomerViewModel
    {
        public KhachHang KhachHang { get; set; } = null!;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
