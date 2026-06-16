using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.ViewModels.Receptionist
{
    public class ReceptionistPaymentListViewModel
    {
        public string? Keyword { get; set; }

        public List<ReceptionistPaymentInvoiceItemViewModel> Invoices { get; set; } = new();
    }

    public class ReceptionistPaymentInvoiceItemViewModel
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
    }

    public class RecordPaymentViewModel
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

        public string InvoiceStatus { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Range(1, double.MaxValue, ErrorMessage = "Số tiền thanh toán phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
        public string? Note { get; set; }

        public bool CanRecordPayment { get; set; }

        public string? RecordPaymentBlockReason { get; set; }

        public List<SelectListItem> PaymentMethodOptions { get; set; } = new();

        public List<ReceptionistInvoicePaymentLineViewModel> ExistingPayments { get; set; } = new();
    }
}
