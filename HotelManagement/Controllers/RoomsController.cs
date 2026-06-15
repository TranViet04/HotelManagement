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

        public IActionResult Details(long id)
        {
            return View("Placeholder", $"Chi tiết loại phòng #{id}");
        }
    }
}
