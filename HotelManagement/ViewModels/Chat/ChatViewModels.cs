namespace HotelManagement.ViewModels.Chat
{
    public class ChatMessageViewModel
    {
        public long Id { get; set; }

        public long ConversationId { get; set; }

        public long SenderId { get; set; }

        public string SenderName { get; set; } = string.Empty;

        public string SenderRole { get; set; } = string.Empty;

        public string MessageType { get; set; } = string.Empty;

        public string? Content { get; set; }

        public string? ImageUrl { get; set; }

        public string? OriginalFileName { get; set; }

        public bool IsMine { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class ChatConversationListItemViewModel
    {
        public long ConversationId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerEmail { get; set; }

        public string? CustomerPhoneNumber { get; set; }

        public string Status { get; set; } = string.Empty;

        public string LastMessagePreview { get; set; } = string.Empty;

        public DateTime? LastMessageAt { get; set; }

        public int UnreadCount { get; set; }
    }

    public class CustomerChatViewModel
    {
        public long ConversationId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public List<ChatMessageViewModel> Messages { get; set; } = new();
    }

    public class ReceptionistChatViewModel
    {
        public long? SelectedConversationId { get; set; }

        public string? SelectedCustomerName { get; set; }

        public List<ChatConversationListItemViewModel> Conversations { get; set; } = new();

        public List<ChatMessageViewModel> Messages { get; set; } = new();
    }

    public class ChatRealtimeMessageViewModel
    {
        public long Id { get; set; }

        public long ConversationId { get; set; }

        public long CustomerId { get; set; }

        public long SenderId { get; set; }

        public string SenderName { get; set; } = string.Empty;

        public string SenderRole { get; set; } = string.Empty;

        public string MessageType { get; set; } = string.Empty;

        public string? Content { get; set; }

        public string? ImageUrl { get; set; }

        public string? OriginalFileName { get; set; }

        public string CreatedAtText { get; set; } = string.Empty;
    }
}
