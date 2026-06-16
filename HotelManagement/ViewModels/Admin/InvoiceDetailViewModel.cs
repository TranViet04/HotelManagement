namespace HotelManagement.ViewModels.Admin
{
    public class InvoiceDetailViewModel
    {
        public long Id { get; set; }

        public string InvoiceCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string BookingCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        public string? CustomerEmail { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public decimal RoomAmount { get; set; }

        public decimal ServiceAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public string? IssuedByName { get; set; }

        public DateTime IssuedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public string? Note { get; set; }

        public List<InvoiceServiceItemViewModel> Services { get; set; } = new();

        public List<InvoicePaymentItemViewModel> Payments { get; set; } = new();
    }

    public class InvoiceServiceItemViewModel
    {
        public string ServiceName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime UsedAt { get; set; }
    }

    public class InvoicePaymentItemViewModel
    {
        public string PaymentCode { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime PaidAt { get; set; }

        public string? CreatedByName { get; set; }

        public string? Note { get; set; }
    }
}
