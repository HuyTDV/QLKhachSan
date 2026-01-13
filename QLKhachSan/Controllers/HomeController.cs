using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKhachSan.Models;
using System.Diagnostics;

namespace QLKhachSan.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Booking()
        {
            return View();
        }

        public IActionResult Blog()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Room()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult RoomDetails()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
