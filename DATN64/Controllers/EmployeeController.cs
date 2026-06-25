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
        [HasPermission("Assign_Role")]
        public IActionResult ToggleStatus(int id)
        {
            var target = _context.NhanViens.FirstOrDefault(e => e.MaNhanVien == id);
            if (target == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy nhân viên!";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }

            if (target.TrangThai == "Hoạt động")
            {
                // Cannot lock the only admin
                var adminRoleId = _context.Roles.FirstOrDefault(r => r.Name == "Admin")?.Id;
                if (adminRoleId != null)
                {
                    bool isAdmin = _context.NhanVienRoles.Any(nr => nr.MaNhanVien == id && nr.RoleId == adminRoleId);
                    if (isAdmin)
                    {
                        int activeAdmins = (from n in _context.NhanViens
                                            join nr in _context.NhanVienRoles on n.MaNhanVien equals nr.MaNhanVien
                                            where nr.RoleId == adminRoleId && n.TrangThai == "Hoạt động"
                                            select n).Count();
                        
                        if (activeAdmins <= 1)
                        {
                            TempData["ToastMessage"] = "Không thể vô hiệu hóa tài khoản Admin duy nhất!";
                            TempData["ToastType"] = "danger";
                            return RedirectToAction("Index");
                        }
                    }
                }
                
                target.TrangThai = "Bị khóa";
                TempData["ToastMessage"] = $"Đã vô hiệu hóa tài khoản \"{target.HoTen}\"!";
                TempData["ToastType"] = "warning";
            }
            else
            {
                target.TrangThai = "Hoạt động";
                TempData["ToastMessage"] = $"Đã kích hoạt lại tài khoản \"{target.HoTen}\"!";
                TempData["ToastType"] = "success";
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
