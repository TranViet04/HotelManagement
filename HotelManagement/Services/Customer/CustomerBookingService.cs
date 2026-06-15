using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Customer;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Customer
{
    public class CustomerBookingService
    {
        private readonly HotelDbContext _context;

        public CustomerBookingService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<CreateBookingViewModel?> PrepareCreateBookingAsync(
            long roomId,
            DateTime? checkInDate,
            DateTime? checkOutDate,
            int adults,
            int children)
        {
            var room = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.RoomType == null)
            {
                return null;
            }

            if (room.Status != RoomStatuses.Available)
            {
                return null;
            }

            if (room.RoomType.Status != "Active")
            {
                return null;
            }

            var finalCheckInDate = (checkInDate ?? DateTime.Today).Date;
            var finalCheckOutDate = (checkOutDate ?? DateTime.Today.AddDays(1)).Date;
            var finalAdults = adults <= 0 ? 1 : adults;
            var finalChildren = children < 0 ? 0 : children;
            var nights = Math.Max(1, (finalCheckOutDate - finalCheckInDate).Days);

            var model = new CreateBookingViewModel
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                Floor = room.Floor,
                RoomTypeId = room.RoomTypeId,
                RoomTypeName = room.RoomType.Name,
                Description = room.RoomType.Description,
                PricePerNight = room.RoomType.Price,
                Capacity = room.RoomType.Capacity,
                BedType = room.RoomType.BedType,
                ThumbnailUrl = room.RoomType.ThumbnailUrl,
                CheckInDate = finalCheckInDate,
                CheckOutDate = finalCheckOutDate,
                Adults = finalAdults,
                Children = finalChildren,
                Nights = nights,
                TotalRoomAmount = nights * room.RoomType.Price
            };

            if (finalCheckInDate < DateTime.Today)
            {
                model.IsAvailableForSelectedDates = false;
                model.AvailabilityMessage = "Ngày nhận phòng không được nhỏ hơn hôm nay.";
                return model;
            }

            if (finalCheckOutDate <= finalCheckInDate)
            {
                model.IsAvailableForSelectedDates = false;
                model.AvailabilityMessage = "Ngày trả phòng phải lớn hơn ngày nhận phòng.";
                return model;
            }

            if (finalAdults + finalChildren > room.RoomType.Capacity)
            {
                model.IsAvailableForSelectedDates = false;
                model.AvailabilityMessage = "Số lượng khách vượt quá sức chứa của phòng.";
                return model;
            }

            var isAvailable = await IsRoomAvailableAsync(room.Id, finalCheckInDate, finalCheckOutDate);

            model.IsAvailableForSelectedDates = isAvailable;
            model.AvailabilityMessage = isAvailable
                ? "Phòng còn trống trong khoảng thời gian đã chọn."
                : "Phòng không còn trống trong khoảng thời gian đã chọn.";

            return model;
        }

        public async Task<CustomerBookingResult> CreateBookingAsync(CreateBookingViewModel model, long customerId)
        {
            var checkInDate = model.CheckInDate.Date;
            var checkOutDate = model.CheckOutDate.Date;

            if (model.RoomId <= 0)
            {
                return CustomerBookingResult.Failure("Phòng không hợp lệ.");
            }

            if (checkInDate < DateTime.Today)
            {
                return CustomerBookingResult.Failure("Ngày nhận phòng không được nhỏ hơn hôm nay.");
            }

            if (checkOutDate <= checkInDate)
            {
                return CustomerBookingResult.Failure("Ngày trả phòng phải lớn hơn ngày nhận phòng.");
            }

            if (model.Adults <= 0)
            {
                return CustomerBookingResult.Failure("Số người lớn phải lớn hơn 0.");
            }

            if (model.Children < 0)
            {
                return CustomerBookingResult.Failure("Số trẻ em không hợp lệ.");
            }

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == model.RoomId);

            if (room == null || room.RoomType == null)
            {
                return CustomerBookingResult.Failure("Không tìm thấy phòng.");
            }

            if (room.Status != RoomStatuses.Available)
            {
                return CustomerBookingResult.Failure("Phòng hiện không ở trạng thái có thể đặt.");
            }

            if (room.RoomType.Status != "Active")
            {
                return CustomerBookingResult.Failure("Loại phòng không còn hoạt động.");
            }

            if (model.Adults + model.Children > room.RoomType.Capacity)
            {
                return CustomerBookingResult.Failure("Số lượng khách vượt quá sức chứa của phòng.");
            }

            var isAvailable = await IsRoomAvailableAsync(room.Id, checkInDate, checkOutDate);

            if (!isAvailable)
            {
                return CustomerBookingResult.Failure("Phòng đã có người đặt trong khoảng thời gian này.");
            }

            var nights = (checkOutDate - checkInDate).Days;
            var totalRoomAmount = nights * room.RoomType.Price;
            var bookingCode = await GenerateBookingCodeAsync();

            var booking = new Booking
            {
                BookingCode = bookingCode,
                CustomerId = customerId,
                RoomId = room.Id,
                CreatedByUserId = customerId,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                Adults = model.Adults,
                Children = model.Children,
                Status = BookingStatuses.Pending,
                TotalRoomAmount = totalRoomAmount,
                TotalServiceAmount = 0,
                TotalAmount = totalRoomAmount,
                SpecialRequest = model.SpecialRequest,
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = customerId,
                Action = "CreateBooking",
                EntityName = "Booking",
                Description = $"Customer tạo booking online cho phòng {room.RoomNumber}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return CustomerBookingResult.Success(booking.Id, booking.BookingCode);
        }

        private async Task<bool> IsRoomAvailableAsync(long roomId, DateTime checkInDate, DateTime checkOutDate)
        {
            var unavailableBookingStatuses = new[]
            {
                BookingStatuses.Pending,
                BookingStatuses.Confirmed,
                BookingStatuses.CheckedIn
            };

            var hasOverlappingBooking = await _context.Bookings
                .AnyAsync(b =>
                    b.RoomId == roomId
                    && unavailableBookingStatuses.Contains(b.Status)
                    && b.CheckInDate < checkOutDate
                    && b.CheckOutDate > checkInDate
                );

            return !hasOverlappingBooking;
        }

        private async Task<string> GenerateBookingCodeAsync()
        {
            for (var i = 0; i < 5; i++)
            {
                var code = $"BK{DateTime.Now:yyyyMMddHHmmssfff}{Random.Shared.Next(100, 999)}";
                var exists = await _context.Bookings.AnyAsync(b => b.BookingCode == code);

                if (!exists)
                {
                    return code;
                }
            }

            return $"BK{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
        }
    }
}
