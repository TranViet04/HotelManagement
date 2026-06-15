using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class ActivityLog
    {
        public long Id { get; set; }

        public long? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityName { get; set; }

        public long? EntityId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User? User { get; set; }
    }
}
