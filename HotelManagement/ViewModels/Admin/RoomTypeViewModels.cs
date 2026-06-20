using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels.Admin
{
    public class RoomTypeListItemViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Capacity { get; set; }

        public string? BedType { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public class RoomTypeFormViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên loại phòng")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0, 999999999, ErrorMessage = "Giá phòng không hợp lệ")]
        public decimal Price { get; set; }

        [Range(1, 20, ErrorMessage = "Sức chứa phải từ 1 đến 20")]
        public int Capacity { get; set; }

        [MaxLength(100)]
        public string? BedType { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        public List<string> ExistingImageUrls { get; set; } = new();
    }
}
