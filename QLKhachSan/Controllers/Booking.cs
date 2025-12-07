using Microsoft.AspNetCore.Mvc;

namespace QLKhachSan.Controllers
{
    public class Booking : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
