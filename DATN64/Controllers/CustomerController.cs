using Microsoft.AspNetCore.Mvc;
using DATN64.Models;
using DATN64.Helpers;
using System.Linq;

namespace DATN64.Controllers
{
    [HasPermission("View_Customer")]
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            var customers = MockDataService.Instance.Customers.ToList();
            return View(customers);
        }

        [HttpPost]
        [HasPermission("Create_Customer")]
        public IActionResult Create(MockDataService.Customer c)
        {
            c.Id = MockDataService.Instance.Customers.Max(cust => cust.Id) + 1;
            c.CreatedDate = System.DateTime.Now;
            c.Points = 10;
            c.MembershipRank = "Đồng";
            
            MockDataService.Instance.Customers.Add(c);
            TempData["ToastMessage"] = "Thêm khách hàng thành công!";
            return RedirectToAction("Index");
        }
    }
}
