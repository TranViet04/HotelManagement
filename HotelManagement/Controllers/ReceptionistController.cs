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
        private readonly ReceptionistOperationService _operationService;

        public ReceptionistController(
            ReceptionistDashboardService dashboardService,
            ReceptionistBookingService bookingService,
            ReceptionistInvoiceService invoiceService,
            ReceptionistOperationService operationService)
        {
            _dashboardService = dashboardService;
            _bookingService = bookingService;
            _invoiceService = invoiceService;
            _operationService = operationService;
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

        [HttpGet]
        public async Task<IActionResult> CreateWalkInBooking()
        {
            var model = await _bookingService.PrepareCreateWalkInBookingAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableRooms(DateTime checkInDate, DateTime checkOutDate)
        {
            var options = await _bookingService.GetAvailableRoomOptionsAsync(checkInDate, checkOutDate);
            return Json(options);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWalkInBooking(CreateWalkInBookingViewModel model)
        {
            if (!TryGetCurrentUserId(out var receptionistId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                model = await _bookingService.PrepareCreateWalkInBookingAsync(model);
                return View(model);
            }

            var result = await _bookingService.CreateWalkInBookingAsync(model, receptionistId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                model = await _bookingService.PrepareCreateWalkInBookingAsync(model);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(WalkInPayment), new { bookingId = result.BookingId, method = model.PaymentMethod });
        }

        [HttpGet]
        public async Task<IActionResult> WalkInPayment(long bookingId, string method)
        {
            var booking = await _bookingService.GetBookingDetailAsync(bookingId);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy booking.";
                return RedirectToAction(nameof(Bookings));
            }

            ViewBag.PaymentMethod = method;
            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> WalkInPaymentQrTab(long bookingId)
        {
            var booking = await _bookingService.GetBookingDetailAsync(bookingId);
            if (booking == null) return NotFound();
            
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmWalkInCashPayment(long bookingId)
        {
            if (!TryGetCurrentUserId(out var receptionistId))
            {
                return Challenge();
            }

            var booking = await _bookingService.GetBookingDetailAsync(bookingId);
            if (booking == null || booking.InvoiceId == null) 
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn của booking.";
                return RedirectToAction(nameof(Bookings));
            }

            var paymentModel = new RecordPaymentViewModel
            {
                InvoiceId = booking.InvoiceId.Value,
                PaymentMethod = "Cash",
                Amount = booking.TotalAmount,
                Note = "Thu tiền mặt tại quầy (Walk-in)"
            };

            var result = await _invoiceService.RecordPaymentAsync(paymentModel, receptionistId);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(WalkInPayment), new { bookingId = bookingId, method = "Cash" });
            }

            return RedirectToAction(nameof(PrintInvoice), new { invoiceId = booking.InvoiceId.Value });
        }

        [HttpGet]
        public async Task<IActionResult> PrintInvoice(long invoiceId)
        {
            var invoice = await _invoiceService.GetInvoiceDetailAsync(invoiceId);
            if (invoice == null) 
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Invoices));
            }
            
            return View(invoice);
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

        [HttpGet]
        public async Task<IActionResult> RoomStatus(DateTime? date)
        {
            var model = await _operationService.GetRoomStatusBoardAsync(date);
            return View(model);
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

            var bookingDetail = await _bookingService.GetBookingDetailAsync(id);
            if (bookingDetail?.InvoiceId != null && bookingDetail.InvoiceRemainingAmount > 0)
            {
                TempData["WarningMessage"] = "Khách hàng có khoản nợ phát sinh cần thanh toán!";
                return RedirectToAction(nameof(RecordPayment), new { invoiceId = bookingDetail.InvoiceId.Value });
            }

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

        [HttpGet]
        public async Task<IActionResult> Payments(string? keyword)
        {
            var model = await _invoiceService.GetPaymentInvoicesAsync(keyword);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> RecordPayment(long invoiceId)
        {
            var model = await _invoiceService.PrepareRecordPaymentAsync(invoiceId);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Payments));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(RecordPaymentViewModel model)
        {
            if (!TryGetCurrentUserId(out var receptionistId))
            {
                return Challenge();
            }

            var displayModel = await _invoiceService.PrepareRecordPaymentAsync(model.InvoiceId);

            if (displayModel == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Payments));
            }

            displayModel.PaymentMethod = model.PaymentMethod;
            displayModel.Amount = model.Amount;
            displayModel.Note = model.Note;

            if (!displayModel.CanRecordPayment)
            {
                TempData["ErrorMessage"] = displayModel.RecordPaymentBlockReason;
                return RedirectToAction(nameof(InvoiceDetails), new { id = model.InvoiceId });
            }

            if (!ModelState.IsValid)
            {
                return View(displayModel);
            }

            var result = await _invoiceService.RecordPaymentAsync(model, receptionistId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);

                displayModel = await _invoiceService.PrepareRecordPaymentAsync(model.InvoiceId);

                if (displayModel == null)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(Payments));
                }

                displayModel.PaymentMethod = model.PaymentMethod;
                displayModel.Amount = model.Amount;
                displayModel.Note = model.Note;

                return View(displayModel);
            }

            TempData["SuccessMessage"] = result.Message;

            // Redirect to PrintInvoice if payment recorded successfully
            return RedirectToAction(nameof(PrintInvoice), new { invoiceId = model.InvoiceId });
        }

        [HttpGet]
        public async Task<IActionResult> InvoicePaymentQrTab(long invoiceId, decimal amount)
        {
            var invoice = await _invoiceService.GetInvoiceDetailAsync(invoiceId);
            if (invoice == null) return NotFound();
            
            ViewBag.AmountToPay = amount;
            return View(invoice);
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            userId = 0;

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return long.TryParse(userIdValue, out userId);
        }
    }
}
