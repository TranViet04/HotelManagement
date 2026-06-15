using HotelManagement.Services.Customer;
using HotelManagement.ViewModels.Customer;
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

        [HttpGet]
        public async Task<IActionResult> Search(RoomSearchViewModel model)
        {
            var hasSearchQuery =
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.CheckInDate)) ||
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.CheckOutDate)) ||
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.Adults)) ||
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.Children));

            model.HasSearched = hasSearchQuery;

            if (!hasSearchQuery)
            {
                model.CheckInDate = DateTime.Today;
                model.CheckOutDate = DateTime.Today.AddDays(1);
                model.Adults = 1;
                model.Children = 0;

                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Results = await _publicRoomService.SearchAvailableRoomsAsync(model);

            return View(model);
        }

        public IActionResult Details(long id)
        {
            return View("Placeholder", $"Chi tiết loại phòng #{id}");
        }
    }
}
