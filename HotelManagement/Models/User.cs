using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class User
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Role { get; set; } = "Customer";
        // Admin, Receptionist, Customer

        [MaxLength(50)]
        public string? IdentityNumber { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Active";
        // Active, Locked, Inactive

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Booking> CustomerBookings { get; set; } = new List<Booking>();

        public ICollection<Booking> CreatedBookings { get; set; } = new List<Booking>();

        public ICollection<BookingService> CreatedBookingServices { get; set; } = new List<BookingService>();

        public ICollection<Invoice> IssuedInvoices { get; set; } = new List<Invoice>();

        public ICollection<Payment> CreatedPayments { get; set; } = new List<Payment>();

        public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    }
}
