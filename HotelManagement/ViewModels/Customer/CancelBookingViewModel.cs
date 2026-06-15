using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels.Customer
{
    public class CancelBookingViewModel
    {
        public long BookingId { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Nights { get; set; }

        public int Adults { get; set; }

        public int Children { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool CanCancel { get; set; }

        public string? CancelBlockReason { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do hủy đặt phòng")]
        [MaxLength(500, ErrorMessage = "Lý do hủy tối đa 500 ký tự")]
        public string? CancelReason { get; set; }
    }
}
