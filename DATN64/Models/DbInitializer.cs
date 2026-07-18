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
            // Alter SanPham to add SKU field if not exists
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SanPham') AND name = 'SKU')
                    ALTER TABLE dbo.SanPham ADD SKU NVARCHAR(100) NULL;
            ");
            // Assign SKU codes to all base products that don't have one yet
            ExecuteSql(db, @"
                -- iPhone 15 base models
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-128G-BLK'  WHERE TenSanPham = N'iPhone 15 Thường'      AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-128G-NAT'  WHERE TenSanPham = N'iPhone 15 Pro'         AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-256G-NAT' WHERE TenSanPham = N'iPhone 15 Pro Max'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-256G-NAT' WHERE TenSanPham = N'iPhone 15 Pro Max 256GB' AND (SKU IS NULL OR SKU = '');

                -- Samsung Galaxy S24 base models
                UPDATE dbo.SanPham SET SKU = 'SS-S24-256G-YLW'   WHERE TenSanPham = N'Samsung Galaxy S24'             AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24U-256G-GRY'   WHERE TenSanPham = N'Samsung Galaxy S24 Ultra'      AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24U-256G-GRY'   WHERE TenSanPham = N'Samsung Galaxy S24 Ultra 256GB' AND (SKU IS NULL OR SKU = '');

                -- Xiaomi
                UPDATE dbo.SanPham SET SKU = 'XI-XM14P-256G-BLK'  WHERE TenSanPham = N'Xiaomi 14 Pro'          AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'XI-XM14-128G-BLK'   WHERE TenSanPham = N'Xiaomi 14'              AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'XI-XM13P-256G-BLK'  WHERE TenSanPham = N'Xiaomi 13 Pro'         AND (SKU IS NULL OR SKU = '');

                -- MacBook / Apple Laptop
                UPDATE dbo.SanPham SET SKU = 'AP-MBP14M3-16G-SLV' WHERE TenSanPham = N'MacBook Pro 14 inch M3'  AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-MBA15M3-8G-SLV'   WHERE TenSanPham = N'MacBook Air 15 inch M3' AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-MBP16M3-16G-SLV'  WHERE TenSanPham = N'MacBook Pro 16 inch M3' AND (SKU IS NULL OR SKU = '');

                -- Dell Laptop
                UPDATE dbo.SanPham SET SKU = 'DL-XPS13-OLED-PLT'  WHERE TenSanPham = N'Dell XPS 13 OLED'       AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'DL-INS15-FHD-BLK'   WHERE TenSanPham = N'Dell Inspiron 15'       AND (SKU IS NULL OR SKU = '');

                -- ASUS Laptop
                UPDATE dbo.SanPham SET SKU = 'AS-ROG16-FHD-BLK'   WHERE TenSanPham = N'ASUS ROG Strix G16'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AS-ZEN14-FHD-BLK'   WHERE TenSanPham = N'ASUS ZenBook 14'        AND (SKU IS NULL OR SKU = '');

                -- iPad / Apple Tablet
                UPDATE dbo.SanPham SET SKU = 'AP-IPP129-256G-SLV' WHERE TenSanPham = N'iPad Pro 12.9 inch'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IPAIR-128G-PNK'  WHERE TenSanPham = N'iPad Air'               AND (SKU IS NULL OR SKU = '');

                -- Samsung Tablet
                UPDATE dbo.SanPham SET SKU = 'SS-TABS9U-256G-GRY' WHERE TenSanPham = N'Samsung Galaxy Tab S9 Ultra' AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-TABA9-128G-SLV'  WHERE TenSanPham = N'Samsung Galaxy Tab A9'       AND (SKU IS NULL OR SKU = '');

                -- Sony Headphones
                UPDATE dbo.SanPham SET SKU = 'SN-WH1000XM5-BLK'  WHERE TenSanPham = N'Sony WH-1000XM5'        AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SN-WF1000XM5-BLK'  WHERE TenSanPham = N'Sony WF-1000XM5'        AND (SKU IS NULL OR SKU = '');

                -- JBL Speaker
                UPDATE dbo.SanPham SET SKU = 'JBL-CHG5-BLK'      WHERE TenSanPham = N'JBL Charge 5'           AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'JBL-FLP6-BLK'      WHERE TenSanPham = N'JBL Flip 6'             AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'JBL-BRCK-BLK'      WHERE TenSanPham = N'JBL Boombox'            AND (SKU IS NULL OR SKU = '');

                -- Logitech Accessories
                UPDATE dbo.SanPham SET SKU = 'LG-MXM3-BLK'       WHERE TenSanPham = N'Logitech MX Master 3'   AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'LG-MKYS-BLK'       WHERE TenSanPham = N'Logitech MX Keys S'     AND (SKU IS NULL OR SKU = '');

                -- Anker Accessories
                UPDATE dbo.SanPham SET SKU = 'AN-PB737-BLK'      WHERE TenSanPham = N'Anker 737 Power Bank'   AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AN-CHG65W-WHT'     WHERE TenSanPham = N'Anker 65W Charger'      AND (SKU IS NULL OR SKU = '');

                -- Oppo
                UPDATE dbo.SanPham SET SKU = 'OP-F25P-256G-BLK'  WHERE TenSanPham = N'Oppo Find X7 Pro'       AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'OP-A79-128G-BLU'   WHERE TenSanPham = N'Oppo A79'               AND (SKU IS NULL OR SKU = '');

                -- iPhone variants by naming pattern (auto-correct already-seeded variants)
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-128G-BLK'  WHERE TenSanPham = N'iPhone 15 Thường 128GB Đen'      AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-128G-PNK'  WHERE TenSanPham = N'iPhone 15 Thường 128GB Hồng'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-128G-BLU'  WHERE TenSanPham = N'iPhone 15 Thường 128GB Xanh Dương' AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-128G-GRN'  WHERE TenSanPham = N'iPhone 15 Thường 128GB Xanh Lá'  AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-128G-YLW'  WHERE TenSanPham = N'iPhone 15 Thường 128GB Vàng'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-256G-BLK'  WHERE TenSanPham = N'iPhone 15 Thường 256GB Đen'      AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-256G-PNK'  WHERE TenSanPham = N'iPhone 15 Thường 256GB Hồng'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-256G-BLU'  WHERE TenSanPham = N'iPhone 15 Thường 256GB Xanh Dương' AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-256G-GRN'  WHERE TenSanPham = N'iPhone 15 Thường 256GB Xanh Lá'  AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-256G-YLW'  WHERE TenSanPham = N'iPhone 15 Thường 256GB Vàng'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-512G-BLK'  WHERE TenSanPham = N'iPhone 15 Thường 512GB Đen'      AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-512G-PNK'  WHERE TenSanPham = N'iPhone 15 Thường 512GB Hồng'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15T-512G-BLU'  WHERE TenSanPham = N'iPhone 15 Thường 512GB Xanh Dương' AND (SKU IS NULL OR SKU = '');

                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-128G-NAT'  WHERE TenSanPham = N'iPhone 15 Pro 128GB Titan Tự Nhiên' AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-128G-BLU'  WHERE TenSanPham = N'iPhone 15 Pro 128GB Titan Xanh'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-128G-WHT'  WHERE TenSanPham = N'iPhone 15 Pro 128GB Titan Trắng'   AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-128G-BLK'  WHERE TenSanPham = N'iPhone 15 Pro 128GB Titan Đen'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-256G-NAT'  WHERE TenSanPham = N'iPhone 15 Pro 256GB Titan Tự Nhiên' AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-256G-BLU'  WHERE TenSanPham = N'iPhone 15 Pro 256GB Titan Xanh'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-256G-WHT'  WHERE TenSanPham = N'iPhone 15 Pro 256GB Titan Trắng'   AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-256G-BLK'  WHERE TenSanPham = N'iPhone 15 Pro 256GB Titan Đen'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-512G-NAT'  WHERE TenSanPham = N'iPhone 15 Pro 512GB Titan Tự Nhiên' AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-512G-BLU'  WHERE TenSanPham = N'iPhone 15 Pro 512GB Titan Xanh'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-512G-WHT'  WHERE TenSanPham = N'iPhone 15 Pro 512GB Titan Trắng'   AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-512G-BLK'  WHERE TenSanPham = N'iPhone 15 Pro 512GB Titan Đen'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-1T-NAT'    WHERE TenSanPham = N'iPhone 15 Pro 1TB Titan Tự Nhiên'  AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-1T-BLU'    WHERE TenSanPham = N'iPhone 15 Pro 1TB Titan Xanh'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-1T-WHT'    WHERE TenSanPham = N'iPhone 15 Pro 1TB Titan Trắng'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15P-1T-BLK'    WHERE TenSanPham = N'iPhone 15 Pro 1TB Titan Đen'     AND (SKU IS NULL OR SKU = '');

                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-256G-NAT' WHERE TenSanPham = N'iPhone 15 Pro Max 256GB Titan Tự Nhiên' AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-256G-BLU' WHERE TenSanPham = N'iPhone 15 Pro Max 256GB Titan Xanh'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-256G-WHT' WHERE TenSanPham = N'iPhone 15 Pro Max 256GB Titan Trắng'   AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-256G-BLK' WHERE TenSanPham = N'iPhone 15 Pro Max 256GB Titan Đen'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-512G-NAT' WHERE TenSanPham = N'iPhone 15 Pro Max 512GB Titan Tự Nhiên' AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-512G-BLU' WHERE TenSanPham = N'iPhone 15 Pro Max 512GB Titan Xanh'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-512G-WHT' WHERE TenSanPham = N'iPhone 15 Pro Max 512GB Titan Trắng'   AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-512G-BLK' WHERE TenSanPham = N'iPhone 15 Pro Max 512GB Titan Đen'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-1T-NAT'   WHERE TenSanPham = N'iPhone 15 Pro Max 1TB Titan Tự Nhiên'  AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-1T-BLU'   WHERE TenSanPham = N'iPhone 15 Pro Max 1TB Titan Xanh'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-1T-WHT'   WHERE TenSanPham = N'iPhone 15 Pro Max 1TB Titan Trắng'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'AP-IP15PM-1T-BLK'   WHERE TenSanPham = N'iPhone 15 Pro Max 1TB Titan Đen'     AND (SKU IS NULL OR SKU = '');

                -- Samsung Galaxy S24 variants
                UPDATE dbo.SanPham SET SKU = 'SS-S24-256G-YLW'    WHERE TenSanPham = N'Samsung Galaxy S24 256GB Vàng'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24-256G-PUR'    WHERE TenSanPham = N'Samsung Galaxy S24 256GB Tím'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24-256G-GRY'    WHERE TenSanPham = N'Samsung Galaxy S24 256GB Xám'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24-256G-BLK'    WHERE TenSanPham = N'Samsung Galaxy S24 256GB Đen'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24-512G-GRY'    WHERE TenSanPham = N'Samsung Galaxy S24 512GB Xám'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24-512G-BLK'    WHERE TenSanPham = N'Samsung Galaxy S24 512GB Đen'     AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24U-256G-YLW'   WHERE TenSanPham = N'Samsung Galaxy S24 Ultra 256GB Titan Vàng' AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24U-256G-PUR'   WHERE TenSanPham = N'Samsung Galaxy S24 Ultra 256GB Titan Tím'  AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24U-256G-GRY'   WHERE TenSanPham = N'Samsung Galaxy S24 Ultra 256GB Titan Xám'  AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24U-256G-BLK'   WHERE TenSanPham = N'Samsung Galaxy S24 Ultra 256GB Titan Đen'  AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24U-512G-GRY'   WHERE TenSanPham = N'Samsung Galaxy S24 Ultra 512GB Titan Xám'  AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24U-512G-BLK'   WHERE TenSanPham = N'Samsung Galaxy S24 Ultra 512GB Titan Đen'  AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24U-1T-GRY'     WHERE TenSanPham = N'Samsung Galaxy S24 Ultra 1TB Titan Xám'    AND (SKU IS NULL OR SKU = '');
                UPDATE dbo.SanPham SET SKU = 'SS-S24U-1T-BLK'     WHERE TenSanPham = N'Samsung Galaxy S24 Ultra 1TB Titan Đen'    AND (SKU IS NULL OR SKU = '');
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

            // Create YeuThich table
            ExecuteSql(db, @"
                IF OBJECT_ID('dbo.YeuThich', 'U') IS NULL
                CREATE TABLE dbo.YeuThich (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    MaKhachHang INT NOT NULL,
                    MaSanPham INT NOT NULL,
                    NgayTao DATETIME NOT NULL DEFAULT GETDATE()
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

            // Add TrangThai and CongNoId columns to SoQuy
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SoQuy') AND name = 'TrangThai')
                    ALTER TABLE dbo.SoQuy ADD TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Đã duyệt';
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SoQuy') AND name = 'CongNoId')
                    ALTER TABLE dbo.SoQuy ADD CongNoId INT NULL;
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
                    (N'Super Admin', N'Quản trị tối cao, toàn quyền hệ thống'),
                    (N'Admin', N'Quản trị viên toàn hệ thống'),
                    (N'Quản lý cửa hàng', N'Quản lý hoạt động bán hàng, nhân sự cửa hàng'),
                    (N'Quản lý kho', N'Quản lý hàng hóa, tồn kho, xuất nhập kho'),
                    (N'Nhân viên bán hàng', N'Xem sản phẩm, bán hàng và quản lý khách hàng'),
                    (N'Nhân viên kho', N'Thực hiện nhập xuất kho theo phiếu'),
                    (N'Kế toán', N'Quản lý tài chính, sổ quỹ, công nợ và báo cáo'),
                    (N'Marketing', N'Quản lý khuyến mãi, voucher và dữ liệu khách hàng'),
                    (N'CSKH', N'Chăm sóc khách hàng và xem đơn hàng');
                END
            ");

            // Idempotent: Add missing roles if they were already seeded partially
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE Name = N'Super Admin')
                    INSERT INTO dbo.Role (Name, Description) VALUES (N'Super Admin', N'Quản trị tối cao, toàn quyền hệ thống');
                IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE Name = N'Quản lý cửa hàng')
                    INSERT INTO dbo.Role (Name, Description) VALUES (N'Quản lý cửa hàng', N'Quản lý hoạt động bán hàng, nhân sự cửa hàng');
                IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE Name = N'Nhân viên kho')
                    INSERT INTO dbo.Role (Name, Description) VALUES (N'Nhân viên kho', N'Thực hiện nhập xuất kho theo phiếu');
                IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE Name = N'Kế toán')
                    INSERT INTO dbo.Role (Name, Description) VALUES (N'Kế toán', N'Quản lý tài chính, sổ quỹ, công nợ và báo cáo');
                IF NOT EXISTS (SELECT 1 FROM dbo.Role WHERE Name = N'Marketing')
                    INSERT INTO dbo.Role (Name, Description) VALUES (N'Marketing', N'Quản lý khuyến mãi, voucher và dữ liệu khách hàng');
            ");

            // Seed default permissions if RolePermission table is empty
            ExecuteSql(db, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission)
                BEGIN
                    -- Admin permissions (full access)
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    ('Admin', 'View_Product'), ('Admin', 'Create_Product'), ('Admin', 'Edit_Product'), ('Admin', 'Delete_Product'),
                    ('Admin', 'View_Inventory'), ('Admin', 'Import_Inventory'), ('Admin', 'Export_Inventory'),
                    ('Admin', 'View_Order'), ('Admin', 'Approve_Order'), ('Admin', 'View_Customer'), ('Admin', 'Create_Customer'),
                    ('Admin', 'View_Promotion'), ('Admin', 'View_Employee'), ('Admin', 'Create_Employee'), ('Admin', 'Assign_Role'),
                    ('Admin', 'Delete_Employee'), ('Admin', 'View_Report'), ('Admin', 'Export_Report'),
                    ('Admin', 'View_Setting'), ('Admin', 'Edit_Setting'),
                    ('Admin', 'View_TikTok'), ('Admin', 'Sync_TikTok'),
                    ('Admin', 'View_Accounting');

                    -- Quản lý cửa hàng permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    (N'Quản lý cửa hàng', 'View_Product'), (N'Quản lý cửa hàng', 'Create_Product'), (N'Quản lý cửa hàng', 'Edit_Product'),
                    (N'Quản lý cửa hàng', 'View_Inventory'), (N'Quản lý cửa hàng', 'Import_Inventory'), (N'Quản lý cửa hàng', 'Export_Inventory'),
                    (N'Quản lý cửa hàng', 'View_Order'), (N'Quản lý cửa hàng', 'Approve_Order'),
                    (N'Quản lý cửa hàng', 'View_Customer'), (N'Quản lý cửa hàng', 'Create_Customer'),
                    (N'Quản lý cửa hàng', 'View_Promotion'),
                    (N'Quản lý cửa hàng', 'View_Employee'),
                    (N'Quản lý cửa hàng', 'View_Report'), (N'Quản lý cửa hàng', 'Export_Report'),
                    (N'Quản lý cửa hàng', 'View_Accounting'), (N'Quản lý cửa hàng', 'View_TikTok');

                    -- Quản lý kho permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    (N'Quản lý kho', 'View_Product'), (N'Quản lý kho', 'Create_Product'), (N'Quản lý kho', 'Edit_Product'),
                    (N'Quản lý kho', 'View_Inventory'), (N'Quản lý kho', 'Import_Inventory'), (N'Quản lý kho', 'Export_Inventory'),
                    (N'Quản lý kho', 'View_Report'), (N'Quản lý kho', 'Export_Report'),
                    (N'Quản lý kho', 'Approve_Order');

                    -- Nhân viên kho permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    (N'Nhân viên kho', 'View_Product'),
                    (N'Nhân viên kho', 'View_Inventory'), (N'Nhân viên kho', 'Import_Inventory'), (N'Nhân viên kho', 'Export_Inventory');

                    -- Nhân viên bán hàng permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    (N'Nhân viên bán hàng', 'View_Product'),
                    (N'Nhân viên bán hàng', 'View_Order'), (N'Nhân viên bán hàng', 'Approve_Order'),
                    (N'Nhân viên bán hàng', 'View_Customer'), (N'Nhân viên bán hàng', 'Create_Customer'),
                    (N'Nhân viên bán hàng', 'View_Promotion');

                    -- Kế toán permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    (N'Kế toán', 'View_Accounting'),
                    (N'Kế toán', 'View_Report'), (N'Kế toán', 'Export_Report'),
                    (N'Kế toán', 'View_Order'),
                    (N'Kế toán', 'View_Customer');

                    -- Marketing permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    (N'Marketing', 'View_Promotion'),
                    (N'Marketing', 'View_Customer'), (N'Marketing', 'Create_Customer'),
                    (N'Marketing', 'View_Report'),
                    (N'Marketing', 'View_Product');

                    -- CSKH permissions
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES 
                    ('CSKH', 'View_Customer'), ('CSKH', 'View_Order');
                END
            ");

            // Idempotent: Seed missing permissions for new roles on existing databases
            ExecuteSql(db, @"
                -- Kế toán
                IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission WHERE RoleName = N'Kế toán' AND PermissionName = 'View_Accounting')
                BEGIN
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES
                    (N'Kế toán', 'View_Accounting'),
                    (N'Kế toán', 'View_Report'), (N'Kế toán', 'Export_Report'),
                    (N'Kế toán', 'View_Order'),
                    (N'Kế toán', 'View_Customer');
                END

                -- Marketing
                IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission WHERE RoleName = N'Marketing' AND PermissionName = 'View_Promotion')
                BEGIN
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES
                    (N'Marketing', 'View_Promotion'),
                    (N'Marketing', 'View_Customer'), (N'Marketing', 'Create_Customer'),
                    (N'Marketing', 'View_Report'),
                    (N'Marketing', 'View_Product');
                END

                -- Quản lý cửa hàng
                IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission WHERE RoleName = N'Quản lý cửa hàng' AND PermissionName = 'View_Order')
                BEGIN
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES
                    (N'Quản lý cửa hàng', 'View_Product'), (N'Quản lý cửa hàng', 'Create_Product'), (N'Quản lý cửa hàng', 'Edit_Product'),
                    (N'Quản lý cửa hàng', 'View_Inventory'), (N'Quản lý cửa hàng', 'Import_Inventory'), (N'Quản lý cửa hàng', 'Export_Inventory'),
                    (N'Quản lý cửa hàng', 'View_Order'), (N'Quản lý cửa hàng', 'Approve_Order'),
                    (N'Quản lý cửa hàng', 'View_Customer'), (N'Quản lý cửa hàng', 'Create_Customer'),
                    (N'Quản lý cửa hàng', 'View_Promotion'),
                    (N'Quản lý cửa hàng', 'View_Employee'),
                    (N'Quản lý cửa hàng', 'View_Report'), (N'Quản lý cửa hàng', 'Export_Report'),
                    (N'Quản lý cửa hàng', 'View_Accounting'), (N'Quản lý cửa hàng', 'View_TikTok');
                END

                -- Nhân viên kho
                IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission WHERE RoleName = N'Nhân viên kho' AND PermissionName = 'View_Inventory')
                BEGIN
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES
                    (N'Nhân viên kho', 'View_Product'),
                    (N'Nhân viên kho', 'View_Inventory'), (N'Nhân viên kho', 'Import_Inventory'), (N'Nhân viên kho', 'Export_Inventory');
                END

                -- Quản lý kho: thêm Approve_Order nếu thiếu
                IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission WHERE RoleName = N'Quản lý kho' AND PermissionName = 'Approve_Order')
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES (N'Quản lý kho', 'Approve_Order');

                -- Admin: thêm View_Accounting nếu thiếu
                IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission WHERE RoleName = 'Admin' AND PermissionName = 'View_Accounting')
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES ('Admin', 'View_Accounting');

                -- Admin: thêm Export_Report nếu thiếu
                IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission WHERE RoleName = 'Admin' AND PermissionName = 'Export_Report')
                    INSERT INTO dbo.RolePermission (RoleName, PermissionName) VALUES ('Admin', 'Export_Report');
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

                // Idempotently seed all permissions for Admin role
                try
                {
                    var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
                    if (adminRole != null)
                    {
                        var existingAdminPerms = context.RolePermissions
                            .Where(rp => rp.RoleName == "Admin")
                            .Select(rp => rp.PermissionName)
                            .ToList();

                        foreach (var perm in DATN64.Helpers.PermissionConstants.All)
                        {
                            if (!existingAdminPerms.Contains(perm))
                            {
                                context.RolePermissions.Add(new RolePermission
                                {
                                    RoleName = "Admin",
                                    PermissionName = perm
                                });
                            }
                        }
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error seeding Admin permissions: " + ex.Message);
                }

                SeedVariants(context);
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

        private static void SeedVariants(AppDbContext context)
        {
            var catId = context.DanhMucs.FirstOrDefault(d => d.TenDanhMuc == "Điện thoại")?.MaDanhMuc ?? 1;
            var brandAppleId = context.ThuongHieus.FirstOrDefault(t => t.TenThuongHieu == "Apple")?.MaThuongHieu ?? 1;
            var brandSamsungId = context.ThuongHieus.FirstOrDefault(t => t.TenThuongHieu == "Samsung")?.MaThuongHieu ?? 2;
            var nccAppleId = context.NhaCungCaps.FirstOrDefault(n => n.TenNCC.Contains("Apple"))?.MaNCC ?? 3;
            var nccSamsungId = context.NhaCungCaps.FirstOrDefault(n => n.TenNCC.Contains("Samsung"))?.MaNCC ?? 2;

            // SKU Helper
            string GetProductSKU(string model, string storage, string color)
            {
                string brand = model.Contains("Samsung", StringComparison.OrdinalIgnoreCase) ? "SS" : "AP";
                string modelCode = "";
                if (model.Contains("Pro Max", StringComparison.OrdinalIgnoreCase)) modelCode = "IP15PM";
                else if (model.Contains("Pro", StringComparison.OrdinalIgnoreCase)) modelCode = "IP15P";
                else if (model.Contains("Thường", StringComparison.OrdinalIgnoreCase) || model.Contains("iPhone 15", StringComparison.OrdinalIgnoreCase)) modelCode = "IP15T";
                else if (model.Contains("Ultra", StringComparison.OrdinalIgnoreCase)) modelCode = "S24U";
                else modelCode = "S24";

                string storageCode = storage.Replace(" ", "").ToUpper();

                string colorCode = "BLK";
                if (color.Contains("Hồng")) colorCode = "PNK";
                else if (color.Contains("Xanh Dương") || color.Contains("Xanh")) colorCode = "BLU";
                else if (color.Contains("Xanh Lá")) colorCode = "GRN";
                else if (color.Contains("Vàng")) colorCode = "YLW";
                else if (color.Contains("Tự Nhiên")) colorCode = "NAT";
                else if (color.Contains("Trắng")) colorCode = "WHT";
                else if (color.Contains("Xám")) colorCode = "GRY";
                else if (color.Contains("Tím")) colorCode = "PUR";

                return $"{brand}-{modelCode}-{storageCode}-{colorCode}";
            }

            // Clean up duplicate base models if any exist
            void CleanupDuplicates(string productName)
            {
                var list = context.SanPhams.Where(p => p.TenSanPham == productName).ToList();
                if (list.Count > 1)
                {
                    for (int i = 1; i < list.Count; i++)
                    {
                        context.SanPhams.Remove(list[i]);
                    }
                    context.SaveChanges();
                }
            }
            CleanupDuplicates("iPhone 15 Thường");
            CleanupDuplicates("iPhone 15 Pro");
            CleanupDuplicates("iPhone 15 Pro Max");
            CleanupDuplicates("Samsung Galaxy S24");
            CleanupDuplicates("Samsung Galaxy S24 Ultra");

            // ── Fix sản phẩm dạng cũ: "iPhone 15 128GB Hồng" → "iPhone 15 Thường 128GB Hồng" + Biến thể ──
            // Các sản phẩm có tên cũ chứa "GB" hoặc "TB" nhưng TrangThai = "Đang bán" cần được reclassify
            var oldFormatProducts = context.SanPhams
                .Where(p => p.TrangThai == "Đang bán"
                         && (p.TenSanPham.Contains("128GB") || p.TenSanPham.Contains("256GB")
                          || p.TenSanPham.Contains("512GB") || p.TenSanPham.Contains("1TB")))
                .ToList();

            foreach (var old in oldFormatProducts)
            {
                // "iPhone 15 128GB Hồng" → "iPhone 15 Thường 128GB Hồng"
                if (old.TenSanPham.StartsWith("iPhone 15 ") &&
                    !old.TenSanPham.StartsWith("iPhone 15 Thường") &&
                    !old.TenSanPham.StartsWith("iPhone 15 Pro") &&
                    !old.TenSanPham.StartsWith("iPhone 15 Plus"))
                {
                    var suffix = old.TenSanPham.Substring("iPhone 15 ".Length); // "128GB Hồng"
                    var newName = "iPhone 15 Thường " + suffix;
                    // Chỉ đổi tên nếu chưa có sản phẩm cùng tên mới
                    if (!context.SanPhams.Any(p => p.TenSanPham == newName && p.MaSanPham != old.MaSanPham))
                    {
                        old.TenSanPham = newName;
                    }
                }
                old.TrangThai = "Biến thể";
            }
            context.SaveChanges();

            // Rename and upsert original base products so they integrate cleanly
            var originalIphone = context.SanPhams.FirstOrDefault(p => p.TenSanPham == "iPhone 15" || p.TenSanPham == "iPhone 15 128GB Đen" || p.TenSanPham == "iPhone 15 Thường");
            if (originalIphone != null)
            {
                originalIphone.TenSanPham = "iPhone 15 Thường";
                originalIphone.GiaBan = 19990000;
                originalIphone.GiaNhap = 16000000;
                originalIphone.HinhAnh = "/uploads/iphone15_thuong.png";
                originalIphone.TrangThai = "Đang bán";
                originalIphone.SKU = GetProductSKU("iPhone 15 Thường", "128GB", "Đen");
            }
            else
            {
                context.SanPhams.Add(new SanPham
                {
                    TenSanPham = "iPhone 15 Thường",
                    MaDanhMuc = catId,
                    MaThuongHieu = brandAppleId,
                    MaNCC = nccAppleId,
                    GiaNhap = 16000000,
                    GiaBan = 19990000,
                    SoLuongTon = 50,
                    MoTa = "iPhone 15 Thường chính hãng Apple. Màn hình Dynamic Island, camera 48MP đỉnh cao, cổng kết nối USB-C.",
                    HinhAnh = "/uploads/iphone15_thuong.png",
                    TrangThai = "Đang bán",
                    SKU = GetProductSKU("iPhone 15 Thường", "128GB", "Đen")
                });
            }

            var originalS24Ultra = context.SanPhams.FirstOrDefault(p => p.TenSanPham == "Samsung Galaxy S24 Ultra" || p.TenSanPham == "Samsung Galaxy S24 Ultra 256GB Titan Xám");
            if (originalS24Ultra != null)
            {
                originalS24Ultra.TenSanPham = "Samsung Galaxy S24 Ultra";
                originalS24Ultra.GiaBan = 25990000;
                originalS24Ultra.GiaNhap = 20000000;
                originalS24Ultra.HinhAnh = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?auto=format&fit=crop&w=600&q=80";
                originalS24Ultra.TrangThai = "Đang bán";
                originalS24Ultra.SKU = GetProductSKU("Samsung Galaxy S24 Ultra", "256GB", "Titan Xám");
            }
            else
            {
                context.SanPhams.Add(new SanPham
                {
                    TenSanPham = "Samsung Galaxy S24 Ultra",
                    MaDanhMuc = catId,
                    MaThuongHieu = brandSamsungId,
                    MaNCC = nccSamsungId,
                    GiaNhap = 20000000,
                    GiaBan = 25990000,
                    SoLuongTon = 45,
                    MoTa = "Samsung Galaxy S24 Ultra chính hãng Samsung. Màn hình Dynamic AMOLED 2X, camera 200MP tích hợp Galaxy AI và bút S-Pen.",
                    HinhAnh = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?auto=format&fit=crop&w=600&q=80",
                    TrangThai = "Đang bán",
                    SKU = GetProductSKU("Samsung Galaxy S24 Ultra", "256GB", "Titan Xám")
                });
            }

            var originalIphonePro = context.SanPhams.FirstOrDefault(p => p.TenSanPham == "iPhone 15 Pro");
            if (originalIphonePro != null)
            {
                originalIphonePro.GiaBan = 24990000;
                originalIphonePro.HinhAnh = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=600&q=80";
                originalIphonePro.TrangThai = "Đang bán";
                originalIphonePro.SKU = GetProductSKU("iPhone 15 Pro", "128GB", "Titan Tự Nhiên");
            }
            else
            {
                context.SanPhams.Add(new SanPham
                {
                    TenSanPham = "iPhone 15 Pro",
                    MaDanhMuc = catId,
                    MaThuongHieu = brandAppleId,
                    MaNCC = nccAppleId,
                    GiaNhap = 20000000,
                    GiaBan = 24990000,
                    SoLuongTon = 50,
                    MoTa = "iPhone 15 Pro chính hãng Apple. Khung Titanium siêu bền nhẹ, chip A17 Pro siêu mạnh mẽ.",
                    HinhAnh = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=600&q=80",
                    TrangThai = "Đang bán",
                    SKU = GetProductSKU("iPhone 15 Pro", "128GB", "Titan Tự Nhiên")
                });
            }

            var originalIphoneProMax = context.SanPhams.FirstOrDefault(p => p.TenSanPham == "iPhone 15 Pro Max");
            if (originalIphoneProMax != null)
            {
                originalIphoneProMax.GiaBan = 29990000;
                originalIphoneProMax.HinhAnh = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=600&q=80";
                originalIphoneProMax.TrangThai = "Đang bán";
                originalIphoneProMax.SKU = GetProductSKU("iPhone 15 Pro Max", "256GB", "Titan Tự Nhiên");
            }
            else
            {
                context.SanPhams.Add(new SanPham
                {
                    TenSanPham = "iPhone 15 Pro Max",
                    MaDanhMuc = catId,
                    MaThuongHieu = brandAppleId,
                    MaNCC = nccAppleId,
                    GiaNhap = 24000000,
                    GiaBan = 29990000,
                    SoLuongTon = 50,
                    MoTa = "iPhone 15 Pro Max chính hãng Apple. Màn hình 6.7 inch sắc nét, camera zoom quang 5x đỉnh cao, chất liệu vỏ Titanium.",
                    HinhAnh = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=600&q=80",
                    TrangThai = "Đang bán",
                    SKU = GetProductSKU("iPhone 15 Pro Max", "256GB", "Titan Tự Nhiên")
                });
            }

            var originalS24 = context.SanPhams.FirstOrDefault(p => p.TenSanPham == "Samsung Galaxy S24");
            if (originalS24 != null)
            {
                originalS24.GiaBan = 18990000;
                originalS24.HinhAnh = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?auto=format&fit=crop&w=600&q=80";
                originalS24.TrangThai = "Đang bán";
                originalS24.SKU = GetProductSKU("Samsung Galaxy S24", "256GB", "Vàng");
            }
            else
            {
                context.SanPhams.Add(new SanPham
                {
                    TenSanPham = "Samsung Galaxy S24",
                    MaDanhMuc = catId,
                    MaThuongHieu = brandSamsungId,
                    MaNCC = nccSamsungId,
                    GiaNhap = 15000000,
                    GiaBan = 18990000,
                    SoLuongTon = 45,
                    MoTa = "Samsung Galaxy S24 chính hãng Samsung. Thiết kế trẻ trung, màn hình sắc nét tích hợp trợ lý ảo Galaxy AI.",
                    HinhAnh = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?auto=format&fit=crop&w=600&q=80",
                    TrangThai = "Đang bán",
                    SKU = GetProductSKU("Samsung Galaxy S24", "256GB", "Vàng")
                });
            }
            context.SaveChanges();

            // Color specific images helper
            string GetIPhone15Image(string model, string color)
            {
                bool isPro = model.Contains("Pro", StringComparison.OrdinalIgnoreCase);
                if (isPro)
                {
                    if (color.Contains("Tự Nhiên")) return "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=600&q=80"; // Natural Titanium
                    if (color.Contains("Xanh")) return "https://images.unsplash.com/photo-1695048065007-ee91055ca132?auto=format&fit=crop&w=600&q=80"; // Blue Titanium
                    if (color.Contains("Trắng")) return "https://images.unsplash.com/photo-1695048132945-f09bf216b0a1?auto=format&fit=crop&w=600&q=80"; // White Titanium
                    return "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=600&q=80"; // Black Titanium
                }
                else
                {
                    return "/uploads/iphone15_thuong.png"; // Use standard iPhone 15 image for all colors
                }
            }

            string GetSamsungS24Image(string model, string color)
            {
                bool isUltra = model.Contains("Ultra", StringComparison.OrdinalIgnoreCase);
                if (isUltra)
                {
                    if (color.Contains("Vàng")) return "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?auto=format&fit=crop&w=600&q=80";
                    if (color.Contains("Tím")) return "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=600&q=80";
                    return "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?auto=format&fit=crop&w=600&q=80"; // Grey/Black
                }
                else
                {
                    if (color.Contains("Vàng")) return "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?auto=format&fit=crop&w=600&q=80";
                    if (color.Contains("Tím")) return "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=600&q=80";
                    return "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?auto=format&fit=crop&w=600&q=80";
                }
            }

            // iPhone 15 variations with market-correct prices
            var iphone15Variants = new List<(string model, string storage, string color, decimal giaNhap, decimal giaBan)>
            {
                // iPhone 15 thường variations
                ("iPhone 15 Thường", "128GB", "Đen", 16000000, 19990000),
                ("iPhone 15 Thường", "128GB", "Hồng", 16000000, 19990000),
                ("iPhone 15 Thường", "128GB", "Xanh Dương", 16000000, 19990000),
                ("iPhone 15 Thường", "128GB", "Xanh Lá", 16000000, 19990000),
                ("iPhone 15 Thường", "128GB", "Vàng", 16000000, 19990000),
                ("iPhone 15 Thường", "256GB", "Đen", 18500000, 22990000),
                ("iPhone 15 Thường", "256GB", "Hồng", 18500000, 22990000),
                ("iPhone 15 Thường", "256GB", "Xanh Dương", 18500000, 22990000),
                ("iPhone 15 Thường", "256GB", "Xanh Lá", 18500000, 22990000),
                ("iPhone 15 Thường", "256GB", "Vàng", 18500000, 22990000),
                ("iPhone 15 Thường", "512GB", "Đen", 23000000, 28990000),
                ("iPhone 15 Thường", "512GB", "Hồng", 23000000, 28990000),
                ("iPhone 15 Thường", "512GB", "Xanh Dương", 23000000, 28990000),

                // iPhone 15 Pro variations
                ("iPhone 15 Pro", "128GB", "Titan Tự Nhiên", 20000000, 24990000),
                ("iPhone 15 Pro", "128GB", "Titan Xanh", 20000000, 24990000),
                ("iPhone 15 Pro", "128GB", "Titan Trắng", 20000000, 24990000),
                ("iPhone 15 Pro", "128GB", "Titan Đen", 20000000, 24990000),
                ("iPhone 15 Pro", "256GB", "Titan Tự Nhiên", 22500000, 27990000),
                ("iPhone 15 Pro", "256GB", "Titan Xanh", 22500000, 27990000),
                ("iPhone 15 Pro", "256GB", "Titan Trắng", 22500000, 27990000),
                ("iPhone 15 Pro", "256GB", "Titan Đen", 22500000, 27990000),
                ("iPhone 15 Pro", "512GB", "Titan Tự Nhiên", 27000000, 33990000),
                ("iPhone 15 Pro", "512GB", "Titan Xanh", 27000000, 33990000),
                ("iPhone 15 Pro", "512GB", "Titan Trắng", 27000000, 33990000),
                ("iPhone 15 Pro", "512GB", "Titan Đen", 27000000, 33990000),
                ("iPhone 15 Pro", "1TB", "Titan Tự Nhiên", 32000000, 39990000),
                ("iPhone 15 Pro", "1TB", "Titan Xanh", 32000000, 39990000),
                ("iPhone 15 Pro", "1TB", "Titan Trắng", 32000000, 39990000),
                ("iPhone 15 Pro", "1TB", "Titan Đen", 32000000, 39990000),

                // iPhone 15 Pro Max variations
                ("iPhone 15 Pro Max", "256GB", "Titan Tự Nhiên", 24000000, 29990000),
                ("iPhone 15 Pro Max", "256GB", "Titan Xanh", 24000000, 29990000),
                ("iPhone 15 Pro Max", "256GB", "Titan Trắng", 24000000, 29990000),
                ("iPhone 15 Pro Max", "256GB", "Titan Đen", 24000000, 29990000),
                ("iPhone 15 Pro Max", "512GB", "Titan Tự Nhiên", 29500000, 36990000),
                ("iPhone 15 Pro Max", "512GB", "Titan Xanh", 29500000, 36990000),
                ("iPhone 15 Pro Max", "512GB", "Titan Trắng", 29500000, 36990000),
                ("iPhone 15 Pro Max", "512GB", "Titan Đen", 29500000, 36990000),
                ("iPhone 15 Pro Max", "1TB", "Titan Tự Nhiên", 34000000, 42990000),
                ("iPhone 15 Pro Max", "1TB", "Titan Xanh", 34000000, 42990000),
                ("iPhone 15 Pro Max", "1TB", "Titan Trắng", 34000000, 42990000),
                ("iPhone 15 Pro Max", "1TB", "Titan Đen", 34000000, 42990000)
            };

            foreach (var item in iphone15Variants)
            {
                string fullName = $"{item.model} {item.storage} {item.color}";
                var img = GetIPhone15Image(item.model, item.color);
                var sku = GetProductSKU(item.model, item.storage, item.color);
                var existing = context.SanPhams.FirstOrDefault(p => p.TenSanPham == fullName);
                if (existing != null)
                {
                    existing.GiaNhap = item.giaNhap;
                    existing.GiaBan = item.giaBan;
                    existing.HinhAnh = img;
                    existing.TrangThai = "Biến thể";
                    existing.SKU = sku;
                }
                else
                {
                    context.SanPhams.Add(new SanPham
                    {
                        TenSanPham = fullName,
                        MaDanhMuc = catId,
                        MaThuongHieu = brandAppleId,
                        MaNCC = nccAppleId,
                        GiaNhap = item.giaNhap,
                        GiaBan = item.giaBan,
                        SoLuongTon = 50,
                        MoTa = $"{item.model} phiên bản {item.storage} màu {item.color} chính hãng Apple.",
                        HinhAnh = img,
                        TrangThai = "Biến thể",
                        SKU = sku
                    });
                }
            }

            // Samsung variants with market-correct prices
            var samsungVariants = new List<(string model, string storage, string color, decimal giaNhap, decimal giaBan)>
            {
                // Samsung Galaxy S24 thường variations
                ("Samsung Galaxy S24", "256GB", "Vàng", 15000000, 18990000),
                ("Samsung Galaxy S24", "256GB", "Tím", 15000000, 18990000),
                ("Samsung Galaxy S24", "256GB", "Xám", 15000000, 18990000),
                ("Samsung Galaxy S24", "256GB", "Đen", 15000000, 18990000),
                ("Samsung Galaxy S24", "512GB", "Xám", 17500000, 21990000),
                ("Samsung Galaxy S24", "512GB", "Đen", 17500000, 21990000),

                // Samsung Galaxy S24 Ultra variations
                ("Samsung Galaxy S24 Ultra", "256GB", "Titan Vàng", 20000000, 25990000),
                ("Samsung Galaxy S24 Ultra", "256GB", "Titan Tím", 20000000, 25990000),
                ("Samsung Galaxy S24 Ultra", "256GB", "Titan Xám", 20000000, 25990000),
                ("Samsung Galaxy S24 Ultra", "256GB", "Titan Đen", 20000000, 25990000),
                ("Samsung Galaxy S24 Ultra", "512GB", "Titan Xám", 23500000, 29990000),
                ("Samsung Galaxy S24 Ultra", "512GB", "Titan Đen", 23500000, 29990000),
                ("Samsung Galaxy S24 Ultra", "1TB", "Titan Xám", 28000000, 35990000),
                ("Samsung Galaxy S24 Ultra", "1TB", "Titan Đen", 28000000, 35990000)
            };

            foreach (var item in samsungVariants)
            {
                string fullName = $"{item.model} {item.storage} {item.color}";
                var img = GetSamsungS24Image(item.model, item.color);
                var sku = GetProductSKU(item.model, item.storage, item.color);
                var existing = context.SanPhams.FirstOrDefault(p => p.TenSanPham == fullName);
                if (existing != null)
                {
                    existing.GiaNhap = item.giaNhap;
                    existing.GiaBan = item.giaBan;
                    existing.HinhAnh = img;
                    existing.TrangThai = "Biến thể";
                    existing.SKU = sku;
                }
                else
                {
                    context.SanPhams.Add(new SanPham
                    {
                        TenSanPham = fullName,
                        MaDanhMuc = catId,
                        MaThuongHieu = brandSamsungId,
                        MaNCC = nccSamsungId,
                        GiaNhap = item.giaNhap,
                        GiaBan = item.giaBan,
                        SoLuongTon = 45,
                        MoTa = $"{item.model} phiên bản {item.storage} màu {item.color} chính hãng Samsung. Màn hình Dynamic AMOLED 2X, camera sắc nét tích hợp Galaxy AI.",
                        HinhAnh = img,
                        TrangThai = "Biến thể",
                        SKU = sku
                    });
                }
            }
        }
    }
}
