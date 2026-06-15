using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class Booking
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BookingCode { get; set; } = string.Empty;

        public long CustomerId { get; set; }

        public long RoomId { get; set; }

        public long? CreatedByUserId { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Adults { get; set; } = 1;

        public int Children { get; set; } = 0;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public decimal TotalRoomAmount { get; set; } = 0;

        public decimal TotalServiceAmount { get; set; } = 0;

        public decimal TotalAmount { get; set; } = 0;

        [MaxLength(1000)]
        public string? SpecialRequest { get; set; }

        [MaxLength(500)]
        public string? CancelReason { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        public DateTime? CheckedInAt { get; set; }

        public DateTime? CheckedOutAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public User? Customer { get; set; }

        public User? CreatedByUser { get; set; }

        public Room? Room { get; set; }

        public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();

        public Invoice? Invoice { get; set; }
    }
}
