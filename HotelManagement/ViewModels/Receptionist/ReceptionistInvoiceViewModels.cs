using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.ViewModels.Receptionist
{
    public class ReceptionistInvoiceListViewModel
    {
        public string? Keyword { get; set; }

        public string? Status { get; set; }

        public List<SelectListItem> StatusOptions { get; set; } = new();

        public List<ReceptionistInvoiceListItemViewModel> Invoices { get; set; } = new();
    }

    public class ReceptionistInvoiceListItemViewModel
    {
        public long InvoiceId { get; set; }

        public string InvoiceCode { get; set; } = string.Empty;

        public long BookingId { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerPhoneNumber { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAt { get; set; }

        public string? IssuedByName { get; set; }
    }

    public class ReceptionistInvoiceDetailViewModel
    {
        public long InvoiceId { get; set; }

        public string InvoiceCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAt { get; set; }

        public string? IssuedByName { get; set; }

        public string? Note { get; set; }

        public long BookingId { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string BookingStatus { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerEmail { get; set; }

        public string? CustomerPhoneNumber { get; set; }

        public string? CustomerIdentityNumber { get; set; }

        public string? CustomerAddress { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Nights { get; set; }

        public decimal RoomAmount { get; set; }

        public decimal ServiceAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public DateTime? PaidAt { get; set; }

        public bool CanRecordPayment { get; set; }

        public string? RecordPaymentBlockReason { get; set; }

        public List<ReceptionistInvoiceServiceLineViewModel> Services { get; set; } = new();

        public List<ReceptionistInvoicePaymentLineViewModel> Payments { get; set; } = new();
    }

    public class ReceptionistInvoiceServiceLineViewModel
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

    public class ReceptionistInvoicePaymentLineViewModel
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
