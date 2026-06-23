using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_Employee")]
    public class EmployeeController : Controller
    {
        public IActionResult Index()
        {
            var employees = MockDataService.Instance.Employees.ToList();
            var roles = MockDataService.Instance.RolesList.ToList();
            
            ViewBag.RolesList = roles;
            return View(employees);
        }

        [HttpPost]
        [HasPermission("Create_Employee")]
        public IActionResult Create(MockDataService.Employee emp, List<string> selectedRoles)
        {
            emp.Id = MockDataService.Instance.Employees.Max(e => e.Id) + 1;
            emp.JoinedDate = System.DateTime.Now;
            emp.Password = "123";
            emp.Roles = selectedRoles ?? new List<string> { "Nhân viên bán hàng" };

            MockDataService.Instance.Employees.Add(emp);
            TempData["ToastMessage"] = "Thêm nhân viên mới thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Assign_Role")]
        public IActionResult UpdateRoles(int employeeId, List<string> selectedRoles)
        {
            var emp = MockDataService.Instance.Employees.FirstOrDefault(e => e.Id == employeeId);
            if (emp != null)
            {
                emp.Roles = selectedRoles ?? new List<string>();
                TempData["ToastMessage"] = "Cập nhật vai trò thành công!";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [HasPermission("Delete_Employee")]
        public IActionResult Delete(int id)
        {
            var target = MockDataService.Instance.Employees.FirstOrDefault(e => e.Id == id);
            if (target != null)
            {
                MockDataService.Instance.Employees.Remove(target);
                TempData["ToastMessage"] = "Đã xóa nhân viên khỏi hệ thống!";
            }
            return RedirectToAction("Index");
        }
    }
}
