using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class ChatMessage
    {
        public long Id { get; set; }

        public long ConversationId { get; set; }

        public long SenderId { get; set; }

        [Required]
        [MaxLength(30)]
        public string MessageType { get; set; } = "Text";

        [MaxLength(2000)]
        public string? Content { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(255)]
        public string? OriginalFileName { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ChatConversation Conversation { get; set; } = null!;

        public User Sender { get; set; } = null!;
    }
}
