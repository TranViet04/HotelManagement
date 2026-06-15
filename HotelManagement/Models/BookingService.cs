using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class BookingService
    {
        public long Id { get; set; }

        public long BookingId { get; set; }

        public long ServiceId { get; set; }

        public long? CreatedByUserId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Note { get; set; }

        public Booking? Booking { get; set; }

        public Service? Service { get; set; }

        public User? CreatedByUser { get; set; }
    }
}
