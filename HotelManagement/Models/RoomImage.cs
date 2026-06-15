using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class RoomImage
    {
        public long Id { get; set; }

        public long RoomId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Caption { get; set; }

        public bool IsMain { get; set; } = false;

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Room? Room { get; set; }
    }
}
