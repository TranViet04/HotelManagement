namespace HotelManagement.ViewModels.Admin
{
    public class InvoiceTrackingViewModel
    {
        public long Id { get; set; }

        public string InvoiceCode { get; set; } = string.Empty;

        public string BookingCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public decimal RoomAmount { get; set; }

        public decimal ServiceAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAt { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}
