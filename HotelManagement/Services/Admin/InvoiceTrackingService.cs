using HotelManagement.Data;
using HotelManagement.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Admin
{
    public class InvoiceTrackingService
    {
        private readonly HotelDbContext _context;

        public InvoiceTrackingService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<InvoiceTrackingViewModel>> GetInvoicesAsync(
            string? keyword,
            string? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _context.Invoices
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();

                query = query.Where(i =>
                    i.InvoiceCode.Contains(normalizedKeyword) ||
                    (i.Booking != null && i.Booking.BookingCode.Contains(normalizedKeyword)) ||
                    (i.Booking != null && i.Booking.Customer != null && i.Booking.Customer.FullName.Contains(normalizedKeyword)) ||
                    (i.Booking != null && i.Booking.Customer != null && i.Booking.Customer.PhoneNumber != null && i.Booking.Customer.PhoneNumber.Contains(normalizedKeyword)) ||
                    (i.Booking != null && i.Booking.Room != null && i.Booking.Room.RoomNumber.Contains(normalizedKeyword)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(i => i.Status == status);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(i => i.IssuedAt >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(i => i.IssuedAt < to);
            }

            return await query
                .OrderByDescending(i => i.IssuedAt)
                .Select(i => new InvoiceTrackingViewModel
                {
                    Id = i.Id,
                    InvoiceCode = i.InvoiceCode,
                    BookingCode = i.Booking != null ? i.Booking.BookingCode : string.Empty,
                    CustomerName = i.Booking != null && i.Booking.Customer != null ? i.Booking.Customer.FullName : string.Empty,
                    CustomerPhone = i.Booking != null && i.Booking.Customer != null ? i.Booking.Customer.PhoneNumber : null,
                    RoomNumber = i.Booking != null && i.Booking.Room != null ? i.Booking.Room.RoomNumber : string.Empty,
                    RoomTypeName = i.Booking != null && i.Booking.Room != null && i.Booking.Room.RoomType != null ? i.Booking.Room.RoomType.Name : string.Empty,
                    RoomAmount = i.RoomAmount,
                    ServiceAmount = i.ServiceAmount,
                    DiscountAmount = i.DiscountAmount,
                    TaxAmount = i.TaxAmount,
                    TotalAmount = i.TotalAmount,
                    PaidAmount = i.PaidAmount,
                    RemainingAmount = i.RemainingAmount,
                    Status = i.Status,
                    IssuedAt = i.IssuedAt,
                    PaidAt = i.PaidAt
                })
                .ToListAsync();
        }

        public async Task<InvoiceDetailViewModel?> GetDetailAsync(long id)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.IssuedByUser)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Room)
                        .ThenInclude(r => r!.RoomType)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.BookingServices)
                        .ThenInclude(bs => bs.Service)
                .Include(i => i.Payments)
                    .ThenInclude(p => p.CreatedByUser)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return null;
            }

            return new InvoiceDetailViewModel
            {
                Id = invoice.Id,
                InvoiceCode = invoice.InvoiceCode,
                Status = invoice.Status,
                BookingCode = invoice.Booking?.BookingCode ?? string.Empty,
                CustomerName = invoice.Booking?.Customer?.FullName ?? string.Empty,
                CustomerPhone = invoice.Booking?.Customer?.PhoneNumber,
                CustomerEmail = invoice.Booking?.Customer?.Email,
                RoomNumber = invoice.Booking?.Room?.RoomNumber ?? string.Empty,
                RoomTypeName = invoice.Booking?.Room?.RoomType?.Name ?? string.Empty,
                CheckInDate = invoice.Booking?.CheckInDate ?? default,
                CheckOutDate = invoice.Booking?.CheckOutDate ?? default,
                RoomAmount = invoice.RoomAmount,
                ServiceAmount = invoice.ServiceAmount,
                DiscountAmount = invoice.DiscountAmount,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.PaidAmount,
                RemainingAmount = invoice.RemainingAmount,
                IssuedByName = invoice.IssuedByUser?.FullName,
                IssuedAt = invoice.IssuedAt,
                PaidAt = invoice.PaidAt,
                Note = invoice.Note,
                Services = invoice.Booking?.BookingServices
                    .Select(bs => new InvoiceServiceItemViewModel
                    {
                        ServiceName = bs.Service != null ? bs.Service.Name : string.Empty,
                        Quantity = bs.Quantity,
                        UnitPrice = bs.UnitPrice,
                        TotalPrice = bs.TotalPrice,
                        UsedAt = bs.UsedAt
                    })
                    .ToList() ?? new List<InvoiceServiceItemViewModel>(),
                Payments = invoice.Payments
                    .Select(p => new InvoicePaymentItemViewModel
                    {
                        PaymentCode = p.PaymentCode,
                        PaymentMethod = p.PaymentMethod,
                        Amount = p.Amount,
                        Status = p.Status,
                        PaidAt = p.PaidAt,
                        CreatedByName = p.CreatedByUser?.FullName,
                        Note = p.Note
                    })
                    .ToList()
            };
        }
    }
}
