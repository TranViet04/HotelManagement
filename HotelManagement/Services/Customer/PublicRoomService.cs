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
    }
}
