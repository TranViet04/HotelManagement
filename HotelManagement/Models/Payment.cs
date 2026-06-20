using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class Payment
    {
        public long Id { get; set; }

        public long InvoiceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? QrExpiresAt { get; set; }

        public long? SepayTransactionId { get; set; }

        public long? CreatedByUserId { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public Invoice? Invoice { get; set; }

        public User? CreatedByUser { get; set; }
    }
}
