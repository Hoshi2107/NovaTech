using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public DbSet<KhoHang> KhoHangs { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<TikTokShopConfig> TikTokShopConfigs { get; set; }
        public DbSet<TikTokSyncLog> TikTokSyncLogs { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<CauHinh> CauHinhs { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<NhanVienRole> NhanVienRoles { get; set; }
        public DbSet<CustomerInboxThread> CustomerInboxThreads { get; set; }
        public DbSet<CustomerInboxMessage> CustomerInboxMessages { get; set; }
        public DbSet<ChamCong> ChamCongs { get; set; }
        public DbSet<SoQuy> SoQuys { get; set; }
        public DbSet<CongNoNCC> CongNoNCCs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DonHang_Voucher>()
                .HasKey(e => new { e.MaDonHang, e.MaVoucher });

            modelBuilder.Entity<CustomerInboxMessage>()
                .HasOne(m => m.Thread)
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            var lowStock = GetLowStockProductsToAutoImport();
            int result = base.SaveChanges();
            ProcessAutoImports(lowStock);
            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var lowStock = GetLowStockProductsToAutoImport();
            int result = await base.SaveChangesAsync(cancellationToken);
            await ProcessAutoImportsAsync(lowStock, cancellationToken);
            return result;
        }

        private List<SanPham> GetLowStockProductsToAutoImport()
        {
            return ChangeTracker.Entries<SanPham>()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added)
                .Select(e => e.Entity)
                .Where(p => p.SoLuongTon <= 5)
                .ToList();
        }

        private void ProcessAutoImports(List<SanPham> lowStockProducts)
        {
            if (lowStockProducts == null || !lowStockProducts.Any()) return;
            
            bool autoImportCreated = false;
            foreach (var item in lowStockProducts)
            {
                string skuStr = item.MaSanPham.ToString();
                bool hasPending = InventoryTransactions.Any(t => 
                    t.ProductSKU == skuStr && 
                    t.TrangThai == "Chờ duyệt" && 
                    (t.Type == "Nhập kho" || t.Type == "Điều chỉnh"));

                if (!hasPending)
                {
                    int count = InventoryTransactions.Count(t => t.Type == "Nhập kho") + 1;
                    string code = "PN" + count.ToString("D6");

                    var autoTx = new InventoryTransaction
                    {
                        Code = code,
                        Type = "Nhập kho",
                        ProductSKU = skuStr,
                        ProductName = item.TenSanPham,
                        QuantityChange = 50,
                        Creator = "Hệ thống (Tự động)",
                        Date = DateTime.Now,
                        Note = $"Đề xuất nhập tự động: Tồn kho chạm ngưỡng tối thiểu ({item.SoLuongTon} cái).",
                        TrangThai = "Chờ duyệt",
                        SoLuongTruoc = null,
                        SoLuongSau = null
                    };

                    InventoryTransactions.Add(autoTx);
                    autoImportCreated = true;
                }
            }

            if (autoImportCreated)
            {
                base.SaveChanges();
            }
        }

        private async Task ProcessAutoImportsAsync(List<SanPham> lowStockProducts, CancellationToken cancellationToken)
        {
            if (lowStockProducts == null || !lowStockProducts.Any()) return;
            
            bool autoImportCreated = false;
            foreach (var item in lowStockProducts)
            {
                string skuStr = item.MaSanPham.ToString();
                bool hasPending = await InventoryTransactions.AnyAsync(t => 
                    t.ProductSKU == skuStr && 
                    t.TrangThai == "Chờ duyệt" && 
                    (t.Type == "Nhập kho" || t.Type == "Điều chỉnh"),
                    cancellationToken);

                if (!hasPending)
                {
                    int count = await InventoryTransactions.CountAsync(t => t.Type == "Nhập kho", cancellationToken) + 1;
                    string code = "PN" + count.ToString("D6");

                    var autoTx = new InventoryTransaction
                    {
                        Code = code,
                        Type = "Nhập kho",
                        ProductSKU = skuStr,
                        ProductName = item.TenSanPham,
                        QuantityChange = 50,
                        Creator = "Hệ thống (Tự động)",
                        Date = DateTime.Now,
                        Note = $"Đề xuất nhập tự động: Tồn kho chạm ngưỡng tối thiểu ({item.SoLuongTon} cái).",
                        TrangThai = "Chờ duyệt",
                        SoLuongTruoc = null,
                        SoLuongSau = null
                    };

                    InventoryTransactions.Add(autoTx);
                    autoImportCreated = true;
                }
            }

            if (autoImportCreated)
            {
                await base.SaveChangesAsync(cancellationToken);
            }
        }

        public void AutoGenerateLowStockTickets()
        {
            var lowStockProducts = SanPhams.Where(p => p.SoLuongTon <= 5).ToList();
            if (lowStockProducts.Any())
            {
                bool autoImportCreated = false;
                foreach (var item in lowStockProducts)
                {
                    string skuStr = item.MaSanPham.ToString();
                    bool hasPending = InventoryTransactions.Any(t => 
                        t.ProductSKU == skuStr && 
                        t.TrangThai == "Chờ duyệt" && 
                        (t.Type == "Nhập kho" || t.Type == "Điều chỉnh"));

                    if (!hasPending)
                    {
                        int count = InventoryTransactions.Count(t => t.Type == "Nhập kho") + 1;
                        string code = "PN" + count.ToString("D6");

                        var autoTx = new InventoryTransaction
                        {
                            Code = code,
                            Type = "Nhập kho",
                            ProductSKU = skuStr,
                            ProductName = item.TenSanPham,
                            QuantityChange = 50,
                            Creator = "Hệ thống (Tự động)",
                            Date = DateTime.Now,
                            Note = $"Đề xuất nhập tự động: Tồn kho chạm ngưỡng tối thiểu ({item.SoLuongTon} cái).",
                            TrangThai = "Chờ duyệt",
                            SoLuongTruoc = null,
                            SoLuongSau = null
                        };

                        InventoryTransactions.Add(autoTx);
                        autoImportCreated = true;
                    }
                }

                if (autoImportCreated)
                {
                    base.SaveChanges();
                }
            }
        }

        public async Task AutoGenerateLowStockTicketsAsync()
        {
            var lowStockProducts = await SanPhams.Where(p => p.SoLuongTon <= 5).ToListAsync();
            if (lowStockProducts.Any())
            {
                bool autoImportCreated = false;
                foreach (var item in lowStockProducts)
                {
                    string skuStr = item.MaSanPham.ToString();
                    bool hasPending = await InventoryTransactions.AnyAsync(t => 
                        t.ProductSKU == skuStr && 
                        t.TrangThai == "Chờ duyệt" && 
                        (t.Type == "Nhập kho" || t.Type == "Điều chỉnh"));

                    if (!hasPending)
                    {
                        int count = await InventoryTransactions.CountAsync(t => t.Type == "Nhập kho") + 1;
                        string code = "PN" + count.ToString("D6");

                        var autoTx = new InventoryTransaction
                        {
                            Code = code,
                            Type = "Nhập kho",
                            ProductSKU = skuStr,
                            ProductName = item.TenSanPham,
                            QuantityChange = 50,
                            Creator = "Hệ thống (Tự động)",
                            Date = DateTime.Now,
                            Note = $"Đề xuất nhập tự động: Tồn kho chạm ngưỡng tối thiểu ({item.SoLuongTon} cái).",
                            TrangThai = "Chờ duyệt",
                            SoLuongTruoc = null,
                            SoLuongSau = null
                        };

                        InventoryTransactions.Add(autoTx);
                        autoImportCreated = true;
                    }
                }

                if (autoImportCreated)
                {
                    await base.SaveChangesAsync();
                }
            }
        }
    }
}
