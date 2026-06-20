namespace HotelManagement.ViewModels.Customer
{
    public class BookingDetailViewModel
    {
        public long BookingId { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerEmail { get; set; }

        public string? CustomerPhoneNumber { get; set; }

        public long RoomId { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public int? Floor { get; set; }

        public string RoomTypeName { get; set; } = string.Empty;

        public string? RoomDescription { get; set; }

        public decimal PricePerNight { get; set; }

        public int Capacity { get; set; }

        public string? BedType { get; set; }

        public string? ThumbnailUrl { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Nights { get; set; }

        public int Adults { get; set; }

        public int Children { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal TotalRoomAmount { get; set; }

        public decimal TotalServiceAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public string? SpecialRequest { get; set; }

        public string? CancelReason { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        public DateTime? CheckedInAt { get; set; }

        public DateTime? CheckedOutAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool CanCancel { get; set; }

        public List<BookingServiceItemViewModel> Services { get; set; } = new();
    }

    public class BookingServiceItemViewModel
    {
        public string ServiceName { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? Unit { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime UsedAt { get; set; }

        public string? Note { get; set; }
    }
}
