using HotelManagement.Services.Customer;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    public class RoomsController : Controller
    {
        private readonly PublicRoomService _publicRoomService;

        public RoomsController(PublicRoomService publicRoomService)
        {
            _publicRoomService = publicRoomService;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _publicRoomService.GetActiveRoomTypesAsync();
            return View(rooms);
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
