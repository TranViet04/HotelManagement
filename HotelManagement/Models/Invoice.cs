using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class Invoice
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceCode { get; set; } = string.Empty;

        public long BookingId { get; set; }

        public decimal RoomAmount { get; set; } = 0;

        public decimal ServiceAmount { get; set; } = 0;

        public decimal DiscountAmount { get; set; } = 0;

        public decimal TaxAmount { get; set; } = 0;

        public decimal TotalAmount { get; set; } = 0;

        public decimal PaidAmount { get; set; } = 0;

        public decimal RemainingAmount { get; set; } = 0;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Unpaid";

        public long? IssuedByUserId { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.Now;

        public DateTime? PaidAt { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public Booking? Booking { get; set; }

        public User? IssuedByUser { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
