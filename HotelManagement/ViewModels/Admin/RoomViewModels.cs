using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.ViewModels.Admin
{
    public class RoomListItemViewModel
    {
        public long Id { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public int? Floor { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Note { get; set; }

        public string? MainImageUrl { get; set; }

        public int ImageCount { get; set; }
    }

    public class RoomFormViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại phòng")]
        public long RoomTypeId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số phòng")]
        [MaxLength(50)]
        public string RoomNumber { get; set; } = string.Empty;

        public int? Floor { get; set; }

        [Required]
        public string Status { get; set; } = "Available";

        [MaxLength(500)]
        public string? Note { get; set; }

        public List<SelectListItem> RoomTypeOptions { get; set; } = new();

        public List<RoomImageViewModel> ExistingImages { get; set; } = new();
    }

    public class RoomImageViewModel
    {
        public long Id { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public string? Caption { get; set; }

        public bool IsMain { get; set; }

        public int SortOrder { get; set; }
    }

    public class RoomDetailViewModel
    {
        public long Id { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public string? RoomTypeDescription { get; set; }

        public decimal RoomTypePrice { get; set; }

        public int RoomTypeCapacity { get; set; }

        public string? BedType { get; set; }

        public int? Floor { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<RoomImageViewModel> Images { get; set; } = new();

        public List<RoomBookingHistoryItemViewModel> RecentBookings { get; set; } = new();
    }

    public class RoomBookingHistoryItemViewModel
    {
        public long Id { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
    }
}
