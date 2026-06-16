using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels.Admin
{
    public class CustomerListItemViewModel
    {
        public long Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? IdentityNumber { get; set; }

        public string Status { get; set; } = string.Empty;

        public int BookingCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CustomerDetailViewModel
    {
        public long Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? IdentityNumber { get; set; }

        public string? Address { get; set; }

        public string Status { get; set; } = string.Empty;

        public int BookingCount { get; set; }

        public decimal TotalBookingAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<CustomerBookingHistoryItemViewModel> RecentBookings { get; set; } = new();
    }

    public class CustomerBookingHistoryItemViewModel
    {
        public long Id { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class EditCustomerViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(50)]
        public string? IdentityNumber { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        public string Status { get; set; } = "Active";
    }
}
