using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.ViewModels.Receptionist
{
    public class AddBookingServiceViewModel
    {
        public long BookingId { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public string BookingStatus { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public decimal TotalRoomAmount { get; set; }

        public decimal TotalServiceAmount { get; set; }

        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn dịch vụ")]
        public long ServiceId { get; set; }

        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
        public int Quantity { get; set; } = 1;

        [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
        public string? Note { get; set; }

        public bool CanAddService { get; set; }

        public string? AddServiceBlockReason { get; set; }

        public List<SelectListItem> ServiceOptions { get; set; } = new();

        public List<ReceptionistBookingServiceItemViewModel> ExistingServices { get; set; } = new();
    }

    public class ReceptionistBookingServiceItemViewModel
    {
        public long BookingServiceId { get; set; }

        public string ServiceName { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? Unit { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime UsedAt { get; set; }

        public string? Note { get; set; }

        public string? CreatedByName { get; set; }
    }
}
