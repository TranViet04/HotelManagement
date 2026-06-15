using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class Room
    {
        public long Id { get; set; }

        public long RoomTypeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoomNumber { get; set; } = string.Empty;

        public int? Floor { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Available";

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public RoomType? RoomType { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
