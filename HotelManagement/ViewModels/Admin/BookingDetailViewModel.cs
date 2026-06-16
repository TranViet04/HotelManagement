namespace HotelManagement.ViewModels.Admin
{
    public class BookingDetailViewModel
    {
        public long Id { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        public string? CustomerEmail { get; set; }

        public string? CustomerIdentityNumber { get; set; }

        public string? CustomerAddress { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public decimal RoomPrice { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Adults { get; set; }

        public int Children { get; set; }

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

        public List<BookingServiceItemViewModel> Services { get; set; } = new();

        public string? InvoiceCode { get; set; }

        public string? InvoiceStatus { get; set; }
    }

    public class BookingServiceItemViewModel
    {
        public string ServiceName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime UsedAt { get; set; }
    }
}
