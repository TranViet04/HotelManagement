using HotelManagement.Constants;
using HotelManagement.Services.Receptionist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Receptionist)]
    public class ReceptionistController : Controller
    {
        private readonly ReceptionistDashboardService _dashboardService;
        private readonly ReceptionistBookingService _bookingService;

        public ReceptionistController(
            ReceptionistDashboardService dashboardService,
            ReceptionistBookingService bookingService)
        {
            _dashboardService = dashboardService;
            _bookingService = bookingService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = await _dashboardService.GetDashboardAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Bookings(
            string? keyword,
            string? status,
            DateTime? checkInFrom,
            DateTime? checkInTo)
        {
            var model = await _bookingService.GetBookingsAsync(
                keyword,
                status,
                checkInFrom,
                checkInTo
            );

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> BookingDetails(long id)
        {
            var model = await _bookingService.GetBookingDetailAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy booking.";
                return RedirectToAction(nameof(Bookings));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(long id)
        {
            if (!TryGetCurrentUserId(out var receptionistId))
            {
                return Challenge();
            }

            var result = await _bookingService.ConfirmBookingAsync(id, receptionistId);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(BookingDetails), new { id });
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(BookingDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInBooking(long id)
        {
            if (!TryGetCurrentUserId(out var receptionistId))
            {
                return Challenge();
            }

            var result = await _bookingService.CheckInBookingAsync(id, receptionistId);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(BookingDetails), new { id });
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(BookingDetails), new { id });
        }

        public IActionResult CreateWalkInBooking()
        {
            return View("Placeholder", "Tạo đặt phòng trực tiếp");
        }

        [HttpGet]
        public async Task<IActionResult> TodayCheckIns()
        {
            var model = await _bookingService.GetTodayCheckInsAsync();
            return View(model);
        }

        public IActionResult TodayCheckOuts()
        {
            return View("Placeholder", "Danh sách khách trả phòng hôm nay");
        }

        public IActionResult Rooms()
        {
            return View("Placeholder", "Theo dõi phòng");
        }

        public IActionResult BookingServices()
        {
            return View("Placeholder", "Dịch vụ phát sinh");
        }

        public IActionResult Invoices()
        {
            return View("Placeholder", "Hóa đơn");
        }

        public IActionResult Payments()
        {
            return View("Placeholder", "Thanh toán");
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            userId = 0;

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return long.TryParse(userIdValue, out userId);
        }
    }
}
