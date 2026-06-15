using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    public class RoomsController : Controller
    {
        public IActionResult Index()
        {
            return View("Placeholder", "Danh sách phòng");
        }

        public IActionResult Search()
        {
            return View("Placeholder", "Tìm phòng trống");
        }
    }
}
