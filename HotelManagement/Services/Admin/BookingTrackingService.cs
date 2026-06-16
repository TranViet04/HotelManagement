using HotelManagement.Data;
using HotelManagement.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Admin
{
    public class BookingTrackingService
    {
        private readonly HotelDbContext _context;

        public BookingTrackingService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<BookingTrackingViewModel>> GetBookingsAsync(
            string? keyword,
            string? status,
            DateTime? checkInDate)
        {
            var query = _context.Bookings
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();

                query = query.Where(b =>
                    b.BookingCode.Contains(normalizedKeyword) ||
                    (b.Customer != null && b.Customer.FullName.Contains(normalizedKeyword)) ||
                    (b.Customer != null && b.Customer.PhoneNumber != null && b.Customer.PhoneNumber.Contains(normalizedKeyword)) ||
                    (b.Room != null && b.Room.RoomNumber.Contains(normalizedKeyword)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (checkInDate.HasValue)
            {
                var fromDate = checkInDate.Value.Date;
                var toDate = fromDate.AddDays(1);

                query = query.Where(b =>
                    b.CheckInDate >= fromDate &&
                    b.CheckInDate < toDate);
            }

            return await query
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BookingTrackingViewModel
                {
                    Id = b.Id,
                    BookingCode = b.BookingCode,
                    CustomerName = b.Customer != null ? b.Customer.FullName : string.Empty,
                    CustomerPhone = b.Customer != null ? b.Customer.PhoneNumber : null,
                    CustomerEmail = b.Customer != null ? b.Customer.Email : null,
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : string.Empty,
                    RoomTypeName = b.Room != null && b.Room.RoomType != null ? b.Room.RoomType.Name : string.Empty,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalRoomAmount = b.TotalRoomAmount,
                    TotalServiceAmount = b.TotalServiceAmount,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<BookingDetailViewModel?> GetDetailAsync(long id)
        {
            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
                .Include(b => b.Invoice)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return null;
            }

            return new BookingDetailViewModel
            {
                Id = booking.Id,
                BookingCode = booking.BookingCode,
                Status = booking.Status,
                CustomerName = booking.Customer != null ? booking.Customer.FullName : string.Empty,
                CustomerPhone = booking.Customer?.PhoneNumber,
                CustomerEmail = booking.Customer?.Email,
                CustomerIdentityNumber = booking.Customer?.IdentityNumber,
                CustomerAddress = booking.Customer?.Address,
                RoomNumber = booking.Room != null ? booking.Room.RoomNumber : string.Empty,
                RoomTypeName = booking.Room?.RoomType != null ? booking.Room.RoomType.Name : string.Empty,
                RoomPrice = booking.Room?.RoomType != null ? booking.Room.RoomType.Price : 0,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Adults = booking.Adults,
                Children = booking.Children,
                TotalRoomAmount = booking.TotalRoomAmount,
                TotalServiceAmount = booking.TotalServiceAmount,
                TotalAmount = booking.TotalAmount,
                SpecialRequest = booking.SpecialRequest,
                CancelReason = booking.CancelReason,
                ConfirmedAt = booking.ConfirmedAt,
                CheckedInAt = booking.CheckedInAt,
                CheckedOutAt = booking.CheckedOutAt,
                CancelledAt = booking.CancelledAt,
                CreatedAt = booking.CreatedAt,
                InvoiceCode = booking.Invoice?.InvoiceCode,
                InvoiceStatus = booking.Invoice?.Status,
                Services = booking.BookingServices
                    .Select(bs => new BookingServiceItemViewModel
                    {
                        ServiceName = bs.Service != null ? bs.Service.Name : string.Empty,
                        Quantity = bs.Quantity,
                        UnitPrice = bs.UnitPrice,
                        TotalPrice = bs.TotalPrice,
                        UsedAt = bs.UsedAt
                    })
                    .ToList()
            };
        }
    }
}
