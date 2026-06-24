using Microsoft.EntityFrameworkCore;

namespace DATN64.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<DanhMuc> DanhMucs { get; set; }
        public DbSet<ThuongHieu> ThuongHieus { get; set; }
        public DbSet<NhaCungCap> NhaCungCaps { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<DonHang> DonHangs { get; set; }
        public DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public DbSet<PhieuNhap> PhieuNhaps { get; set; }
        public DbSet<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<DonHang_Voucher> DonHang_Vouchers { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<TikTokShopConfig> TikTokShopConfigs { get; set; }
        public DbSet<TikTokSyncLog> TikTokSyncLogs { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DonHang_Voucher>()
                .HasKey(e => new { e.MaDonHang, e.MaVoucher });
            
            base.OnModelCreating(modelBuilder);
        }
    }
}
