using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;
using System.IO;
using System;
using System.Threading.Tasks;

namespace DATN64.Controllers
{
    [HasPermission("View_Setting")]
    public class SettingController : Controller
    {
        private readonly AppDbContext _context;

        public SettingController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var config = _context.CauHinhs.FirstOrDefault();
            if (config == null)
            {
                config = new CauHinh
                {
                    TenCuaHang = "Siêu thị NovaTech",
                    Email = "contact@novatech.vn",
                    SoDienThoai = "1900 1000",
                    DiaChi = "123 Đường Điện Biên Phủ, TP.HCM",
                    Logo = "/uploads/logo/default_logo.png"
                };
                _context.CauHinhs.Add(config);
                _context.SaveChanges();
            }

            return View(config);
        }

        [HttpPost]
        [HasPermission("Edit_Setting")]
        public async Task<IActionResult> Save(string shopName, string shopAddress, string shopEmail, string shopHotline, IFormFile? logoFile, string? currentLogo)
        {
            var config = _context.CauHinhs.FirstOrDefault();
            if (config == null)
            {
                config = new CauHinh();
                _context.CauHinhs.Add(config);
            }

            if (logoFile != null && logoFile.Length > 0)
            {
                // 1. Validate file extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(logoFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Định dạng tệp không hợp lệ. Chỉ cho phép tải lên hình ảnh .jpg, .jpeg, .png, .webp.");
                }

                // 2. Validate file size (Max 5MB)
                if (logoFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Kích thước hình ảnh Logo vượt quá giới hạn cho phép (Tối đa 5MB).");
                }

                if (!ModelState.IsValid)
                {
                    // Update model state config values before rendering back view so the user's inputs are preserved
                    config.TenCuaHang = shopName;
                    config.DiaChi = shopAddress;
                    config.Email = shopEmail;
                    config.SoDienThoai = shopHotline;
                    return View("Index", config);
                }

                // 3. Upload new logo
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logo");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(logoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(fileStream);
                }

                var newLogoUrl = $"/uploads/logo/{uniqueFileName}";

                // 4. Safely delete old logo if not default
                var oldLogo = config.Logo;
                if (!string.IsNullOrEmpty(oldLogo) && 
                    oldLogo != "/uploads/logo/default_logo.png" && 
                    !oldLogo.Equals("default_logo.png", StringComparison.OrdinalIgnoreCase))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldLogo.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Lỗi khi xóa ảnh logo cũ: {ex.Message}");
                        }
                    }
                }

                config.Logo = newLogoUrl;
            }
            else if (!string.IsNullOrEmpty(currentLogo))
            {
                config.Logo = currentLogo;
            }

            // Update other information
            config.TenCuaHang = shopName;
            config.DiaChi = shopAddress;
            config.Email = shopEmail;
            config.SoDienThoai = shopHotline;

            _context.SaveChanges();

            TempData["ToastMessage"] = "Lưu cấu hình hệ thống thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }
    }
}
