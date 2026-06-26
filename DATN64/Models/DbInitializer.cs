using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace DATN64.Models
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            var db = context.Database;
            
            // Create KhoHang
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.KhoHang', 'U') IS NULL
                CREATE TABLE dbo.KhoHang (
                    MaKho INT IDENTITY(1,1) PRIMARY KEY,
                    TenKho NVARCHAR(255) NOT NULL DEFAULT '',
                    MoTa NVARCHAR(500) NULL,
                    TrangThai BIT NOT NULL DEFAULT 1,
                    NgayTao DATETIME NOT NULL DEFAULT GETDATE()
                );
            ");

            // Seed KhoHang
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.KhoHang)
                BEGIN
                    INSERT INTO dbo.KhoHang (TenKho, MoTa, TrangThai, NgayTao) 
                    VALUES (N'Kho chính', N'Kho lưu trữ hàng hóa chính của công ty', 1, GETDATE());
                    INSERT INTO dbo.KhoHang (TenKho, MoTa, TrangThai, NgayTao) 
                    VALUES (N'Kho cửa hàng', N'Kho hàng trực tiếp phục vụ bán lẻ tại cửa hàng', 1, GETDATE());
                END
            ");

            // Create InventoryTransaction
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.InventoryTransaction', 'U') IS NULL
                CREATE TABLE dbo.InventoryTransaction (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Code NVARCHAR(MAX) NOT NULL DEFAULT '',
                    Type NVARCHAR(MAX) NOT NULL DEFAULT 'Nhập kho',
                    ProductSKU NVARCHAR(MAX) NOT NULL DEFAULT '',
                    ProductName NVARCHAR(MAX) NOT NULL DEFAULT '',
                    QuantityChange INT NOT NULL,
                    Creator NVARCHAR(MAX) NOT NULL DEFAULT '',
                    Date DATETIME NOT NULL DEFAULT GETDATE(),
                    Note NVARCHAR(MAX) NOT NULL DEFAULT ''
                );
            ");

            // Alter InventoryTransaction to add Vietnamese fields if not exists
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InventoryTransaction') AND name = 'MaKho')
                    ALTER TABLE dbo.InventoryTransaction ADD MaKho INT NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InventoryTransaction') AND name = 'MaKhoNguon')
                    ALTER TABLE dbo.InventoryTransaction ADD MaKhoNguon INT NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InventoryTransaction') AND name = 'MaKhoDich')
                    ALTER TABLE dbo.InventoryTransaction ADD MaKhoDich INT NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InventoryTransaction') AND name = 'SoLuongTruoc')
                    ALTER TABLE dbo.InventoryTransaction ADD SoLuongTruoc INT NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InventoryTransaction') AND name = 'SoLuongSau')
                    ALTER TABLE dbo.InventoryTransaction ADD SoLuongSau INT NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InventoryTransaction') AND name = 'TrangThai')
                    ALTER TABLE dbo.InventoryTransaction ADD TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Đã duyệt';
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InventoryTransaction') AND name = 'NguoiDuyet')
                    ALTER TABLE dbo.InventoryTransaction ADD NguoiDuyet NVARCHAR(200) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InventoryTransaction') AND name = 'NgayDuyet')
                    ALTER TABLE dbo.InventoryTransaction ADD NgayDuyet DATETIME2 NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InventoryTransaction') AND name = 'LyDoTuChoi')
                    ALTER TABLE dbo.InventoryTransaction ADD LyDoTuChoi NVARCHAR(500) NULL;
            ");

            // Create SystemNotification
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.SystemNotification', 'U') IS NULL
                CREATE TABLE dbo.SystemNotification (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Title NVARCHAR(MAX) NOT NULL DEFAULT '',
                    Message NVARCHAR(MAX) NOT NULL DEFAULT '',
                    Type NVARCHAR(MAX) NOT NULL DEFAULT 'Thông tin',
                    Timestamp DATETIME NOT NULL DEFAULT GETDATE(),
                    IsRead BIT NOT NULL DEFAULT 0
                );
            ");

            // Create TikTokShopConfig
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.TikTokShopConfig', 'U') IS NULL
                CREATE TABLE dbo.TikTokShopConfig (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    IsConnected BIT NOT NULL DEFAULT 0,
                    ShopName NVARCHAR(MAX) NOT NULL DEFAULT '',
                    ShopId NVARCHAR(MAX) NOT NULL DEFAULT '',
                    LastSyncTime DATETIME NOT NULL DEFAULT GETDATE(),
                    SyncStatus NVARCHAR(MAX) NOT NULL DEFAULT 'Chưa kết nối'
                );
            ");

            // Create TikTokSyncLog
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.TikTokSyncLog', 'U') IS NULL
                CREATE TABLE dbo.TikTokSyncLog (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Type NVARCHAR(MAX) NOT NULL DEFAULT 'Sản phẩm',
                    Message NVARCHAR(MAX) NOT NULL DEFAULT '',
                    Status NVARCHAR(MAX) NOT NULL DEFAULT 'Thành công',
                    Timestamp DATETIME NOT NULL DEFAULT GETDATE()
                );
            ");

            // Create ChatMessage
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.ChatMessage', 'U') IS NULL
                CREATE TABLE dbo.ChatMessage (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Sender NVARCHAR(MAX) NOT NULL DEFAULT '',
                    Message NVARCHAR(MAX) NOT NULL DEFAULT '',
                    Timestamp DATETIME NOT NULL DEFAULT GETDATE()
                );
            ");
            // Create CustomerInboxThread
            ExecuteSql(db, @"
    IF OBJECT_ID('dbo.CustomerInboxThread', 'U') IS NULL
    CREATE TABLE dbo.CustomerInboxThread (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CustomerId INT NOT NULL DEFAULT 0,
        CustomerName NVARCHAR(150) NOT NULL DEFAULT '',
        CustomerPhone NVARCHAR(20) NULL,
        Channel NVARCHAR(50) NOT NULL DEFAULT 'Store',
        Subject NVARCHAR(255) NOT NULL DEFAULT N'Chat hỗ trợ NovaTech',
        Status NVARCHAR(50) NOT NULL DEFAULT 'Unread',
        Priority NVARCHAR(50) NOT NULL DEFAULT 'Medium',
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
");

            // Create CustomerInboxMessage
            ExecuteSql(db, @"
    IF OBJECT_ID('dbo.CustomerInboxMessage', 'U') IS NULL
    CREATE TABLE dbo.CustomerInboxMessage (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ThreadId INT NOT NULL,
        Sender NVARCHAR(50) NOT NULL DEFAULT 'customer',
        Text NVARCHAR(MAX) NOT NULL DEFAULT '',
        Timestamp DATETIME NOT NULL DEFAULT GETDATE(),
        IsRead BIT NOT NULL DEFAULT 0,
        IsAutoReply BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_CustomerInboxMessage_CustomerInboxThread
            FOREIGN KEY (ThreadId) REFERENCES dbo.CustomerInboxThread(Id)
            ON DELETE CASCADE
    );
");

            // Create CauHinh
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.CauHinh', 'U') IS NULL
                CREATE TABLE dbo.CauHinh (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    TenCuaHang NVARCHAR(255) NOT NULL DEFAULT 'NovaTech',
                    Email NVARCHAR(100) NULL,
                    SoDienThoai NVARCHAR(20) NULL,
                    DiaChi NVARCHAR(255) NULL,
                    Logo NVARCHAR(500) NULL
                );
            ");

            // Seed default CauHinh if empty
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.CauHinh)
                BEGIN
                    INSERT INTO dbo.CauHinh (TenCuaHang, Email, SoDienThoai, DiaChi, Logo)
                    VALUES (N'Siêu thị NovaTech', 'contact@novatech.vn', '1900 1000', N'123 Đường Điện Biên Phủ, TP.HCM', '/uploads/logo/default_logo.png');
                END
            ");

            // Create Role table
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.Role', 'U') IS NULL
                CREATE TABLE dbo.Role (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(50) NOT NULL,
                    Description NVARCHAR(255) NULL
                );
            ");

            // Create RolePermission table
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.RolePermission', 'U') IS NULL
                CREATE TABLE dbo.RolePermission (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    RoleName NVARCHAR(50) NOT NULL,
                    PermissionName NVARCHAR(100) NOT NULL
                );
            ");

            // Create NhanVienRole table
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.NhanVienRole', 'U') IS NULL
                CREATE TABLE dbo.NhanVienRole (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    MaNhanVien INT NOT NULL,
                    RoleId INT NOT NULL
                );
            ");

            // Create ChamCong table
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.ChamCong', 'U') IS NULL
                CREATE TABLE dbo.ChamCong (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    MaNhanVien INT NOT NULL,
                    NgayCham DATETIME NOT NULL,
                    GioVao DATETIME NULL,
                    GioRa DATETIME NULL,
                    TongGioLam FLOAT NULL,
                    GhiChu NVARCHAR(255) NULL,
                    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Đang làm'
                );
            ");

            // Add LuongTheoGio to NhanVien
            ExecuteSql(db, @"
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.objects o ON c.object_id = o.object_id
                    WHERE o.name = 'NhanVien' AND c.name = 'LuongTheoGio'
                )
                ALTER TABLE dbo.NhanVien ADD LuongTheoGio DECIMAL(18,2) NULL;
            ");

            // Ensure DonHang.MaNhanVien column allows NULL when the model is optional
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.DonHang', 'U') IS NOT NULL
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM sys.columns c
                        INNER JOIN sys.objects o ON c.object_id = o.object_id
                        WHERE o.name = 'DonHang' AND c.name = 'MaNhanVien' AND c.is_nullable = 0
                    )
                    ALTER TABLE dbo.DonHang ALTER COLUMN MaNhanVien INT NULL;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.columns c
                        INNER JOIN sys.objects o ON c.object_id = o.object_id
                        WHERE o.name = 'DonHang' AND c.name = 'GhiChu'
                    )
                    ALTER TABLE dbo.DonHang ADD GhiChu NVARCHAR(MAX) NULL;
                END
            ");

            // Seed categories, brands, suppliers, vouchers and featured products
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.DanhMuc)
                BEGIN
                    SET IDENTITY_INSERT dbo.DanhMuc ON;
                    INSERT INTO dbo.DanhMuc (MaDanhMuc, TenDanhMuc, MoTa) VALUES
                        (1, N'Điện thoại', N'Điện thoại cao cấp và phổ thông'),
                        (2, N'Laptop', N'Laptop hiệu năng cao cho công việc và giải trí'),
                        (3, N'Máy tính bảng', N'Máy tính bảng mỏng nhẹ, giải trí và học tập'),
                        (4, N'Tai nghe', N'Tai nghe chống ồn, true wireless và gaming'),
                        (5, N'Loa', N'Loa bluetooth và loa máy tính chất lượng cao'),
                        (6, N'Phụ kiện', N'Phụ kiện công nghệ đa dạng và tiện dụng');
                    SET IDENTITY_INSERT dbo.DanhMuc OFF;
                END
            ");

            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.ThuongHieu)
                BEGIN
                    SET IDENTITY_INSERT dbo.ThuongHieu ON;
                    INSERT INTO dbo.ThuongHieu (MaThuongHieu, TenThuongHieu, MoTa) VALUES
                        (1, N'Apple', N'Thiết bị Apple chính hãng'),
                        (2, N'Samsung', N'Công nghệ Samsung hàng đầu'),
                        (3, N'Xiaomi', N'Thiết bị thông minh giá tốt'),
                        (4, N'Oppo', N'Điện thoại thông minh trẻ trung'),
                        (5, N'Asus', N'Laptop cho công việc và game'),
                        (6, N'Dell', N'Laptop doanh nghiệp bền bỉ'),
                        (7, N'Sony', N'Thiết bị âm thanh cao cấp'),
                        (8, N'JBL', N'Loa và âm thanh di động'),
                        (9, N'Logitech', N'Phụ kiện máy tính chuyên nghiệp'),
                        (10, N'Anker', N'Phụ kiện và sạc nhanh');
                    SET IDENTITY_INSERT dbo.ThuongHieu OFF;
                END
            ");

            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.NhaCungCap)
                BEGIN
                    SET IDENTITY_INSERT dbo.NhaCungCap ON;
                    INSERT INTO dbo.NhaCungCap (MaNCC, TenNCC, SoDienThoai, Email, DiaChi) VALUES
                        (1, N'NovaTech Logistics', '028 3827 1900', 'logistics@novatech.vn', N'123 Đường 3/2, TP.HCM'),
                        (2, N'Samsung Vietnam', '028 3910 8383', 'support@samsung.com', N'456 Đường Trần Hưng Đạo, TP.HCM'),
                        (3, N'Apple Vietnam', '028 3949 0000', 'apple@vn.com', N'789 Đường Nguyễn Huệ, TP.HCM'),
                        (4, N'Xiaomi Vietnam', '028 3518 8888', 'sale@xiaomi.vn', N'101 Đường Lê Lai, TP.HCM'),
                        (5, N'ASUS Vietnam', '028 3910 1234', 'service@asus.vn', N'202 Đường Võ Văn Kiệt, TP.HCM');
                    SET IDENTITY_INSERT dbo.NhaCungCap OFF;
                END
            ");

            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.Voucher)
                BEGIN
                    INSERT INTO dbo.Voucher (MaCode, GiaTri, SoLuong, NgayBatDau, NgayKetThuc) VALUES
                        (N'NOVATECH50', 50000, 200, GETDATE(), DATEADD(day, 30, GETDATE())),
                        (N'NOVA10', 100000, 150, GETDATE(), DATEADD(day, 60, GETDATE())),
                        (N'FHDPROMO', 150000, 100, GETDATE(), DATEADD(day, 45, GETDATE())),
                        (N'GIAOFREE', 50000, 80, GETDATE(), DATEADD(day, 90, GETDATE()));
                END
            ");

            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.SanPham)
                BEGIN
                    INSERT INTO dbo.SanPham (TenSanPham, MaDanhMuc, MaThuongHieu, MaNCC, GiaNhap, GiaBan, SoLuongTon, MoTa, HinhAnh, TrangThai) VALUES
                        (N'iPhone 15 Pro Max 256GB', 1, 1, 3, 26000000, 31990000, 20, N'iPhone 15 Pro Max chính hãng, chip A17, camera siêu nét.', 'https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'Samsung Galaxy S24 Ultra', 1, 2, 2, 22000000, 27990000, 18, N'Samsung Galaxy S24 Ultra, màn hình Infinity-O, camera 200MP.', 'https://images.unsplash.com/photo-1510557880182-3f8ed9f4a7b6?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'Xiaomi 14 Pro', 1, 3, 4, 12000000, 16990000, 25, N'Xiaomi 14 Pro hiệu năng cao, pin 5100mAh.', 'https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'MacBook Pro 14 inch M3', 2, 1, 3, 36000000, 42990000, 12, N'MacBook Pro M3, hiệu năng vượt trội cho sáng tạo nội dung.', 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'Dell XPS 13 OLED', 2, 6, 5, 25000000, 31990000, 10, N'Dell XPS 13 phiên bản OLED sang trọng, siêu nhẹ.', 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'ASUS ROG Strix G16', 2, 5, 5, 31000000, 38990000, 8, N'Laptop gaming ASUS ROG Strix, hiệu năng xử lý game mượt mà.', 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'iPad Pro 12.9 inch', 3, 1, 3, 24000000, 29990000, 14, N'iPad Pro 12.9 inch, màn hình Liquid Retina XDR cho sáng tạo.', 'https://images.unsplash.com/photo-1517430816045-df4b7de11d5c?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'Samsung Galaxy Tab S9 Ultra', 3, 2, 2, 19000000, 21990000, 16, N'Tablet cao cấp Samsung, phù hợp học tập và giải trí.', 'https://images.unsplash.com/photo-1517430816045-df4b7de11d5c?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'Sony WH-1000XM5', 4, 7, 2, 5200000, 6990000, 30, N'Tai nghe chống ồn Sony WH-1000XM5, chuẩn studio.', 'https://images.unsplash.com/photo-1511367461989-f85a21fda167?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'JBL Charge 5', 5, 8, 1, 2500000, 3490000, 36, N'Loa JBL Charge 5 chống nước IP67, bass mạnh mẽ.', 'https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'Logitech MX Master 3', 6, 9, 1, 1500000, 2190000, 50, N'Chuột không dây Logitech MX Master 3 cho công việc chuyên nghiệp.', 'https://images.unsplash.com/photo-1587825140708-5a03b97a5b6a?auto=format&fit=crop&w=1200&q=80', N'Đang bán'),
                        (N'Anker 737 Power Bank', 6, 10, 1, 600000, 890000, 60, N'Sạc dự phòng Anker 737, dung lượng lớn và sạc nhanh.', 'https://images.unsplash.com/photo-1503602642458-232111445657?auto=format&fit=crop&w=1200&q=80', N'Đang bán');
                END
            ");

            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.KhachHang)
                BEGIN
                    INSERT INTO dbo.KhachHang (HoTen, SoDienThoai, Email, DiaChi, DiemTichLuy) VALUES
                        (N'Nguyễn Văn A', '0901234567', 'khachhang1@novatech.vn', N'123 Lê Lợi, Quận 1, TP.HCM', 120);
                END
            ");

            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.DonHang)
                BEGIN
                    DECLARE @customerId INT = (SELECT TOP 1 MaKhachHang FROM dbo.KhachHang ORDER BY MaKhachHang);
                    DECLARE @order1 INT;
                    INSERT INTO dbo.DonHang (MaKhachHang, MaNhanVien, NgayDat, TongTien, TrangThai, PhuongThucThanhToan, GhiChu)
                    VALUES (@customerId, NULL, DATEADD(day, -5, GETDATE()), 43990000, N'Đã duyệt', N'COD', N'Giao hàng nhanh trong 2 ngày.');
                    SET @order1 = SCOPE_IDENTITY();

                    DECLARE @order2 INT;
                    INSERT INTO dbo.DonHang (MaKhachHang, MaNhanVien, NgayDat, TongTien, TrangThai, PhuongThucThanhToan, GhiChu)
                    VALUES (@customerId, NULL, DATEADD(day, -2, GETDATE()), 21990000, N'Đang vận chuyển', N'Tiền mặt', N'Đã duyệt đơn. Đang giao đến kho vận chuyển.');
                    SET @order2 = SCOPE_IDENTITY();

                    INSERT INTO dbo.ChiTietDonHang (MaDonHang, MaSanPham, SoLuong, DonGia)
                    VALUES
                        (@order1, (SELECT TOP 1 MaSanPham FROM dbo.SanPham WHERE TenSanPham = N'iPhone 15 Pro Max 256GB'), 1, 31990000),
                        (@order1, (SELECT TOP 1 MaSanPham FROM dbo.SanPham WHERE TenSanPham = N'Logitech MX Master 3'), 2, 2190000),
                        (@order2, (SELECT TOP 1 MaSanPham FROM dbo.SanPham WHERE TenSanPham = N'Samsung Galaxy Tab S9 Ultra'), 1, 21990000);
                END
            ");

            // Seed default roles if Role table is empty
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.Role)
                BEGIN
                    INSERT INTO dbo.Role (Name, Description) VALUES 
                    (N'Admin', N'Quản trị viên toàn hệ thống'),
                    (N'Quản lý kho', N'Quản lý hàng hóa, tồn kho, xuất nhập kho'),
                    (N'Nhân viên bán hàng', N'Xem sản phẩm, bán hàng và quản lý khách hàng'),
                    (N'CSKH', N'Chăm sóc khách hàng và xem đơn hàng');
                END
            ");

            // Seed default permissions if RolePermission table is empty
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission)
                BEGIN
                    -- Admin permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    ('Admin', 'View_Product'), ('Admin', 'Create_Product'), ('Admin', 'Edit_Product'), ('Admin', 'Delete_Product'),
                    ('Admin', 'View_Inventory'), ('Admin', 'Import_Inventory'), ('Admin', 'Export_Inventory'),
                    ('Admin', 'View_Order'), ('Admin', 'Approve_Order'), ('Admin', 'View_Customer'), ('Admin', 'Create_Customer'),
                    ('Admin', 'View_Promotion'), ('Admin', 'View_Employee'), ('Admin', 'Create_Employee'), ('Admin', 'Assign_Role'),
                    ('Admin', 'Delete_Employee'), ('Admin', 'View_Report'), ('Admin', 'View_Setting'), ('Admin', 'Edit_Setting'),
                    ('Admin', 'View_TikTok'), ('Admin', 'Sync_TikTok');

                    -- Quản lý kho permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    (N'Quản lý kho', 'View_Product'), (N'Quản lý kho', 'Create_Product'), (N'Quản lý kho', 'Edit_Product'),
                    (N'Quản lý kho', 'View_Inventory'), (N'Quản lý kho', 'Import_Inventory'), (N'Quản lý kho', 'Export_Inventory');

                    -- Nhân viên bán hàng permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    (N'Nhân viên bán hàng', 'View_Product'),
                    (N'Nhân viên bán hàng', 'View_Order'), (N'Nhân viên bán hàng', 'Approve_Order'),
                    (N'Nhân viên bán hàng', 'View_Customer'), (N'Nhân viên bán hàng', 'Create_Customer'),
                    (N'Nhân viên bán hàng', 'View_Promotion');

                    -- CSKH permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    ('CSKH', 'View_Customer'), ('CSKH', 'View_Order');
                END
            ");

            // Seed Export_Report permission (idempotent)
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission WHERE PermissionName = 'Export_Report')
                BEGIN
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES
                    ('Admin', 'Export_Report'),
                    (N'Quản lý kho', 'Export_Report');
                END
            ");

            // Clean up legacy roles in NhanVien table
            ExecuteSql(db, @"
                UPDATE dbo.NhanVien SET VaiTro = N'Nhân viên bán hàng' WHERE VaiTro = N'Nhân viên';
                UPDATE dbo.NhanVien SET VaiTro = N'Quản lý kho' WHERE VaiTro = N'Nhân viên kho';
                UPDATE dbo.NhanVien SET VaiTro = N'Admin' WHERE VaiTro = N'Quản lý';
                UPDATE dbo.NhanVien SET TrangThai = N'Hoạt động' WHERE TrangThai IS NULL OR TrangThai = '';
            ");

            // Dynamic C# migrations: Hashing passwords and migrating user roles
            try
            {
                var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<NhanVien>();
                
                // Seed demo accounts if they don't exist
                var demoAccounts = new List<NhanVien>
                {
                    new NhanVien { HoTen = "Super Admin Demo", Email = "admin@novatech.vn", SoDienThoai = "0911111111", MatKhau = "123", VaiTro = "Admin", TrangThai = "Hoạt động" },
                    new NhanVien { HoTen = "Bán Hàng Demo", Email = "sale@novatech.vn", SoDienThoai = "0922222222", MatKhau = "123", VaiTro = "Nhân viên bán hàng", TrangThai = "Hoạt động" },
                    new NhanVien { HoTen = "Nhân Viên Kho Demo", Email = "kho@novatech.vn", SoDienThoai = "0933333333", MatKhau = "123", VaiTro = "Quản lý kho", TrangThai = "Hoạt động" }
                };

                foreach (var demo in demoAccounts)
                {
                    if (!context.NhanViens.Any(e => e.Email == demo.Email))
                    {
                        demo.MatKhau = passwordHasher.HashPassword(demo, demo.MatKhau);
                        context.NhanViens.Add(demo);
                    }
                }
                context.SaveChanges();

                var staff = context.NhanViens.ToList();
                foreach (var emp in staff)
                {
                    if (string.IsNullOrEmpty(emp.MatKhau)) continue;

                    // Robust check if password is not hashed yet
                    bool isHashed = false;
                    if (emp.MatKhau.Length >= 60 && emp.MatKhau.Length <= 200)
                    {
                        try
                        {
                            var bytes = Convert.FromBase64String(emp.MatKhau);
                            if (bytes.Length > 40)
                            {
                                isHashed = true;
                            }
                        }
                        catch { }
                    }

                    if (!isHashed)
                    {
                        emp.MatKhau = passwordHasher.HashPassword(emp, emp.MatKhau);
                    }
                }
                context.SaveChanges();

                var rolesInDb = context.Roles.ToList();
                foreach (var emp in staff)
                {
                    if (string.IsNullOrEmpty(emp.VaiTro)) continue;

                    var empRoles = emp.VaiTro.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim())
                        .ToList();

                    foreach (var roleName in empRoles)
                    {
                        var role = rolesInDb.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
                        if (role != null)
                        {
                            bool mappingExists = context.NhanVienRoles.Any(nr => nr.MaNhanVien == emp.MaNhanVien && nr.RoleId == role.Id);
                            if (!mappingExists)
                            {
                                context.NhanVienRoles.Add(new NhanVienRole
                                {
                                    MaNhanVien = emp.MaNhanVien,
                                    RoleId = role.Id
                                });
                            }
                        }
                    }
                }
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error running C# migrations in DbInitializer: " + ex.Message);
            }
        }

        private static void ExecuteSql(DatabaseFacade db, string sql)
        {
            try
            {
                db.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating table: " + ex.Message);
            }
        }
    }
}
