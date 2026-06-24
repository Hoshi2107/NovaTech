using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;
using System.Collections.Generic;
using System;

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
            var roles = new List<string> { "Quản lý", "Nhân viên bán hàng", "Nhân viên kho", "Kế toán" };
            
            ViewBag.RolesList = roles;
            return View(employees);
        }

        [HttpPost]
        [HasPermission("Create_Employee")]
        public IActionResult Create(NhanVien emp, List<string> selectedRoles)
        {
            emp.VaiTro = selectedRoles != null && selectedRoles.Any() ? string.Join(", ", selectedRoles) : "Nhân viên bán hàng";

            _context.NhanViens.Add(emp);
            _context.SaveChanges();
            TempData["ToastMessage"] = "Thêm nhân viên mới thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Assign_Role")]
        public IActionResult UpdateRoles(int employeeId, List<string> selectedRoles)
        {
            var emp = _context.NhanViens.FirstOrDefault(e => e.MaNhanVien == employeeId);
            if (emp != null)
            {
                emp.VaiTro = selectedRoles != null && selectedRoles.Any() ? string.Join(", ", selectedRoles) : "Nhân viên bán hàng";
                _context.SaveChanges();
                TempData["ToastMessage"] = "Cập nhật chức vụ thành công!";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Delete_Employee")]
        public IActionResult Delete(int id)
        {
            var target = _context.NhanViens.FirstOrDefault(e => e.MaNhanVien == id);
            if (target != null)
            {
                _context.NhanViens.Remove(target);
                _context.SaveChanges();
                TempData["ToastMessage"] = "Đã xóa nhân viên khỏi hệ thống!";
            }
            return RedirectToAction("Index");
        }
    }
}
