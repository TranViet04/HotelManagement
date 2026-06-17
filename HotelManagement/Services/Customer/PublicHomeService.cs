using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.ViewModels.Customer;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Customer
{
    public class PublicHomeService
    {
        private readonly HotelDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PublicHomeService(HotelDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

            foreach (var room in featuredRooms)
            {
                room.ThumbnailUrl = ResolveImageUrl(room.RoomTypeId, room.Name, room.ThumbnailUrl);
            }

            return new HomeViewModel
            {
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1),
                Adults = 1,
                Children = 0,
                FeaturedRooms = featuredRooms
            };
        }

        private string ResolveImageUrl(long roomTypeId, string roomTypeName, string? thumbnailUrl)
        {
            if (ImageExists(thumbnailUrl))
            {
                return thumbnailUrl!;
            }

            var uploadedRoomTypeImage = GetFirstUploadedRoomTypeImage(roomTypeId);

            if (!string.IsNullOrWhiteSpace(uploadedRoomTypeImage))
            {
                return uploadedRoomTypeImage;
            }

            var imageByName = GetImageUrlByRoomTypeName(roomTypeName);

            if (ImageExists(imageByName))
            {
                return imageByName;
            }

            var firstRoomImage = GetFirstImageFromFolder("rooms");

            if (!string.IsNullOrWhiteSpace(firstRoomImage))
            {
                return firstRoomImage;
            }

            return "/images/rooms/default-room.jpg";
        }

        private string? GetFirstUploadedRoomTypeImage(long roomTypeId)
        {
            if (string.IsNullOrWhiteSpace(_environment.WebRootPath))
            {
                return null;
            }

            var uploadFolder = Path.Combine(_environment.WebRootPath, "images", "room-types", roomTypeId.ToString());

            if (!Directory.Exists(uploadFolder))
            {
                return null;
            }

            var file = Directory
                .GetFiles(uploadFolder)
                .Where(IsSupportedImage)
                .OrderBy(path => path)
                .FirstOrDefault();

            return file == null
                ? null
                : $"/images/room-types/{roomTypeId}/{Path.GetFileName(file)}";
        }

        private string? GetFirstImageFromFolder(string folderName)
        {
            if (string.IsNullOrWhiteSpace(_environment.WebRootPath))
            {
                return null;
            }

            var folder = Path.Combine(_environment.WebRootPath, "images", folderName);

            if (!Directory.Exists(folder))
            {
                return null;
            }

            var file = Directory
                .GetFiles(folder)
                .Where(IsSupportedImage)
                .OrderBy(path => path)
                .FirstOrDefault();

            return file == null
                ? null
                : $"/images/{folderName}/{Path.GetFileName(file)}";
        }

        private bool ImageExists(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || string.IsNullOrWhiteSpace(_environment.WebRootPath))
            {
                return false;
            }

            var relativePath = imageUrl.Split('?', '#')[0].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_environment.WebRootPath, relativePath);

            return File.Exists(physicalPath);
        }

        private static bool IsSupportedImage(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension is ".jpg" or ".jpeg" or ".png" or ".webp";
        }

        private static string GetImageUrlByRoomTypeName(string roomTypeName)
        {
            var name = roomTypeName.ToLowerInvariant();

            if (name.Contains("standard"))
            {
                return "/images/rooms/standard.jpg";
            }

            if (name.Contains("deluxe"))
            {
                return "/images/rooms/deluxe.jpg";
            }

            if (name.Contains("suite"))
            {
                return "/images/rooms/suite.jpg";
            }

            if (name.Contains("premium"))
            {
                return "/images/rooms/premium.jpg";
            }

            if (name.Contains("family"))
            {
                return "/images/rooms/family.jpg";
            }

            return "/images/rooms/default-room.jpg";
        }
    }
}
