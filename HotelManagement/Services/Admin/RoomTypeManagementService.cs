using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Admin
{
    public class RoomTypeManagementService
    {
        private readonly HotelDbContext _context;

        public RoomTypeManagementService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoomTypeListItemViewModel>> GetAllAsync()
        {
            return await _context.RoomTypes
                .OrderByDescending(rt => rt.CreatedAt)
                .Select(rt => new RoomTypeListItemViewModel
                {
                    Id = rt.Id,
                    Name = rt.Name,
                    Description = rt.Description,
                    Price = rt.Price,
                    Capacity = rt.Capacity,
                    BedType = rt.BedType,
                    ThumbnailUrl = rt.ThumbnailUrl,
                    Status = rt.Status
                })
                .ToListAsync();
        }

        public async Task<RoomTypeFormViewModel?> GetForEditAsync(long id)
        {
            var roomType = await _context.RoomTypes.FindAsync(id);

            if (roomType == null)
            {
                return null;
            }

            return new RoomTypeFormViewModel
            {
                Id = roomType.Id,
                Name = roomType.Name,
                Description = roomType.Description,
                Price = roomType.Price,
                Capacity = roomType.Capacity,
                BedType = roomType.BedType,
                ThumbnailUrl = roomType.ThumbnailUrl,
                Status = roomType.Status
            };
        }

        public async Task<bool> IsNameExistsAsync(string name, long? ignoreId = null)
        {
            var query = _context.RoomTypes.AsQueryable();

            if (ignoreId.HasValue)
            {
                query = query.Where(rt => rt.Id != ignoreId.Value);
            }

            return await query.AnyAsync(rt => rt.Name == name);
        }

        public async Task<long> CreateAsync(RoomTypeFormViewModel model)
        {
            var roomType = new RoomType
            {
                Name = model.Name.Trim(),
                Description = model.Description,
                Price = model.Price,
                Capacity = model.Capacity,
                BedType = model.BedType,
                ThumbnailUrl = model.ThumbnailUrl,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.RoomTypes.Add(roomType);
            await _context.SaveChangesAsync();

            return roomType.Id;
        }

        public async Task<bool> UpdateAsync(RoomTypeFormViewModel model)
        {
            var roomType = await _context.RoomTypes.FindAsync(model.Id);

            if (roomType == null)
            {
                return false;
            }

            roomType.Name = model.Name.Trim();
            roomType.Description = model.Description;
            roomType.Price = model.Price;
            roomType.Capacity = model.Capacity;
            roomType.BedType = model.BedType;
            roomType.ThumbnailUrl = model.ThumbnailUrl;
            roomType.Status = model.Status;
            roomType.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(long id)
        {
            var roomType = await _context.RoomTypes.FindAsync(id);

            if (roomType == null)
            {
                return false;
            }

            roomType.Status = "Inactive";
            roomType.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
