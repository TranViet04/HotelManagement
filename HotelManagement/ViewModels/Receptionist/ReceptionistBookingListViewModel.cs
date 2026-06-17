using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.ViewModels.Receptionist
{
    public class ReceptionistBookingListViewModel
    {
        public string? Keyword { get; set; }

        public string? Status { get; set; }

        public DateTime? CheckInFrom { get; set; }

        public DateTime? CheckInTo { get; set; }

        public List<SelectListItem> StatusOptions { get; set; } = new();

        public List<ReceptionistBookingListItemViewModel> Bookings { get; set; } = new();
    }

    public class ReceptionistBookingListItemViewModel
    {
        public long BookingId { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerPhoneNumber { get; set; }

        public string? CustomerEmail { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Nights { get; set; }

        public int Adults { get; set; }

        public int Children { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? InvoiceStatus { get; set; }

        public decimal PaidAmount { get; set; }
    }
}
