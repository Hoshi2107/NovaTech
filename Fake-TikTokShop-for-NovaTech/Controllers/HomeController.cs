using Microsoft.AspNetCore.Mvc;

namespace FakeTikTokShop.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Livestream()
        {
            return View();
        }

        public IActionResult LiveViewer()
        {
            return View();
        }
    }
}
