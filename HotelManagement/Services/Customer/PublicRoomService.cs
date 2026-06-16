using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.ViewModels.Customer;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Customer
{
    public class PublicRoomService
    {
        private readonly HotelDbContext _context;

        public PublicRoomService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoomCardViewModel>> GetActiveRoomTypesAsync()
        {
            return await _context.RoomTypes
                .AsNoTracking()
                .Where(rt => rt.Status == "Active")
                .OrderBy(rt => rt.Price)
                .Select(rt => new RoomCardViewModel
                {
                    RoomTypeId = rt.Id,
                    Name = rt.Name,
                    Description = rt.Description,
                    Price = rt.Price,
                    Capacity = rt.Capacity,
                    BedType = rt.BedType,
                    ThumbnailUrl = rt.ThumbnailUrl,
                    AvailableRoomCount = rt.Rooms.Count(r => r.Status == RoomStatuses.Available)
                })
                .ToListAsync();
        }

        public async Task<List<AvailableRoomViewModel>> SearchAvailableRoomsAsync(RoomSearchViewModel model)
        {
            var checkInDate = model.CheckInDate.Date;
            var checkOutDate = model.CheckOutDate.Date;
            var totalGuests = model.Adults + model.Children;
            var nights = (checkOutDate - checkInDate).Days;

            var unavailableBookingStatuses = new[]
            {
                BookingStatuses.Pending,
                BookingStatuses.Confirmed,
                BookingStatuses.CheckedIn
            };

            var availableRooms = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .Where(r => r.Status == RoomStatuses.Available)
                .Where(r => r.RoomType != null && r.RoomType.Status == "Active")
                .Where(r => r.RoomType != null && r.RoomType.Capacity >= totalGuests)
                .Where(r => !r.Bookings.Any(b =>
                    unavailableBookingStatuses.Contains(b.Status)
                    && b.CheckInDate < checkOutDate
                    && b.CheckOutDate > checkInDate
                ))
                .OrderBy(r => r.RoomType!.Price)
                .ThenBy(r => r.RoomNumber)
                .Select(r => new AvailableRoomViewModel
                {
                    RoomId = r.Id,
                    RoomNumber = r.RoomNumber,
                    Floor = r.Floor,
                    RoomTypeId = r.RoomTypeId,
                    RoomTypeName = r.RoomType!.Name,
                    Description = r.RoomType.Description,
                    PricePerNight = r.RoomType.Price,
                    Capacity = r.RoomType.Capacity,
                    BedType = r.RoomType.BedType,
                    ThumbnailUrl = r.RoomType.ThumbnailUrl,
                    Nights = nights,
                    TotalRoomAmount = nights * r.RoomType.Price
                })
                .ToListAsync();

            return availableRooms;
        }

        public async Task<RoomDetailViewModel?> GetRoomDetailAsync(
            long roomTypeId,
            DateTime? checkInDate,
            DateTime? checkOutDate,
            int adults,
            int children)
        {
            var roomType = await _context.RoomTypes
                .AsNoTracking()
                .Where(rt => rt.Id == roomTypeId && rt.Status == "Active")
                .Select(rt => new RoomDetailViewModel
                {
                    RoomTypeId = rt.Id,
                    Name = rt.Name,
                    Description = rt.Description,
                    Price = rt.Price,
                    Capacity = rt.Capacity,
                    BedType = rt.BedType,
                    ThumbnailUrl = rt.ThumbnailUrl,
                    TotalRoomCount = rt.Rooms.Count(),
                    AvailableRoomCount = rt.Rooms.Count(r => r.Status == RoomStatuses.Available),
                    Adults = adults <= 0 ? 1 : adults,
                    Children = children < 0 ? 0 : children
                })
                .FirstOrDefaultAsync();

            if (roomType == null)
            {
                return null;
            }

            var hasSearchCriteria = checkInDate.HasValue || checkOutDate.HasValue;

            roomType.HasSearchCriteria = hasSearchCriteria;
            roomType.CheckInDate = checkInDate;
            roomType.CheckOutDate = checkOutDate;

            if (!hasSearchCriteria)
            {
                roomType.SearchMessage = "Chọn ngày nhận phòng và ngày trả phòng để kiểm tra phòng còn trống.";
                return roomType;
            }

            if (!checkInDate.HasValue || !checkOutDate.HasValue)
            {
                roomType.SearchMessage = "Vui lòng nhập đầy đủ ngày nhận phòng và ngày trả phòng.";
                return roomType;
            }

            var checkIn = checkInDate.Value.Date;
            var checkOut = checkOutDate.Value.Date;

            if (checkIn < DateTime.Today)
            {
                roomType.SearchMessage = "Ngày nhận phòng không được nhỏ hơn hôm nay.";
                return roomType;
            }

            if (checkOut <= checkIn)
            {
                roomType.SearchMessage = "Ngày trả phòng phải lớn hơn ngày nhận phòng.";
                return roomType;
            }

            var totalGuests = roomType.Adults + roomType.Children;

            if (totalGuests > roomType.Capacity)
            {
                roomType.SearchMessage = "Số lượng khách vượt quá sức chứa của loại phòng này.";
                return roomType;
            }

            roomType.HasValidSearchCriteria = true;

            var nights = (checkOut - checkIn).Days;

            var unavailableBookingStatuses = new[]
            {
                BookingStatuses.Pending,
                BookingStatuses.Confirmed,
                BookingStatuses.CheckedIn
            };

            roomType.AvailableRooms = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .Where(r => r.RoomTypeId == roomTypeId)
                .Where(r => r.Status == RoomStatuses.Available)
                .Where(r => r.RoomType != null && r.RoomType.Status == "Active")
                .Where(r => !r.Bookings.Any(b =>
                    unavailableBookingStatuses.Contains(b.Status)
                    && b.CheckInDate < checkOut
                    && b.CheckOutDate > checkIn
                ))
                .OrderBy(r => r.RoomNumber)
                .Select(r => new AvailableRoomViewModel
                {
                    RoomId = r.Id,
                    RoomNumber = r.RoomNumber,
                    Floor = r.Floor,
                    RoomTypeId = r.RoomTypeId,
                    RoomTypeName = r.RoomType!.Name,
                    Description = r.RoomType.Description,
                    PricePerNight = r.RoomType.Price,
                    Capacity = r.RoomType.Capacity,
                    BedType = r.RoomType.BedType,
                    ThumbnailUrl = r.RoomType.ThumbnailUrl,
                    Nights = nights,
                    TotalRoomAmount = nights * r.RoomType.Price
                })
                .ToListAsync();

            if (!roomType.AvailableRooms.Any())
            {
                roomType.SearchMessage = "Không còn phòng trống phù hợp trong khoảng thời gian đã chọn.";
            }

            return roomType;
        }
    }
}
