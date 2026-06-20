namespace HotelManagement.ViewModels.Payment
{
    public class CheckoutViewModel
    {
        public long PaymentId { get; set; }

        public long InvoiceId { get; set; }

        public long BookingId { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public string RoomNumber { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Nights { get; set; }

        public decimal RoomAmount { get; set; }

        public decimal ServiceAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public string QrCodeUrl { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;

        public string BankAccount { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;

        public string TransferContent { get; set; } = string.Empty;

        public DateTime QrExpiresAt { get; set; }

        public bool IsQrExpired { get; set; }

        public string PaymentStatus { get; set; } = string.Empty;

        public List<CheckoutServiceItemViewModel> Services { get; set; } = new();
    }

    public class CheckoutServiceItemViewModel
    {
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal TotalPrice { get; set; }
    }
}
