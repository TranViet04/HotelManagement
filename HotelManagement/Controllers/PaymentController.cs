using HotelManagement.Constants;
using HotelManagement.Services.Payments;
using HotelManagement.ViewModels.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace HotelManagement.Controllers
{
    public class PaymentController : Controller
    {
        private readonly PaymentService _paymentService;

        public PaymentController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Authorize(Roles = UserRoles.Customer)]
        [HttpGet]
        public async Task<IActionResult> Checkout(long bookingId)
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Challenge();
            }

            var model = await _paymentService.GetCheckoutAsync(bookingId, customerId);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            if (model.PaymentStatus == PaymentStatuses.Paid)
            {
                return RedirectToAction(nameof(Success), new { bookingId });
            }

            return View(model);
        }

        [Authorize(Roles = UserRoles.Customer)]
        [HttpGet]
        public IActionResult Success(long bookingId)
        {
            ViewBag.BookingId = bookingId;
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Status(long? paymentId, string? bookingCode, string? invoiceCode)
        {
            if (paymentId.HasValue)
            {
                if (!TryGetCurrentUserId(out var customerId))
                {
                    return Unauthorized();
                }

                var pStatus = await _paymentService.GetPaymentStatusAsync(paymentId.Value, customerId);

                if (pStatus == null)
                {
                    return NotFound();
                }

                return Json(new { status = pStatus });
            }

            var codeStatus = await _paymentService.GetStatusByCodeAsync(bookingCode, invoiceCode);
            
            if (codeStatus == null)
            {
                return NotFound();
            }

            return Json(new { status = codeStatus.Value.Status, remainingAmount = codeStatus.Value.RemainingAmount });
        }

        [Authorize(Roles = UserRoles.Customer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateQr(long paymentId, long bookingId)
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Challenge();
            }

            await _paymentService.RegenerateQrAsync(paymentId);
            return RedirectToAction(nameof(Checkout), new { bookingId });
        }

        [HttpPost("Payment/Webhook")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            SepayWebhookPayload? payload;

            try
            {
                payload = await JsonSerializer.DeserializeAsync<SepayWebhookPayload>(
                    Request.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return BadRequest(new { success = false });
            }

            if (payload == null)
            {
                return BadRequest(new { success = false });
            }

            var apiKey = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Apikey ", string.Empty)
                ?? Request.Headers["X-Api-Key"].FirstOrDefault();

            var result = await _paymentService.ProcessWebhookAsync(payload, apiKey);

            if (!result.Succeeded)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true });
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            userId = 0;
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(userIdValue, out userId);
        }
    }
}
