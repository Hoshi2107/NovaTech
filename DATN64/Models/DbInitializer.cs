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
