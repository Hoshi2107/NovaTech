using Microsoft.AspNetCore.Mvc;

namespace FakeTikTokShop.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
