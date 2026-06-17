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
                return RedirectToAction("Index", "Rooms");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            return await SelectServices(model);
        }

        [HttpGet]
        public async Task<IActionResult> SelectServices(
            long roomId,
            DateTime? checkInDate,
            DateTime? checkOutDate,
            int adults = 1,
            int children = 0)
        {
            var model = await _customerBookingService.PrepareSelectServicesAsync(
                roomId,
                checkInDate,
                checkOutDate,
                adults,
                children);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng hoặc phòng không còn trống.";
                return RedirectToAction("Search", "Rooms");
            }

            return View("SelectServices", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectServices(CreateBookingViewModel model)
        {
            var selectModel = await _customerBookingService.PrepareSelectServicesAsync(
                model.RoomId,
                model.CheckInDate,
                model.CheckOutDate,
                model.Adults,
                model.Children);

            if (selectModel == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng hoặc phòng không còn trống.";
                return RedirectToAction("Search", "Rooms");
            }

            selectModel.SpecialRequest = model.SpecialRequest;

            if (!ModelState.IsValid)
            {
                var displayModel = await _customerBookingService.PrepareCreateBookingAsync(
                    model.RoomId,
                    model.CheckInDate,
                    model.CheckOutDate,
                    model.Adults,
                    model.Children);

                if (displayModel == null)
                {
                    return RedirectToAction("Search", "Rooms");
                }

                displayModel.SpecialRequest = model.SpecialRequest;
                return View("Create", displayModel);
            }

            return View("SelectServices", selectModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(SelectServicesViewModel model)
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Challenge();
            }


            if (!ModelState.IsValid)
            {
                return View("SelectServices", model);
            }

            var result = await _customerBookingService.CreateBookingWithServicesAsync(model, customerId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);

                var refreshed = await _customerBookingService.PrepareSelectServicesAsync(
                    model.RoomId,
                    model.CheckInDate,
                    model.CheckOutDate,
                    model.Adults,
                    model.Children);

                if (refreshed == null)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Index", "Rooms");
                }

                refreshed.SpecialRequest = model.SpecialRequest;

                foreach (var service in refreshed.AvailableServices)
                {
                    var selected = model.AvailableServices.FirstOrDefault(s => s.ServiceId == service.ServiceId);
                    if (selected != null)
                    {
                        service.IsSelected = selected.IsSelected;
                        service.Quantity = selected.Quantity;
                    }
                }

                return View("SelectServices", refreshed);
            }

            return RedirectToAction("Checkout", "Payment", new { bookingId = result.BookingId });
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Challenge();
            }

            var bookings = await _customerBookingService.GetMyBookingsAsync(customerId);

            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Challenge();
            }

            var booking = await _customerBookingService.GetMyBookingDetailAsync(id, customerId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt phòng hoặc bạn không có quyền xem đặt phòng này.";
                return RedirectToAction(nameof(MyBookings));
            }

            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(long id)
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Challenge();
            }

            var model = await _customerBookingService.PrepareCancelBookingAsync(id, customerId);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt phòng hoặc bạn không có quyền hủy đặt phòng này.";
                return RedirectToAction(nameof(MyBookings));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(CancelBookingViewModel model)
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Challenge();
            }

            var displayModel = await _customerBookingService.PrepareCancelBookingAsync(
                model.BookingId,
                customerId
            );

            if (displayModel == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt phòng hoặc bạn không có quyền hủy đặt phòng này.";
                return RedirectToAction(nameof(MyBookings));
            }

            displayModel.CancelReason = model.CancelReason;

            if (!displayModel.CanCancel)
            {
                TempData["ErrorMessage"] = displayModel.CancelBlockReason;
                return RedirectToAction(nameof(Details), new { id = model.BookingId });
            }

            if (!ModelState.IsValid)
            {
                return View(displayModel);
            }

            var result = await _customerBookingService.CancelMyBookingAsync(
                model.BookingId,
                customerId,
                model.CancelReason
            );

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);

                displayModel = await _customerBookingService.PrepareCancelBookingAsync(
                    model.BookingId,
                    customerId
                );

                if (displayModel == null)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(MyBookings));
                }

                displayModel.CancelReason = model.CancelReason;
                return View(displayModel);
            }

            TempData["SuccessMessage"] = result.Message ?? $"Đã hủy đặt phòng {result.BookingCode} thành công.";

            return RedirectToAction(nameof(MyBookings));
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            userId = 0;

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return long.TryParse(userIdValue, out userId);
        }
    }
}
