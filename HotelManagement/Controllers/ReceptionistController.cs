using HotelManagement.Constants;
using HotelManagement.Services.Receptionist;
using HotelManagement.ViewModels.Receptionist;
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
        private readonly ReceptionistInvoiceService _invoiceService;

        public ReceptionistController(
            ReceptionistDashboardService dashboardService,
            ReceptionistBookingService bookingService,
            ReceptionistInvoiceService invoiceService)
        {
            _dashboardService = dashboardService;
            _bookingService = bookingService;
            _invoiceService = invoiceService;
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

        [HttpGet]
        public async Task<IActionResult> TodayCheckOuts()
        {
            var model = await _bookingService.GetTodayCheckOutsAsync();
            return View(model);
        }

        public IActionResult Rooms()
        {
            return View("Placeholder", "Theo dõi phòng");
        }

        [HttpGet]
        public async Task<IActionResult> BookingServices()
        {
            var model = await _bookingService.GetCheckedInBookingsForServiceAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddService(long bookingId)
        {
            var model = await _bookingService.PrepareAddServiceAsync(bookingId);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy booking.";
                return RedirectToAction(nameof(BookingServices));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService(AddBookingServiceViewModel model)
        {
            if (!TryGetCurrentUserId(out var receptionistId))
            {
                return Challenge();
            }

            var displayModel = await _bookingService.PrepareAddServiceAsync(model.BookingId);

            if (displayModel == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy booking.";
                return RedirectToAction(nameof(BookingServices));
            }

            displayModel.ServiceId = model.ServiceId;
            displayModel.Quantity = model.Quantity;
            displayModel.Note = model.Note;

            if (!displayModel.CanAddService)
            {
                TempData["ErrorMessage"] = displayModel.AddServiceBlockReason;
                return RedirectToAction(nameof(BookingDetails), new { id = model.BookingId });
            }

            if (!ModelState.IsValid)
            {
                return View(displayModel);
            }

            var result = await _bookingService.AddServiceToBookingAsync(model, receptionistId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);

                displayModel = await _bookingService.PrepareAddServiceAsync(model.BookingId);

                if (displayModel == null)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(BookingServices));
                }

                displayModel.ServiceId = model.ServiceId;
                displayModel.Quantity = model.Quantity;
                displayModel.Note = model.Note;

                return View(displayModel);
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(BookingDetails), new { id = model.BookingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOutBooking(long id)
        {
            if (!TryGetCurrentUserId(out var receptionistId))
            {
                return Challenge();
            }

            var result = await _bookingService.CheckOutBookingAsync(id, receptionistId);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(BookingDetails), new { id });
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(BookingDetails), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Invoices(string? keyword, string? status)
        {
            var model = await _invoiceService.GetInvoicesAsync(keyword, status);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> InvoiceDetails(long id)
        {
            var model = await _invoiceService.GetInvoiceDetailAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Invoices));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvoice(long bookingId)
        {
            if (!TryGetCurrentUserId(out var receptionistId))
            {
                return Challenge();
            }

            var result = await _invoiceService.CreateInvoiceAsync(bookingId, receptionistId);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(BookingDetails), new { id = bookingId });
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(InvoiceDetails), new { id = result.InvoiceId });
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
