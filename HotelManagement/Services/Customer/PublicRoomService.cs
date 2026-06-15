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
    }
}
