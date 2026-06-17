using HotelManagement.Configuration;
using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services.Email;
using HotelManagement.ViewModels.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelManagement.Services.Payments
{
    public class PaymentService
    {
        private static readonly TimeSpan QrValidity = TimeSpan.FromMinutes(30);

        private readonly HotelDbContext _context;
        private readonly SepaySettings _sepaySettings;
        private readonly EmailService _emailService;

        public PaymentService(
            HotelDbContext context,
            IOptions<SepaySettings> sepaySettings,
            EmailService emailService)
        {
            _context = context;
            _sepaySettings = sepaySettings.Value;
            _emailService = emailService;
        }

        public async Task<CheckoutViewModel?> GetCheckoutAsync(long bookingId, long customerId)
        {
            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.Invoice)
                    .ThenInclude(i => i!.Payments)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

            if (booking?.Invoice == null)
            {
                return null;
            }

            var payment = booking.Invoice.Payments
                .Where(p => p.PaymentMethod == PaymentMethods.Sepay)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (payment == null)
            {
                return null;
            }

            if (payment.Status == PaymentStatuses.Pending && IsQrExpired(payment))
            {
                payment = await RegenerateQrAsync(payment.Id);
            }

            if (payment == null)
            {
                return null;
            }

            return BuildCheckoutViewModel(booking, booking.Invoice, payment);
        }

        public async Task<Payment?> RegenerateQrAsync(long paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null || payment.Status != PaymentStatuses.Pending)
            {
                return payment;
            }

            payment.QrExpiresAt = DateTime.Now.Add(QrValidity);
            await _context.SaveChangesAsync();

            return payment;
        }

        public async Task<PaymentResult> ProcessWebhookAsync(SepayWebhookPayload payload, string? apiKey)
        {
            if (!ValidateWebhookApiKey(apiKey))
            {
                return PaymentResult.Failure("Unauthorized");
            }

            if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentResult.Success("Ignored non-inbound transfer");
            }

            var existingBySepayId = await _context.Payments
                .AnyAsync(p => p.SepayTransactionId == payload.Id);

            if (existingBySepayId)
            {
                return PaymentResult.Success("Already processed");
            }

            var codes = ExtractCodes(payload);
            if (string.IsNullOrWhiteSpace(codes.BookingCode) && string.IsNullOrWhiteSpace(codes.InvoiceCode))
            {
                return PaymentResult.Failure("Booking code or Invoice code not found in transfer content");
            }

            var bookingQuery = _context.Bookings
                .Include(b => b.Invoice)
                    .ThenInclude(i => i!.Payments)
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
                .AsQueryable();

            Booking? booking = null;
            if (!string.IsNullOrWhiteSpace(codes.BookingCode))
            {
                booking = await bookingQuery.FirstOrDefaultAsync(b => b.BookingCode == codes.BookingCode);
            }
            else if (!string.IsNullOrWhiteSpace(codes.InvoiceCode))
            {
                booking = await bookingQuery.FirstOrDefaultAsync(b => b.Invoice != null && b.Invoice.InvoiceCode == codes.InvoiceCode);
            }

            if (booking?.Invoice == null)
            {
                return PaymentResult.Failure("Booking or invoice not found");
            }

            if (!string.IsNullOrWhiteSpace(_sepaySettings.BankAccount)
                && !string.Equals(
                    payload.AccountNumber?.Trim(),
                    _sepaySettings.BankAccount.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return PaymentResult.Failure("Account number mismatch");
            }

            var payment = booking.Invoice.Payments
                .Where(p => p.PaymentMethod == PaymentMethods.Sepay && p.Status == PaymentStatuses.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (payload.TransferAmount < booking.Invoice.RemainingAmount)
            {
                return PaymentResult.Failure("Transfer amount is less than remaining amount");
            }

            var now = DateTime.Now;

            if (payment == null)
            {
                payment = new Payment
                {
                    InvoiceId = booking.Invoice.Id,
                    PaymentCode = await GeneratePaymentCodeAsync(),
                    PaymentMethod = PaymentMethods.Sepay,
                    Amount = payload.TransferAmount,
                    Status = PaymentStatuses.Paid,
                    PaidAt = now,
                    SepayTransactionId = payload.Id,
                    Note = $"Sepay ref: {payload.ReferenceCode} (Tại quầy)",
                    CreatedAt = now
                };
                _context.Payments.Add(payment);
            }
            else
            {
                payment.Status = PaymentStatuses.Paid;
                payment.PaidAt = now;
                payment.SepayTransactionId = payload.Id;
                payment.Note = $"Sepay ref: {payload.ReferenceCode}";
            }

            booking.Invoice.PaidAmount += payload.TransferAmount;
            if (booking.Invoice.PaidAmount >= booking.Invoice.TotalAmount)
            {
                booking.Invoice.PaidAmount = booking.Invoice.TotalAmount;
                booking.Invoice.Status = InvoiceStatuses.Paid;
            }
            else
            {
                booking.Invoice.Status = InvoiceStatuses.PartiallyPaid;
            }
            booking.Invoice.RemainingAmount = booking.Invoice.TotalAmount - booking.Invoice.PaidAmount;

            if (booking.Invoice.RemainingAmount == 0)
            {
                booking.Invoice.PaidAt = now;
            }

            if (booking.Status == BookingStatuses.Pending)
            {
                booking.Status = BookingStatuses.Confirmed;
                booking.ConfirmedAt = now;
            }
            booking.UpdatedAt = now;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = booking.CustomerId,
                Action = "SepayPayment",
                EntityName = "Payment",
                EntityId = payment.Id,
                Description = $"Thanh toán Sepay thành công cho booking {booking.BookingCode}. Số tiền: {payload.TransferAmount:N0} VND",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            if (booking.Customer != null && !string.IsNullOrWhiteSpace(booking.Customer.Email))
            {
                try
                {
                    await _emailService.SendPaymentReceiptAsync(
                        booking.Customer.Email,
                        booking.Customer.FullName,
                        booking,
                        booking.Invoice,
                        booking.BookingServices);
                }
                catch
                {
                    // Email failure should not fail webhook acknowledgment
                }
            }

            return PaymentResult.Success("Payment confirmed");
        }

        public async Task<string?> GetPaymentStatusAsync(long paymentId, long customerId)
        {
            var status = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Id == paymentId && p.Invoice!.Booking!.CustomerId == customerId)
                .Select(p => p.Status)
                .FirstOrDefaultAsync();

            return status;
        }

        public async Task<(string Status, decimal RemainingAmount)?> GetStatusByCodeAsync(string? bookingCode, string? invoiceCode)
        {
            if (!string.IsNullOrEmpty(bookingCode))
            {
                var invoice = await _context.Bookings
                    .AsNoTracking()
                    .Where(b => b.BookingCode == bookingCode && b.Invoice != null)
                    .Select(b => new { b.Invoice!.Status, b.Invoice.RemainingAmount })
                    .FirstOrDefaultAsync();
                
                if (invoice != null) return (invoice.Status, invoice.RemainingAmount);
            }

            if (!string.IsNullOrEmpty(invoiceCode))
            {
                var invoice = await _context.Invoices
                    .AsNoTracking()
                    .Where(i => i.InvoiceCode == invoiceCode)
                    .Select(i => new { i.Status, i.RemainingAmount })
                    .FirstOrDefaultAsync();

                if (invoice != null) return (invoice.Status, invoice.RemainingAmount);
            }

            return null;
        }

        public string BuildQrCodeUrl(decimal amount, string transferContent)
        {
            var bankId = Uri.EscapeDataString(_sepaySettings.BankId);
            var account = Uri.EscapeDataString(_sepaySettings.BankAccount);
            var accountName = Uri.EscapeDataString(_sepaySettings.AccountName);
            var addInfo = Uri.EscapeDataString(transferContent);
            var amountValue = ((long)amount).ToString();

            return $"https://qr.sepay.vn/img?acc={account}&bank={bankId}&amount={amountValue}&des={addInfo}&template=compact&showinfo=true&fullacc=true&holder={accountName}";
        }

        public async Task<Payment> CreatePendingSepayPaymentAsync(Invoice invoice, long customerId)
        {
            var paymentCode = await GeneratePaymentCodeAsync();
            var now = DateTime.Now;

            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                PaymentCode = paymentCode,
                PaymentMethod = PaymentMethods.Sepay,
                Amount = invoice.TotalAmount,
                Status = PaymentStatuses.Pending,
                CreatedAt = now,
                QrExpiresAt = now.Add(QrValidity),
                CreatedByUserId = customerId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return payment;
        }

        private CheckoutViewModel BuildCheckoutViewModel(Booking booking, Invoice invoice, Payment payment)
        {
            var transferContent = booking.BookingCode;
            var qrExpiresAt = payment.QrExpiresAt ?? DateTime.Now;

            return new CheckoutViewModel
            {
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                BookingId = booking.Id,
                BookingCode = booking.BookingCode,
                RoomTypeName = booking.Room?.RoomType?.Name ?? "Không xác định",
                RoomNumber = booking.Room?.RoomNumber ?? "N/A",
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Nights = Math.Max(1, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days),
                RoomAmount = invoice.RoomAmount,
                ServiceAmount = invoice.ServiceAmount,
                TotalAmount = invoice.TotalAmount,
                QrCodeUrl = BuildQrCodeUrl(invoice.TotalAmount, transferContent),
                BankName = _sepaySettings.BankName,
                BankAccount = _sepaySettings.BankAccount,
                AccountName = _sepaySettings.AccountName,
                TransferContent = transferContent,
                QrExpiresAt = qrExpiresAt,
                IsQrExpired = IsQrExpired(payment),
                PaymentStatus = payment.Status,
                Services = booking.BookingServices.Select(bs => new CheckoutServiceItemViewModel
                {
                    Name = bs.Service?.Name ?? "Dịch vụ",
                    Quantity = bs.Quantity,
                    TotalPrice = bs.TotalPrice
                }).ToList()
            };
        }

        private static bool IsQrExpired(Payment payment)
        {
            return payment.QrExpiresAt.HasValue && payment.QrExpiresAt.Value <= DateTime.Now;
        }

        private bool ValidateWebhookApiKey(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(_sepaySettings.WebhookApiKey))
            {
                return true;
            }

            return string.Equals(apiKey, _sepaySettings.WebhookApiKey, StringComparison.Ordinal);
        }

        private static (string? BookingCode, string? InvoiceCode) ExtractCodes(SepayWebhookPayload payload)
        {
            var content = payload.Content ?? string.Empty;
            var code = payload.Code ?? string.Empty;
            var fullText = $"{code} {content}";

            var bkMatch = System.Text.RegularExpressions.Regex.Match(fullText, @"BK[A-Z0-9]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var invMatch = System.Text.RegularExpressions.Regex.Match(fullText, @"INV[A-Z0-9]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return (
                bkMatch.Success ? bkMatch.Value.ToUpperInvariant() : null,
                invMatch.Success ? invMatch.Value.ToUpperInvariant() : null
            );
        }

        private async Task<string> GeneratePaymentCodeAsync()
        {
            for (var i = 0; i < 5; i++)
            {
                var code = $"PAY{DateTime.Now:yyyyMMddHHmmssfff}{Random.Shared.Next(100, 999)}";
                var exists = await _context.Payments.AnyAsync(p => p.PaymentCode == code);

                if (!exists)
                {
                    return code;
                }
            }

            return $"PAY{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
        }
    }

    public class PaymentResult
    {
        public bool Succeeded { get; set; }

        public string Message { get; set; } = string.Empty;

        public static PaymentResult Success(string message) =>
            new() { Succeeded = true, Message = message };

        public static PaymentResult Failure(string message) =>
            new() { Succeeded = false, Message = message };
    }
}
