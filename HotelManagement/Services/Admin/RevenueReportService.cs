using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Admin
{
    public class RevenueReportService
    {
        private readonly HotelDbContext _context;

        public RevenueReportService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueReportViewModel> GetReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            var today = DateTime.Today;

            var from = fromDate?.Date ?? new DateTime(today.Year, today.Month, 1);
            var to = toDate?.Date ?? today;

            if (to < from)
            {
                to = from;
            }

            var toExclusive = to.AddDays(1);
            var tomorrow = today.AddDays(1);
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayNextMonth = firstDayOfMonth.AddMonths(1);

            var paidPaymentsQuery = _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatuses.Paid);

            var filteredPayments = await paidPaymentsQuery
                .Where(p => p.PaidAt >= from && p.PaidAt < toExclusive)
                .Select(p => new
                {
                    p.Amount,
                    p.PaidAt,
                    p.PaymentMethod
                })
                .ToListAsync();

            var revenueByDays = filteredPayments
                .GroupBy(p => p.PaidAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new RevenueByDayViewModel
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.Amount),
                    PaymentCount = g.Count()
                })
                .ToList();

            var revenueByPaymentMethods = filteredPayments
                .GroupBy(p => p.PaymentMethod)
                .OrderByDescending(g => g.Sum(x => x.Amount))
                .Select(g => new RevenueByPaymentMethodViewModel
                {
                    PaymentMethod = g.Key,
                    Revenue = g.Sum(x => x.Amount),
                    PaymentCount = g.Count()
                })
                .ToList();

            return new RevenueReportViewModel
            {
                FromDate = from,
                ToDate = to,
                RangeRevenue = filteredPayments.Sum(p => p.Amount),
                PaymentCount = filteredPayments.Count,
                TodayRevenue = await paidPaymentsQuery
                    .Where(p => p.PaidAt >= today && p.PaidAt < tomorrow)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,
                MonthRevenue = await paidPaymentsQuery
                    .Where(p => p.PaidAt >= firstDayOfMonth && p.PaidAt < firstDayNextMonth)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,
                TotalRevenue = await paidPaymentsQuery
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,
                BookingCount = await _context.Bookings
                    .CountAsync(b => b.CreatedAt >= from && b.CreatedAt < toExclusive),
                TotalInvoices = await _context.Invoices.CountAsync(),
                PaidInvoices = await _context.Invoices.CountAsync(i => i.Status == InvoiceStatuses.Paid),
                UnpaidInvoices = await _context.Invoices.CountAsync(i => i.Status == InvoiceStatuses.Unpaid),
                PartiallyPaidInvoices = await _context.Invoices.CountAsync(i => i.Status == InvoiceStatuses.PartiallyPaid),
                PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatuses.Pending),
                ConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatuses.Confirmed),
                CheckedInBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatuses.CheckedIn),
                CheckedOutBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatuses.CheckedOut),
                CancelledBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatuses.Cancelled),
                RevenueByDays = revenueByDays,
                RevenueByPaymentMethods = revenueByPaymentMethods
            };
        }
    }
}
