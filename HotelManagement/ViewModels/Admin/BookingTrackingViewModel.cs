namespace HotelManagement.ViewModels.Admin
{
    public class BookingTrackingViewModel
    {
        public long Id { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        public string? CustomerEmail { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Adults { get; set; }

        public int Children { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal TotalRoomAmount { get; set; }

        public decimal TotalServiceAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
