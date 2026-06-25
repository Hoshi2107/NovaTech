using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Identity;

namespace DATN64.Controllers
{
    [HasPermission("View_Employee")]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var employees = _context.NhanViens.ToList();
            var roles = _context.Roles.ToList();
            var rolePermissions = _context.RolePermissions.ToList();
            
            // Map employee ID to list of role IDs and names
            var employeeRoles = (from nr in _context.NhanVienRoles
                                 join r in _context.Roles on nr.RoleId equals r.Id
                                 select new { nr.MaNhanVien, nr.RoleId, r.Name }).ToList();
            
            ViewBag.RolesList = roles;
            ViewBag.RolePermissions = rolePermissions;
            
            // Populate a lookup for roles assigned to employees
            var empRolesDict = new Dictionary<int, List<Role>>();
            foreach (var mapping in employeeRoles)
            {
                var roleObj = roles.FirstOrDefault(r => r.Id == mapping.RoleId);
                if (roleObj != null)
                {
                    if (!empRolesDict.ContainsKey(mapping.MaNhanVien))
                    {
                        empRolesDict[mapping.MaNhanVien] = new List<Role>();
                    }
                    empRolesDict[mapping.MaNhanVien].Add(roleObj);
                }
            }
            ViewBag.EmployeeRolesDict = empRolesDict;
            
            // Dictionary of permissions organized by categories for a premium UI
            var permissionCategories = new Dictionary<string, List<string>>
            {
                { "Quản lý sản phẩm", new List<string> { "View_Product", "Create_Product", "Edit_Product", "Delete_Product" } },
                { "Quản lý kho", new List<string> { "View_Inventory", "Import_Inventory", "Export_Inventory" } },
                { "Quản lý đơn hàng", new List<string> { "View_Order", "Approve_Order" } },
                { "Khách hàng & Khuyến mãi", new List<string> { "View_Customer", "Create_Customer", "View_Promotion" } },
                { "Quản trị hệ thống", new List<string> { "View_Employee", "Create_Employee", "Assign_Role", "Delete_Employee", "View_Report", "View_Setting", "Edit_Setting" } },
                { "Liên kết TikTok", new List<string> { "View_TikTok", "Sync_TikTok" } }
            };
            ViewBag.PermissionCategories = permissionCategories;

            return View(employees);
        }

        [HttpPost]
        [HasPermission("Create_Employee")]
        public IActionResult Create(NhanVien emp, List<int> selectedRoleIds)
        {
            if (string.IsNullOrEmpty(emp.HoTen) || string.IsNullOrEmpty(emp.MatKhau))
            {
                TempData["ToastMessage"] = "Họ tên và Mật khẩu không được để trống!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            // Check if email already exists
            if (!string.IsNullOrEmpty(emp.Email) && _context.NhanViens.Any(e => e.Email == emp.Email))
            {
                TempData["ToastMessage"] = "Email đã được sử dụng bởi nhân viên khác!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            // Hash password
            var hasher = new PasswordHasher<NhanVien>();
            emp.MatKhau = hasher.HashPassword(emp, emp.MatKhau);
            emp.TrangThai = emp.TrangThai ?? "Hoạt động";

            _context.NhanViens.Add(emp);
            _context.SaveChanges(); // to get MaNhanVien

            // Add roles to NhanVienRole mapping table
            var rolesInDb = _context.Roles.Where(r => selectedRoleIds.Contains(r.Id)).ToList();
            foreach (var role in rolesInDb)
            {
                _context.NhanVienRoles.Add(new NhanVienRole
                {
                    MaNhanVien = emp.MaNhanVien,
                    RoleId = role.Id
                });
            }

            // Update VaiTro string for backwards compatibility
            emp.VaiTro = rolesInDb.Any() ? string.Join(", ", rolesInDb.Select(r => r.Name)) : "Nhân viên bán hàng";
            _context.SaveChanges();

            TempData["ToastMessage"] = "Thêm nhân viên mới thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Assign_Role")]
        public IActionResult Edit(int maNhanVien, string hoTen, string email, string soDienThoai, string? matKhau, string trangThai, List<int> selectedRoleIds)
        {
            var emp = _context.NhanViens.FirstOrDefault(e => e.MaNhanVien == maNhanVien);
            if (emp == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy nhân viên!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(hoTen))
            {
                TempData["ToastMessage"] = "Họ tên không được để trống!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            // Check if email already exists for another employee
            if (!string.IsNullOrEmpty(email) && _context.NhanViens.Any(e => e.Email == email && e.MaNhanVien != maNhanVien))
            {
                TempData["ToastMessage"] = "Email đã được sử dụng bởi nhân viên khác!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            // Update properties
            emp.HoTen = hoTen;
            emp.Email = email;
            emp.SoDienThoai = soDienThoai;
            emp.TrangThai = trangThai ?? "Hoạt động";

            // Hash new password if provided
            if (!string.IsNullOrEmpty(matKhau))
            {
                var hasher = new PasswordHasher<NhanVien>();
                emp.MatKhau = hasher.HashPassword(emp, matKhau);
            }

            // Clear old roles
            var oldRoles = _context.NhanVienRoles.Where(nr => nr.MaNhanVien == maNhanVien);
            _context.NhanVienRoles.RemoveRange(oldRoles);

            // Add new roles
            var rolesInDb = _context.Roles.Where(r => selectedRoleIds.Contains(r.Id)).ToList();
            foreach (var role in rolesInDb)
            {
                _context.NhanVienRoles.Add(new NhanVienRole
                {
                    MaNhanVien = maNhanVien,
                    RoleId = role.Id
                });
            }

            // Update legacy VaiTro column
            emp.VaiTro = rolesInDb.Any() ? string.Join(", ", rolesInDb.Select(r => r.Name)) : "Nhân viên bán hàng";

            _context.SaveChanges();

            TempData["ToastMessage"] = "Cập nhật nhân viên thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Delete_Employee")]
        public IActionResult Delete(int id)
        {
            var target = _context.NhanViens.FirstOrDefault(e => e.MaNhanVien == id);
            if (target == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy nhân viên!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            // Kiểm tra nhân viên có đơn hàng liên kết không
            bool hasOrders = _context.DonHangs.Any(d => d.MaNhanVien == id);
            if (hasOrders)
            {
                TempData["ToastMessage"] = $"Không thể xóa nhân viên \"{target.HoTen}\" vì đã có đơn hàng liên kết trong hệ thống. Hãy chuyển trạng thái sang \"Bị khóa\" để ngăn đăng nhập.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index");
            }

            // Xóa mapping vai trò trước (FK từ NhanVienRole)
            var oldRoles = _context.NhanVienRoles.Where(nr => nr.MaNhanVien == id);
            _context.NhanVienRoles.RemoveRange(oldRoles);

            _context.NhanViens.Remove(target);
            _context.SaveChanges();

            TempData["ToastMessage"] = $"Đã xóa nhân viên \"{target.HoTen}\" khỏi hệ thống!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Assign_Role")]
        public IActionResult SaveRolePermissions(string roleName, List<string> selectedPermissions)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return Json(new { success = false, message = "Tên vai trò không hợp lệ!" });
            }

            // Clear old permissions
            var oldPerms = _context.RolePermissions.Where(rp => rp.RoleName == roleName);
            _context.RolePermissions.RemoveRange(oldPerms);

            // Add new permissions
            if (selectedPermissions != null)
            {
                foreach (var perm in selectedPermissions)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleName = roleName,
                        PermissionName = perm
                    });
                }
            }

            _context.SaveChanges();

            return Json(new { success = true, message = $"Đã cập nhật quyền cho vai trò '{roleName}' thành công!" });
        }
    }
}
