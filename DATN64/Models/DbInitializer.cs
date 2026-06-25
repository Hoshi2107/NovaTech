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
