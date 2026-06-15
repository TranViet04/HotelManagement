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

        public async Task<List<MyBookingListItemViewModel>> GetMyBookingsAsync(long customerId)
        {
            var cancellableStatuses = new[]
            {
                BookingStatuses.Pending,
                BookingStatuses.Confirmed
            };

            var bookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new MyBookingListItemViewModel
                {
                    BookingId = b.Id,
                    BookingCode = b.BookingCode,
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : "Không xác định",
                    RoomTypeName = b.Room != null && b.Room.RoomType != null
                        ? b.Room.RoomType.Name
                        : "Không xác định",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Adults = b.Adults,
                    Children = b.Children,
                    TotalAmount = b.TotalAmount,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    CanCancel = cancellableStatuses.Contains(b.Status)
                })
                .ToListAsync();

            foreach (var booking in bookings)
            {
                booking.Nights = Math.Max(0, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days);
            }

            return bookings;
        }

        public async Task<BookingDetailViewModel?> GetMyBookingDetailAsync(long bookingId, long customerId)
        {
            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

            if (booking == null)
            {
                return null;
            }

            var cancellableStatuses = new[]
            {
                BookingStatuses.Pending,
                BookingStatuses.Confirmed
            };

            return new BookingDetailViewModel
            {
                BookingId = booking.Id,
                BookingCode = booking.BookingCode,
                CustomerName = booking.Customer?.FullName ?? "Không xác định",
                CustomerEmail = booking.Customer?.Email,
                CustomerPhoneNumber = booking.Customer?.PhoneNumber,
                RoomId = booking.RoomId,
                RoomNumber = booking.Room?.RoomNumber ?? "Không xác định",
                Floor = booking.Room?.Floor,
                RoomTypeName = booking.Room?.RoomType?.Name ?? "Không xác định",
                RoomDescription = booking.Room?.RoomType?.Description,
                PricePerNight = booking.Room?.RoomType?.Price ?? 0,
                Capacity = booking.Room?.RoomType?.Capacity ?? 0,
                BedType = booking.Room?.RoomType?.BedType,
                ThumbnailUrl = booking.Room?.RoomType?.ThumbnailUrl,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Nights = Math.Max(0, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days),
                Adults = booking.Adults,
                Children = booking.Children,
                Status = booking.Status,
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
                CanCancel = cancellableStatuses.Contains(booking.Status),
                Services = booking.BookingServices
                    .OrderByDescending(bs => bs.UsedAt)
                    .Select(bs => new BookingServiceItemViewModel
                    {
                        ServiceName = bs.Service?.Name ?? "Dịch vụ không xác định",
                        Category = bs.Service?.Category,
                        Unit = bs.Service?.Unit,
                        Quantity = bs.Quantity,
                        UnitPrice = bs.UnitPrice,
                        TotalPrice = bs.TotalPrice,
                        UsedAt = bs.UsedAt,
                        Note = bs.Note
                    })
                    .ToList()
            };
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
