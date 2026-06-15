using HotelManagement.Constants;
using HotelManagement.Services.Customer;
using HotelManagement.ViewModels.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Customer)]
    public class BookingsController : Controller
    {
        private readonly CustomerBookingService _customerBookingService;

        public BookingsController(CustomerBookingService customerBookingService)
        {
            _customerBookingService = customerBookingService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(
            long roomId,
            DateTime? checkInDate,
            DateTime? checkOutDate,
            int adults = 1,
            int children = 0)
        {
            var model = await _customerBookingService.PrepareCreateBookingAsync(
                roomId,
                checkInDate,
                checkOutDate,
                adults,
                children
            );

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng hoặc phòng không thể đặt.";
                return RedirectToAction("Search", "Rooms");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Challenge();
            }

            var displayModel = await _customerBookingService.PrepareCreateBookingAsync(
                model.RoomId,
                model.CheckInDate,
                model.CheckOutDate,
                model.Adults,
                model.Children
            );

            if (displayModel == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng hoặc phòng không thể đặt.";
                return RedirectToAction("Search", "Rooms");
            }

            displayModel.SpecialRequest = model.SpecialRequest;

            if (!ModelState.IsValid)
            {
                return View(displayModel);
            }

            var result = await _customerBookingService.CreateBookingAsync(model, customerId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);

                displayModel = await _customerBookingService.PrepareCreateBookingAsync(
                    model.RoomId,
                    model.CheckInDate,
                    model.CheckOutDate,
                    model.Adults,
                    model.Children
                );

                if (displayModel == null)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Search", "Rooms");
                }

                displayModel.SpecialRequest = model.SpecialRequest;

                return View(displayModel);
            }

            TempData["SuccessMessage"] = $"Đặt phòng thành công. Mã đặt phòng: {result.BookingCode}. Vui lòng chờ lễ tân xác nhận.";

            return RedirectToAction(nameof(MyBookings));
        }

        [HttpGet]
        public IActionResult MyBookings()
        {
            return View("Placeholder", "Lịch sử đặt phòng");
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            userId = 0;

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return long.TryParse(userIdValue, out userId);
        }
    }
}
