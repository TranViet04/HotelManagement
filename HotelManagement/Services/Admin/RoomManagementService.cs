using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Admin
{
    public class RoomManagementService
    {
        private readonly HotelDbContext _context;

        public RoomManagementService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoomListItemViewModel>> GetAllAsync()
        {
            return await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .OrderBy(r => r.RoomNumber)
                .Select(r => new RoomListItemViewModel
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    RoomTypeName = r.RoomType != null ? r.RoomType.Name : string.Empty,
                    Floor = r.Floor,
                    Status = r.Status,
                    Note = r.Note,
                    MainImageUrl = r.RoomImages
                        .OrderByDescending(i => i.IsMain)
                        .ThenBy(i => i.SortOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),
                    ImageCount = r.RoomImages.Count
                })
                .ToListAsync();
        }

        public async Task<RoomFormViewModel> GetCreateModelAsync()
        {
            return new RoomFormViewModel
            {
                Status = RoomStatuses.Available,
                RoomTypeOptions = await GetRoomTypeOptionsAsync()
            };
        }

        public async Task<RoomFormViewModel?> GetForEditAsync(long id)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return null;
            }

            return new RoomFormViewModel
            {
                Id = room.Id,
                RoomTypeId = room.RoomTypeId,
                RoomNumber = room.RoomNumber,
                Floor = room.Floor,
                Status = room.Status,
                Note = room.Note,
                RoomTypeOptions = await GetRoomTypeOptionsAsync(),
                ExistingImages = room.RoomImages
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new RoomImageViewModel
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        Caption = i.Caption,
                        IsMain = i.IsMain,
                        SortOrder = i.SortOrder
                    })
                    .ToList()
            };
        }

        public async Task<RoomDetailViewModel?> GetDetailAsync(long id)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return null;
            }

            return new RoomDetailViewModel
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                RoomTypeName = room.RoomType != null ? room.RoomType.Name : string.Empty,
                RoomTypeDescription = room.RoomType?.Description,
                RoomTypePrice = room.RoomType != null ? room.RoomType.Price : 0,
                RoomTypeCapacity = room.RoomType != null ? room.RoomType.Capacity : 0,
                BedType = room.RoomType?.BedType,
                Floor = room.Floor,
                Status = room.Status,
                Note = room.Note,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.UpdatedAt,
                Images = room.RoomImages
                    .OrderByDescending(i => i.IsMain)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => new RoomImageViewModel
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        Caption = i.Caption,
                        IsMain = i.IsMain,
                        SortOrder = i.SortOrder
                    })
                    .ToList(),
                RecentBookings = room.Bookings
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .Select(b => new RoomBookingHistoryItemViewModel
                    {
                        Id = b.Id,
                        BookingCode = b.BookingCode,
                        CustomerName = b.Customer != null ? b.Customer.FullName : string.Empty,
                        CheckInDate = b.CheckInDate,
                        CheckOutDate = b.CheckOutDate,
                        Status = b.Status,
                        TotalAmount = b.TotalAmount
                    })
                    .ToList()
            };
        }

        public async Task<bool> IsRoomNumberExistsAsync(string roomNumber, long? ignoreId = null)
        {
            var normalizedRoomNumber = roomNumber.Trim();
            var query = _context.Rooms.AsQueryable();

            if (ignoreId.HasValue)
            {
                query = query.Where(r => r.Id != ignoreId.Value);
            }

            return await query.AnyAsync(r => r.RoomNumber == normalizedRoomNumber);
        }

        public async Task<bool> RoomTypeExistsAsync(long roomTypeId)
        {
            return await _context.RoomTypes.AnyAsync(rt => rt.Id == roomTypeId);
        }

        public async Task<long> CreateAsync(RoomFormViewModel model)
        {
            var room = new Room
            {
                RoomTypeId = model.RoomTypeId,
                RoomNumber = model.RoomNumber.Trim(),
                Floor = model.Floor,
                Status = model.Status,
                Note = model.Note,
                CreatedAt = DateTime.Now
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return room.Id;
        }

        public async Task<bool> UpdateAsync(RoomFormViewModel model)
        {
            var room = await _context.Rooms.FindAsync(model.Id);

            if (room == null)
            {
                return false;
            }

            room.RoomTypeId = model.RoomTypeId;
            room.RoomNumber = model.RoomNumber.Trim();
            room.Floor = model.Floor;
            room.Status = model.Status;
            room.Note = model.Note;
            room.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddImagesAsync(long roomId, List<string> imageUrls)
        {
            if (!imageUrls.Any())
            {
                return;
            }

            var hasMainImage = await _context.RoomImages
                .AnyAsync(i => i.RoomId == roomId && i.IsMain);

            var currentMaxSortOrder = await _context.RoomImages
                .Where(i => i.RoomId == roomId)
                .Select(i => (int?)i.SortOrder)
                .MaxAsync() ?? 0;

            var images = imageUrls.Select((url, index) => new RoomImage
            {
                RoomId = roomId,
                ImageUrl = url,
                IsMain = !hasMainImage && index == 0,
                SortOrder = currentMaxSortOrder + index + 1,
                CreatedAt = DateTime.Now
            }).ToList();

            _context.RoomImages.AddRange(images);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteImageAsync(long imageId)
        {
            var image = await _context.RoomImages.FindAsync(imageId);

            if (image == null)
            {
                return false;
            }

            _context.RoomImages.Remove(image);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetMainImageAsync(long imageId)
        {
            var image = await _context.RoomImages.FindAsync(imageId);

            if (image == null)
            {
                return false;
            }

            var roomImages = await _context.RoomImages
                .Where(i => i.RoomId == image.RoomId)
                .ToListAsync();

            foreach (var item in roomImages)
            {
                item.IsMain = item.Id == imageId;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SelectListItem>> GetRoomTypeOptionsAsync()
        {
            return await _context.RoomTypes
                .Where(rt => rt.Status == "Active")
                .OrderBy(rt => rt.Name)
                .Select(rt => new SelectListItem
                {
                    Value = rt.Id.ToString(),
                    Text = rt.Name
                })
                .ToListAsync();
        }
    }
}
