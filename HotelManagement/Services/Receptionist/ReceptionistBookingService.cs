using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.ViewModels.Receptionist;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Receptionist
{
    public class ReceptionistBookingService
    {
        private readonly HotelDbContext _context;

        public ReceptionistBookingService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<ReceptionistBookingListViewModel> GetBookingsAsync(
            string? keyword,
            string? status,
            DateTime? checkInFrom,
            DateTime? checkInTo)
        {
            keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
            status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();

            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(b =>
                    b.BookingCode.Contains(keyword)
                    || (b.Customer != null && b.Customer.FullName.Contains(keyword))
                    || (b.Customer != null && b.Customer.Email.Contains(keyword))
                    || (b.Customer != null && b.Customer.PhoneNumber != null && b.Customer.PhoneNumber.Contains(keyword))
                    || (b.Room != null && b.Room.RoomNumber.Contains(keyword))
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (checkInFrom.HasValue)
            {
                var from = checkInFrom.Value.Date;
                query = query.Where(b => b.CheckInDate >= from);
            }

            if (checkInTo.HasValue)
            {
                var to = checkInTo.Value.Date.AddDays(1);
                query = query.Where(b => b.CheckInDate < to);
            }

            var bookingRows = await query
                .OrderByDescending(b => b.CreatedAt)
                .ThenByDescending(b => b.Id)
                .Select(b => new
                {
                    BookingId = b.Id,
                    BookingCode = b.BookingCode,
                    CustomerName = b.Customer != null ? b.Customer.FullName : "Không xác định",
                    CustomerPhoneNumber = b.Customer != null ? b.Customer.PhoneNumber : null,
                    CustomerEmail = b.Customer != null ? b.Customer.Email : null,
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : "Không xác định",
                    RoomTypeName = b.Room != null && b.Room.RoomType != null
                        ? b.Room.RoomType.Name
                        : "Không xác định",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            var bookings = bookingRows
                .Select(b => new ReceptionistBookingListItemViewModel
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    CustomerName = b.CustomerName,
                    CustomerPhoneNumber = b.CustomerPhoneNumber,
                    CustomerEmail = b.CustomerEmail,
                    RoomNumber = b.RoomNumber,
                    RoomTypeName = b.RoomTypeName,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Nights = Math.Max(0, (b.CheckOutDate.Date - b.CheckInDate.Date).Days),
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToList();

            return new ReceptionistBookingListViewModel
            {
                Keyword = keyword,
                Status = status,
                CheckInFrom = checkInFrom,
                CheckInTo = checkInTo,
                StatusOptions = GetStatusOptions(status),
                Bookings = bookings
            };
        }

        private static List<SelectListItem> GetStatusOptions(string? selectedStatus)
        {
            return new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "",
                    Text = "Tất cả trạng thái",
                    Selected = string.IsNullOrWhiteSpace(selectedStatus)
                },
                new SelectListItem
                {
                    Value = BookingStatuses.Pending,
                    Text = "Chờ xác nhận",
                    Selected = selectedStatus == BookingStatuses.Pending
                },
                new SelectListItem
                {
                    Value = BookingStatuses.Confirmed,
                    Text = "Đã xác nhận",
                    Selected = selectedStatus == BookingStatuses.Confirmed
                },
                new SelectListItem
                {
                    Value = BookingStatuses.CheckedIn,
                    Text = "Đã nhận phòng",
                    Selected = selectedStatus == BookingStatuses.CheckedIn
                },
                new SelectListItem
                {
                    Value = BookingStatuses.CheckedOut,
                    Text = "Đã trả phòng",
                    Selected = selectedStatus == BookingStatuses.CheckedOut
                },
                new SelectListItem
                {
                    Value = BookingStatuses.Cancelled,
                    Text = "Đã hủy",
                    Selected = selectedStatus == BookingStatuses.Cancelled
                }
            };
        }
    }
}
