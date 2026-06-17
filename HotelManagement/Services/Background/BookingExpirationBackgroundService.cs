using HotelManagement.Constants;
using HotelManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Background
{
    public class BookingExpirationBackgroundService : BackgroundService
    {
        private static readonly TimeSpan PaymentTimeout = TimeSpan.FromHours(3);
        private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingExpirationBackgroundService> _logger;

        public BookingExpirationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<BookingExpirationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CancelExpiredBookingsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while cancelling expired bookings");
                }

                await Task.Delay(ScanInterval, stoppingToken);
            }
        }

        private async Task CancelExpiredBookingsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var cutoff = DateTime.Now.Subtract(PaymentTimeout);

            var expiredBookings = await context.Bookings
                .Include(b => b.Invoice)
                    .ThenInclude(i => i!.Payments)
                .Where(b =>
                    b.Status == BookingStatuses.Pending
                    && b.CreatedAt <= cutoff
                    && (b.Invoice == null || b.Invoice.Status == InvoiceStatuses.Unpaid))
                .ToListAsync(stoppingToken);

            if (expiredBookings.Count == 0)
            {
                return;
            }

            var now = DateTime.Now;

            foreach (var booking in expiredBookings)
            {
                booking.Status = BookingStatuses.Cancelled;
                booking.CancelReason = "Tự động hủy do quá 3 giờ chưa thanh toán";
                booking.CancelledAt = now;
                booking.UpdatedAt = now;

                if (booking.Invoice != null)
                {
                    booking.Invoice.Status = InvoiceStatuses.Cancelled;

                    foreach (var payment in booking.Invoice.Payments
                                 .Where(p => p.Status == PaymentStatuses.Pending))
                    {
                        payment.Status = PaymentStatuses.Cancelled;
                    }
                }

                context.ActivityLogs.Add(new Models.ActivityLog
                {
                    UserId = booking.CustomerId,
                    Action = "AutoCancelBooking",
                    EntityName = "Booking",
                    EntityId = booking.Id,
                    Description = $"Tự động hủy booking {booking.BookingCode} do quá hạn thanh toán 3 giờ",
                    CreatedAt = now
                });
            }

            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Auto-cancelled {Count} expired bookings", expiredBookings.Count);
        }
    }
}
