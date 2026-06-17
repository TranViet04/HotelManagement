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

        public async Task<IActionResult> Index(RoomSearchViewModel search)
        {
            var hasAvailabilitySearch =
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.CheckInDate)) ||
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.CheckOutDate)) ||
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.Adults)) ||
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.Children));

            var model = new RoomListViewModel
            {
                HasAvailabilitySearch = hasAvailabilitySearch,
                CheckInDate = hasAvailabilitySearch ? search.CheckInDate : DateTime.Today,
                CheckOutDate = hasAvailabilitySearch ? search.CheckOutDate : DateTime.Today.AddDays(1),
                Adults = hasAvailabilitySearch ? search.Adults : 1,
                Children = hasAvailabilitySearch ? search.Children : 0
            };

            if (!hasAvailabilitySearch)
            {
                model.Rooms = await _publicRoomService.GetActiveRoomTypesAsync();
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                model.Rooms = await _publicRoomService.GetActiveRoomTypesAsync();
                return View(model);
            }

            var availableRoomTypes = await _publicRoomService.SearchAvailableRoomTypesAsync(search);

            model.Rooms = availableRoomTypes
                .Select(roomType => new RoomCardViewModel
                {
                    RoomTypeId = roomType.RoomTypeId,
                    Name = roomType.Name,
                    Description = roomType.Description,
                    Price = roomType.PricePerNight,
                    Capacity = roomType.Capacity,
                    BedType = roomType.BedType,
                    ThumbnailUrl = roomType.ThumbnailUrl,
                    AvailableRoomCount = roomType.AvailableRoomCount
                })
                .ToList();

            return View(model);
        }

        [HttpGet]
        public IActionResult Search(RoomSearchViewModel model)
        {
            var hasSearchQuery =
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.CheckInDate)) ||
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.CheckOutDate)) ||
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.Adults)) ||
                Request.Query.ContainsKey(nameof(RoomSearchViewModel.Children));

            if (hasSearchQuery)
            {
                return RedirectToAction(nameof(Index), new
                {
                    CheckInDate = model.CheckInDate.ToString("yyyy-MM-dd"),
                    CheckOutDate = model.CheckOutDate.ToString("yyyy-MM-dd"),
                    model.Adults,
                    model.Children
                });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(
            long id,
            DateTime? checkInDate,
            DateTime? checkOutDate,
            int adults = 1,
            int children = 0)
        {
            var model = await _publicRoomService.GetRoomDetailAsync(
                id,
                checkInDate,
                checkOutDate,
                adults,
                children
            );

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy loại phòng hoặc loại phòng không còn hoạt động.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}
