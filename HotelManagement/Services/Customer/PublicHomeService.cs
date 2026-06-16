using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.ViewModels.Customer;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Customer
{
    public class PublicHomeService
    {
        private readonly HotelDbContext _context;

        public PublicHomeService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<HomeViewModel> GetHomePageAsync()
        {
            var featuredRooms = await _context.RoomTypes
                .AsNoTracking()
                .Where(rt => rt.Status == "Active")
                .OrderBy(rt => rt.Price)
                .Take(4)
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

            return new HomeViewModel
            {
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1),
                Adults = 1,
                Children = 0,
                FeaturedRooms = featuredRooms
            };
        }
    }
}
