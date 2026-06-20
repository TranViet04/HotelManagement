using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class ChatConversation
    {
        public long Id { get; set; }

        public long CustomerId { get; set; }

        public long? AssignedReceptionistId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Open";

        public DateTime? LastMessageAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ClosedAt { get; set; }

        public User Customer { get; set; } = null!;

        public User? AssignedReceptionist { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
