using System;
using System.Collections.Generic;
using System.Linq;

namespace DATN64.Models
{
    public class MockDataService
    {
        private static MockDataService? _instance;
        public static MockDataService Instance => _instance ??= new MockDataService();

        // Models
        public class UserSession
        {
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Avatar { get; set; } = "";
            public List<string> Roles { get; set; } = new();
            public List<string> Permissions { get; set; } = new();
        }

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string SKU { get; set; } = "";
            public string Barcode { get; set; } = "";
            public string Brand { get; set; } = "";
            public string Category { get; set; } = "";
            public string Supplier { get; set; } = "";
            public decimal ImportPrice { get; set; }
            public decimal Price { get; set; }
            public decimal OriginalPrice { get; set; }
            public DateTime? DiscountExpiry { get; set; }
            public int DiscountRate { get; set; }
            public bool IsBestSeller { get; set; }
            public int Stock { get; set; }
            public string Specifications { get; set; } = "";
            public string Image { get; set; } = "";
            public string Status { get; set; } = "Đang bán"; // Đang bán, Ngừng bán, Hết hàng
            public List<string> Images { get; set; } = new();
        }

        public class Category
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public int ProductCount { get; set; }
        }

        public class Brand
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public int ProductCount { get; set; }
        }

        public class Supplier
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Code { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";
            public string Address { get; set; } = "";
        }

        public class OrderItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public string SKU { get; set; } = "";
            public string Image { get; set; } = "";
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Total => Price * Quantity;
        }

        public class Order
        {
            public int Id { get; set; }
            public string OrderCode { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public string CustomerPhone { get; set; } = "";
            public string CustomerAddress { get; set; } = "";
            public DateTime OrderDate { get; set; }
            public string Status { get; set; } = "Đơn mới"; // Đơn mới, Đã xác nhận, Đang đóng gói, Đang giao, Hoàn thành, Đã hủy
            public string Channel { get; set; } = "Cửa hàng"; // Cửa hàng, Website, TikTok Shop
            public List<OrderItem> Items { get; set; } = new();
            public decimal SubTotal => Items.Sum(i => i.Total);
            public decimal Discount { get; set; } = 0;
            public decimal Total => SubTotal - Discount;
            public string PaymentMethod { get; set; } = "Tiền mặt"; // Tiền mặt, Chuyển khoản, COD
            public string Note { get; set; } = "";
        }

        public class InventoryTransaction
        {
            public int Id { get; set; }
            public string Code { get; set; } = "";
            public string Type { get; set; } = "Nhập kho"; // Nhập kho, Xuất kho, Kiểm kho, Điều chỉnh
            public string ProductSKU { get; set; } = "";
            public string ProductName { get; set; } = "";
            public int QuantityChange { get; set; }
            public string Creator { get; set; } = "";
            public DateTime Date { get; set; }
            public string Note { get; set; } = "";
        }

        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";
            public string Address { get; set; } = "";
            public int Points { get; set; } = 0;
            public string MembershipRank { get; set; } = "Đồng"; // Đồng, Bạc, Vàng, Kim Cương
            public DateTime CreatedDate { get; set; }
        }

        public class Employee
        {
            public int Id { get; set; }
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Password { get; set; } = "";
            public string Status { get; set; } = "Đang làm việc"; // Đang làm việc, Tạm ngưng, Đã nghỉ
            public List<string> Roles { get; set; } = new();
            public DateTime JoinedDate { get; set; }
        }

        public class RolePermission
        {
            public string RoleName { get; set; } = "";
            public List<string> Permissions { get; set; } = new();
        }

        public class Voucher
        {
            public int Id { get; set; }
            public string Code { get; set; } = "";
            public string Type { get; set; } = "Giảm %"; // Giảm %, Giảm tiền
            public decimal Value { get; set; }
            public decimal MinOrderValue { get; set; }
            public int Quantity { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Status { get; set; } = "Đang diễn ra";
        }

        public class TikTokShopConfig
        {
            public bool IsConnected { get; set; } = false;
            public string ShopName { get; set; } = "";
            public string ShopId { get; set; } = "";
            public DateTime LastSyncTime { get; set; }
            public string SyncStatus { get; set; } = "Chưa kết nối"; // Chưa kết nối, Bình thường, Lỗi
        }

        public class TikTokSyncLog
        {
            public int Id { get; set; }
            public string Type { get; set; } = "Sản phẩm"; // Sản phẩm, Đơn hàng, Tồn kho
            public string Message { get; set; } = "";
            public string Status { get; set; } = "Thành công"; // Thành công, Thất bại
            public DateTime Timestamp { get; set; }
        }

        public class SystemNotification
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public string Type { get; set; } = "Thông tin"; // Đơn mới, Hết hàng, Đồng bộ lỗi, Hệ thống
            public DateTime Timestamp { get; set; }
            public bool IsRead { get; set; } = false;
        }

        public class ChatMessage
        {
            public string Sender { get; set; } = ""; // User, AI
            public string Message { get; set; } = "";
            public DateTime Timestamp { get; set; }
        }

        // Memory Collections
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<InventoryTransaction> InventoryTransactions { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Employee> Employees { get; set; } = new();
        public List<RolePermission> RolesList { get; set; } = new();
        public List<Voucher> Vouchers { get; set; } = new();
        public TikTokShopConfig TikTokConfig { get; set; } = new();
        public List<TikTokSyncLog> TikTokSyncLogs { get; set; } = new();
        public List<SystemNotification> Notifications { get; set; } = new();
        public List<ChatMessage> ChatHistory { get; set; } = new();
        
        // System Settings
        public string ShopName { get; set; } = "Cửa hàng Công nghệ NovaTech";
        public string ShopLogo { get; set; } = "/images/novatech_logo.png";
        public string ShopAddress { get; set; } = "123 Đường Ba Tháng Hai, Quận 10, TP. Hồ Chí Minh";
        public string ShopEmail { get; set; } = "contact@novatech.vn";
        public string ShopHotline { get; set; } = "1900 8198";
        public string SystemConfig { get; set; } = "Mở cửa: 8:00 - 22:00 hằng ngày";

        private MockDataService()
        {
            SeedInitialData();
        }

        private void SeedInitialData()
        {
            // Initializing Categories
            Categories.Add(new Category { Id = 1, Name = "Laptop", Description = "Laptop văn phòng, Gaming, Đồ họa", ProductCount = 2 });
            Categories.Add(new Category { Id = 2, Name = "Điện thoại", Description = "Smartphone chính hãng", ProductCount = 3 });
            Categories.Add(new Category { Id = 3, Name = "Phụ kiện", Description = "Bàn phím, chuột, tai nghe", ProductCount = 2 });
            Categories.Add(new Category { Id = 4, Name = "Máy tính bảng", Description = "Máy tính bảng iPad, Android", ProductCount = 2 });
            Categories.Add(new Category { Id = 5, Name = "Loa", Description = "Loa Bluetooth, âm thanh", ProductCount = 1 });

            // Initializing Brands
            Brands.Add(new Brand { Id = 1, Name = "Apple", Description = "Thương hiệu cao cấp từ Mỹ", ProductCount = 2 });
            Brands.Add(new Brand { Id = 2, Name = "Asus", Description = "Laptop gaming & linh kiện", ProductCount = 2 });
            Brands.Add(new Brand { Id = 3, Name = "Sony", Description = "Thiết bị âm thanh chính hãng", ProductCount = 2 });

            // Initializing Suppliers
            Suppliers.Add(new Supplier { Id = 1, Name = "Công ty TNHH Apple Việt Nam", Code = "SUP-APL", Phone = "028 3910 1818", Email = "contact@apple.com.vn", Address = "Q.1, TP.HCM" });
            Suppliers.Add(new Supplier { Id = 2, Name = "Nhà phân phối FPT Synnex", Code = "SUP-FPT", Phone = "024 7300 6666", Email = "fpt.synnex@fpt.com.vn", Address = "Cầu Giấy, Hà Nội" });

            // Initializing Products
            Products.Add(new Product 
            { 
                Id = 1, 
                Name = "iPhone 15 Pro Max 256GB", 
                SKU = "IPHONE15PM-256", 
                Barcode = "893123456789", 
                Brand = "Apple", 
                Category = "Điện thoại", 
                Supplier = "Công ty TNHH Apple Việt Nam", 
                ImportPrice = 28000000, 
                Price = 27990000, 
                OriginalPrice = 32990000,
                DiscountRate = 15,
                DiscountExpiry = DateTime.Now.AddHours(36),
                IsBestSeller = true,
                Stock = 15, 
                Specifications = "Màn hình: 6.7 inch OLED Super Retina XDR\nChip: Apple A17 Pro\nRAM: 8GB\nBộ nhớ: 256GB\nCamera: Chính 48MP & Phụ 12MP, 12MP", 
                Image = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?q=80&w=300&auto=format&fit=crop",
                Status = "Đang bán",
                Images = new List<string> { "https://images.unsplash.com/photo-1695048133142-1a20484d2569?q=80&w=300&auto=format&fit=crop" }
            });
            Products.Add(new Product 
            { 
                Id = 2, 
                Name = "MacBook Pro 14 inch M3 8GB/512GB", 
                SKU = "MACM3-14-512", 
                Barcode = "893123456790", 
                Brand = "Apple", 
                Category = "Laptop", 
                Supplier = "Công ty TNHH Apple Việt Nam", 
                ImportPrice = 34000000, 
                Price = 39990000, 
                OriginalPrice = 39990000,
                IsBestSeller = true,
                Stock = 2, // Sắp hết hàng
                Specifications = "Màn hình: 14.2 inch Liquid Retina XDR\nChip: Apple M3\nRAM: 8GB\nSSD: 512GB\nHĐH: macOS Sonoma", 
                Image = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?q=80&w=300&auto=format&fit=crop",
                Status = "Đang bán",
                Images = new List<string> { "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?q=80&w=300&auto=format&fit=crop" }
            });
            Products.Add(new Product 
            { 
                Id = 3, 
                Name = "ASUS ROG Zephyrus G14", 
                SKU = "ASUS-G14-ROG", 
                Barcode = "893123456791", 
                Brand = "Asus", 
                Category = "Laptop", 
                Supplier = "Nhà phân phối FPT Synnex", 
                ImportPrice = 31000000, 
                Price = 35990000, 
                OriginalPrice = 35990000,
                Stock = 0, // Hết hàng
                Specifications = "Màn hình: 14 inch WQXGA 120Hz\nCPU: AMD Ryzen 9\nRAM: 16GB\nSSD: 1TB\nGPU: RTX 4060", 
                Image = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?q=80&w=300&auto=format&fit=crop",
                Status = "Hết hàng",
                Images = new List<string> { "https://images.unsplash.com/photo-1603302576837-37561b2e2302?q=80&w=300&auto=format&fit=crop" }
            });
            Products.Add(new Product 
            { 
                Id = 4, 
                Name = "Tai nghe chống ồn Sony WH-1000XM5", 
                SKU = "SONY-WH1000XM5", 
                Barcode = "893123456792", 
                Brand = "Sony", 
                Category = "Phụ kiện", 
                Supplier = "Nhà phân phối FPT Synnex", 
                ImportPrice = 6500000, 
                Price = 6990000, 
                OriginalPrice = 7990000,
                DiscountRate = 12,
                DiscountExpiry = DateTime.Now.AddHours(20),
                Stock = 25, 
                Specifications = "Chống ồn: Active Noise Cancelling\nThời lượng pin: Lên đến 30 giờ\nKết nối: Bluetooth 5.2", 
                Image = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?q=80&w=300&auto=format&fit=crop",
                Status = "Đang bán",
                Images = new List<string> { "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?q=80&w=300&auto=format&fit=crop" }
            });
            Products.Add(new Product 
            { 
                Id = 5, 
                Name = "iPad Pro M4 11 inch Wifi 256GB", 
                SKU = "IPADPRO-M4-11", 
                Barcode = "893123456793", 
                Brand = "Apple", 
                Category = "Máy tính bảng", 
                Supplier = "Công ty TNHH Apple Việt Nam", 
                ImportPrice = 24000000, 
                Price = 28990000, 
                OriginalPrice = 28990000,
                Stock = 10, 
                Specifications = "Màn hình: 11 inch Ultra Retina XDR Tandem OLED\nChip: Apple M4 9 nhân\nRAM: 8GB\nBộ nhớ: 256GB", 
                Image = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?q=80&w=300&auto=format&fit=crop",
                Status = "Đang bán",
                Images = new List<string> { "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?q=80&w=300&auto=format&fit=crop" }
            });
            Products.Add(new Product 
            { 
                Id = 6, 
                Name = "Máy tính bảng Samsung Galaxy Tab S9 Ultra", 
                SKU = "SAMSUNG-TABS9U", 
                Barcode = "893123456794", 
                Brand = "Samsung", 
                Category = "Máy tính bảng", 
                Supplier = "Nhà phân phối FPT Synnex", 
                ImportPrice = 18000000, 
                Price = 21990000, 
                OriginalPrice = 24990000,
                DiscountRate = 12,
                DiscountExpiry = DateTime.Now.AddHours(12),
                IsBestSeller = true,
                Stock = 8, 
                Specifications = "Màn hình: 14.6 inch Dynamic AMOLED 2X 120Hz\nChip: Snapdragon 8 Gen 2 for Galaxy\nRAM: 12GB\nBộ nhớ: 256GB\nKèm bút S-Pen cao cấp", 
                Image = "https://images.unsplash.com/photo-1589739900243-4b52cd9b104e?q=80&w=300&auto=format&fit=crop",
                Status = "Đang bán",
                Images = new List<string> { "https://images.unsplash.com/photo-1589739900243-4b52cd9b104e?q=80&w=300&auto=format&fit=crop" }
            });
            Products.Add(new Product 
            { 
                Id = 7, 
                Name = "Loa Bluetooth di động Sony SRS-XE200", 
                SKU = "SONY-SRS-XE200", 
                Barcode = "893123456795", 
                Brand = "Sony", 
                Category = "Loa", 
                Supplier = "Nhà phân phối FPT Synnex", 
                ImportPrice = 1800000, 
                Price = 2490000, 
                OriginalPrice = 2990000,
                DiscountRate = 16,
                DiscountExpiry = DateTime.Now.AddHours(40),
                Stock = 20, 
                Specifications = "Âm thanh: Line-Shape Diffuser cho âm thanh rộng\nKháng nước/bụi: IP67\nThời lượng pin: Lên đến 16 giờ", 
                Image = "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?q=80&w=300&auto=format&fit=crop",
                Status = "Đang bán",
                Images = new List<string> { "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?q=80&w=300&auto=format&fit=crop" }
            });

            // Initializing Role Permissions
            RolesList.Add(new RolePermission 
            { 
                RoleName = "Super Admin", 
                Permissions = new List<string> { 
                    "View_Dashboard", "View_Product", "Create_Product", "Edit_Product", "Delete_Product",
                    "View_Inventory", "Import_Inventory", "Export_Inventory", "Inventory_Inventory",
                    "View_Order", "Create_Order", "Edit_Order", "Delete_Order", "Approve_Order", "Export_Order",
                    "View_Customer", "Create_Customer", "Edit_Customer", "Delete_Customer",
                    "View_Employee", "Create_Employee", "Edit_Employee", "Delete_Employee",
                    "View_Role", "Create_Role", "Edit_Role", "Delete_Role", "Assign_Role",
                    "View_Setting", "Edit_Setting", "View_TikTok", "Sync_TikTok", "View_Report", "View_AI", "View_Promotion"
                } 
            });
            RolesList.Add(new RolePermission 
            { 
                RoleName = "Quản lý cửa hàng", 
                Permissions = new List<string> { 
                    "View_Dashboard", "View_Product", "Create_Product", "Edit_Product", "Delete_Product",
                    "View_Inventory", "Import_Inventory", "Export_Inventory", "Inventory_Inventory",
                    "View_Order", "Create_Order", "Edit_Order", "Delete_Order", "Approve_Order", "Export_Order",
                    "View_Customer", "Create_Customer", "Edit_Customer", "Delete_Customer",
                    "View_Employee", "View_TikTok", "Sync_TikTok", "View_Report", "View_AI", "View_Promotion"
                } 
            });
            RolesList.Add(new RolePermission 
            { 
                RoleName = "Nhân viên bán hàng", 
                Permissions = new List<string> { 
                    "View_Dashboard", "View_Product", "View_Order", "Create_Order", "Edit_Order",
                    "View_Customer", "Create_Customer", "Edit_Customer", "View_AI"
                } 
            });
            RolesList.Add(new RolePermission 
            { 
                RoleName = "Nhân viên kho", 
                Permissions = new List<string> { 
                    "View_Dashboard", "View_Product", "View_Inventory", "Import_Inventory", "Export_Inventory", "Inventory_Inventory"
                } 
            });
            RolesList.Add(new RolePermission 
            { 
                RoleName = "Kế toán", 
                Permissions = new List<string> { 
                    "View_Dashboard", "View_Order", "Export_Order", "View_Report"
                } 
            });
            RolesList.Add(new RolePermission 
            { 
                RoleName = "Marketing", 
                Permissions = new List<string> { 
                    "View_Dashboard", "View_Product", "View_TikTok", "Sync_TikTok", "View_Promotion", "View_Report"
                } 
            });

            // Initializing Employees
            Employees.Add(new Employee { Id = 1, FullName = "Nguyễn Văn Admin", Email = "admin@novatech.vn", Phone = "0901234567", Password = "123", Status = "Đang làm việc", Roles = new List<string> { "Super Admin" }, JoinedDate = DateTime.Now.AddYears(-2) });
            Employees.Add(new Employee { Id = 2, FullName = "Trần Thị Manager", Email = "manager@novatech.vn", Phone = "0907654321", Password = "123", Status = "Đang làm việc", Roles = new List<string> { "Quản lý cửa hàng" }, JoinedDate = DateTime.Now.AddYears(-1) });
            Employees.Add(new Employee { Id = 3, FullName = "Lê Văn Sale", Email = "sale@novatech.vn", Phone = "0912345678", Password = "123", Status = "Đang làm việc", Roles = new List<string> { "Nhân viên bán hàng" }, JoinedDate = DateTime.Now.AddMonths(-6) });
            Employees.Add(new Employee { Id = 4, FullName = "Phạm Văn Kho", Email = "kho@novatech.vn", Phone = "0987654321", Password = "123", Status = "Đang làm việc", Roles = new List<string> { "Nhân viên kho" }, JoinedDate = DateTime.Now.AddMonths(-3) });
            Employees.Add(new Employee { Id = 5, FullName = "Đỗ Thị Kế Toán", Email = "ketoan@novatech.vn", Phone = "0977665544", Password = "123", Status = "Đang làm việc", Roles = new List<string> { "Kế toán" }, JoinedDate = DateTime.Now.AddMonths(-8) });
            Employees.Add(new Employee { Id = 6, FullName = "Hoàng Văn Marketing", Email = "marketing@novatech.vn", Phone = "0944556677", Password = "123", Status = "Đang làm việc", Roles = new List<string> { "Marketing" }, JoinedDate = DateTime.Now.AddMonths(-4) });

            // Initializing Customers
            Customers.Add(new Customer { Id = 1, Name = "Trần Minh Quân", Phone = "0909876543", Email = "minhquan@gmail.com", Address = "Quận 1, TP.HCM", Points = 1200, MembershipRank = "Vàng", CreatedDate = DateTime.Now.AddMonths(-12) });
            Customers.Add(new Customer { Id = 2, Name = "Lê Hồng Thắm", Phone = "0918765432", Email = "hongtham@gmail.com", Address = "Quận Bình Thạnh, TP.HCM", Points = 450, MembershipRank = "Bạc", CreatedDate = DateTime.Now.AddMonths(-6) });
            Customers.Add(new Customer { Id = 3, Name = "Nguyễn Hữu Khang", Phone = "0923456789", Email = "huukhang@gmail.com", Address = "Quận 7, TP.HCM", Points = 3200, MembershipRank = "Kim Cương", CreatedDate = DateTime.Now.AddMonths(-18) });

            // Initializing Orders
            Orders.Add(new Order 
            { 
                Id = 1, 
                OrderCode = "ORD-001", 
                CustomerName = "Trần Minh Quân", 
                CustomerPhone = "0909876543", 
                CustomerAddress = "Quận 1, TP.HCM", 
                OrderDate = DateTime.Now.AddDays(-2), 
                Status = "Hoàn thành", 
                Channel = "Website", 
                PaymentMethod = "Chuyển khoản",
                Items = new List<OrderItem> 
                { 
                    new OrderItem { ProductId = 1, ProductName = "iPhone 15 Pro Max 256GB", SKU = "IPHONE15PM-256", Image = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?q=80&w=300&auto=format&fit=crop", Quantity = 1, Price = 32990000 } 
                } 
            });
            Orders.Add(new Order 
            { 
                Id = 2, 
                OrderCode = "ORD-002", 
                CustomerName = "Lê Hồng Thắm", 
                CustomerPhone = "0918765432", 
                CustomerAddress = "Quận Bình Thạnh, TP.HCM", 
                OrderDate = DateTime.Now.AddDays(-1), 
                Status = "Đang giao", 
                Channel = "TikTok Shop", 
                PaymentMethod = "COD",
                Items = new List<OrderItem> 
                { 
                    new OrderItem { ProductId = 4, ProductName = "Tai nghe chống ồn Sony WH-1000XM5", SKU = "SONY-WH1000XM5", Image = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?q=80&w=300&auto=format&fit=crop", Quantity = 2, Price = 7990000 } 
                } 
            });
            Orders.Add(new Order 
            { 
                Id = 3, 
                OrderCode = "ORD-003", 
                CustomerName = "Khách Hàng Vãng Lai", 
                CustomerPhone = "0934567890", 
                CustomerAddress = "Mua trực tiếp tại cửa hàng", 
                OrderDate = DateTime.Now, 
                Status = "Đơn mới", 
                Channel = "Cửa hàng", 
                PaymentMethod = "Tiền mặt",
                Items = new List<OrderItem> 
                { 
                    new OrderItem { ProductId = 2, ProductName = "MacBook Pro 14 inch M3 8GB/512GB", SKU = "MACM3-14-512", Image = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?q=80&w=300&auto=format&fit=crop", Quantity = 1, Price = 39990000 } 
                } 
            });

            // Initializing Inventory Transactions
            InventoryTransactions.Add(new InventoryTransaction { Id = 1, Code = "GRN-001", Type = "Nhập kho", ProductSKU = "IPHONE15PM-256", ProductName = "iPhone 15 Pro Max 256GB", QuantityChange = 20, Creator = "Phạm Văn Kho", Date = DateTime.Now.AddDays(-10), Note = "Nhập hàng đợt đầu tháng" });
            InventoryTransactions.Add(new InventoryTransaction { Id = 2, Code = "GIN-001", Type = "Xuất kho", ProductSKU = "IPHONE15PM-256", ProductName = "iPhone 15 Pro Max 256GB", QuantityChange = -5, Creator = "Phạm Văn Kho", Date = DateTime.Now.AddDays(-2), Note = "Xuất bán cho khách hàng" });

            // Initializing Vouchers
            Vouchers.Add(new Voucher { Id = 1, Code = "NOVATECH10", Type = "Giảm %", Value = 10, MinOrderValue = 500000, Quantity = 100, StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddDays(15), Status = "Đang diễn ra" });
            Vouchers.Add(new Voucher { Id = 2, Code = "NOVATECH500K", Type = "Giảm tiền", Value = 500000, MinOrderValue = 10000000, Quantity = 20, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(7), Status = "Đang diễn ra" });

            // Initializing TikTok Config
            TikTokConfig.IsConnected = true;
            TikTokConfig.ShopName = "NovaTech Official Store";
            TikTokConfig.ShopId = "TTS-789234123";
            TikTokConfig.LastSyncTime = DateTime.Now.AddMinutes(-30);
            TikTokConfig.SyncStatus = "Bình thường";

            // Initializing TikTok Sync Logs
            TikTokSyncLogs.Add(new TikTokSyncLog { Id = 1, Type = "Sản phẩm", Message = "Đồng bộ thành công 4 sản phẩm lên TikTok Shop", Status = "Thành công", Timestamp = DateTime.Now.AddMinutes(-30) });
            TikTokSyncLogs.Add(new TikTokSyncLog { Id = 2, Type = "Đơn hàng", Message = "Đồng bộ đơn hàng mới: TTS-ORD-11234", Status = "Thành công", Timestamp = DateTime.Now.AddMinutes(-15) });
            TikTokSyncLogs.Add(new TikTokSyncLog { Id = 3, Type = "Tồn kho", Message = "Cập nhật tồn kho sản phẩm MACM3-14-512 bị lỗi kết nối mạng", Status = "Thất bại", Timestamp = DateTime.Now.AddMinutes(-5) });

            // Initializing Notifications
            Notifications.Add(new SystemNotification { Id = 1, Title = "Đơn hàng mới", Message = "Đơn hàng ORD-003 vừa được tạo từ kênh trực tiếp.", Type = "Đơn mới", Timestamp = DateTime.Now.AddMinutes(-5), IsRead = false });
            Notifications.Add(new SystemNotification { Id = 2, Title = "Sản phẩm sắp hết hàng", Message = "Sản phẩm MacBook Pro M3 còn dưới 3 chiếc trong kho.", Type = "Hết hàng", Timestamp = DateTime.Now.AddHours(-2), IsRead = false });
            Notifications.Add(new SystemNotification { Id = 3, Title = "Đồng bộ lỗi", Message = "Lỗi đồng bộ tồn kho sang TikTok Shop vào 14:05.", Type = "Đồng bộ lỗi", Timestamp = DateTime.Now.AddMinutes(-9), IsRead = false });

            // Chat History
            ChatHistory.Add(new ChatMessage { Sender = "AI", Message = "Chào bạn! Tôi là Trợ lý AI của NovaTech. Tôi có thể giúp bạn phân tích doanh thu, tìm sản phẩm tồn kho hoặc soạn thảo chương trình khuyến mãi. Hãy đặt câu hỏi cho tôi!", Timestamp = DateTime.Now.AddHours(-1) });
        }
    }
}
