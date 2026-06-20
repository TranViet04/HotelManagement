using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Chat;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Chat
{
    public class ChatService
    {
        private const long MaxImageFileSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

        private static readonly string[] AllowedImageContentTypes =
        [
            "image/jpeg",
            "image/jpg",
            "image/pjpeg",
            "image/png",
            "image/x-png",
            "image/webp",
            "application/octet-stream"
        ];

        private readonly HotelDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ChatService(HotelDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<CustomerChatViewModel> GetCustomerChatAsync(long customerId)
        {
            var conversation = await GetOrCreateCustomerConversationAsync(customerId);

            await MarkMessagesAsReadAsync(conversation.Id, customerId);

            var messages = await GetMessagesAsync(conversation.Id, customerId);
            var customer = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == customerId);

            return new CustomerChatViewModel
            {
                ConversationId = conversation.Id,
                CustomerName = customer?.FullName ?? "Customer",
                Messages = messages
            };
        }

        public async Task<ReceptionistChatViewModel> GetReceptionistChatAsync(long receptionistId, long? conversationId)
        {
            List<ChatMessageViewModel> messages = new();
            string? selectedCustomerName = null;

            if (conversationId.HasValue)
            {
                var canAccess = await CanUserAccessConversationAsync(
                    conversationId.Value,
                    receptionistId,
                    UserRoles.Receptionist);

                if (canAccess)
                {
                    await MarkMessagesAsReadAsync(conversationId.Value, receptionistId);
                    messages = await GetMessagesAsync(conversationId.Value, receptionistId);
                }
            }

            var conversations = await GetReceptionistConversationsAsync(receptionistId);

            if (conversationId.HasValue)
            {
                selectedCustomerName = conversations
                    .FirstOrDefault(c => c.ConversationId == conversationId.Value)
                    ?.CustomerName;
            }

            return new ReceptionistChatViewModel
            {
                SelectedConversationId = conversationId,
                SelectedCustomerName = selectedCustomerName,
                Conversations = conversations,
                Messages = messages
            };
        }

        public async Task<ChatConversation> GetOrCreateCustomerConversationAsync(long customerId)
        {
            var conversation = await _context.ChatConversations
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == customerId
                    && c.Status == ChatConversationStatuses.Open);

            if (conversation != null)
            {
                return conversation;
            }

            conversation = new ChatConversation
            {
                CustomerId = customerId,
                Status = ChatConversationStatuses.Open,
                CreatedAt = DateTime.Now
            };

            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            return conversation;
        }

        public async Task<bool> CanUserAccessConversationAsync(long conversationId, long userId, string role)
        {
            var conversation = await _context.ChatConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
            {
                return false;
            }

            if (role == UserRoles.Customer)
            {
                return conversation.CustomerId == userId;
            }

            return role == UserRoles.Receptionist;
        }

        public async Task<List<ChatConversationListItemViewModel>> GetReceptionistConversationsAsync(long receptionistId)
        {
            return await _context.ChatConversations
                .AsNoTracking()
                .Where(c => c.Status == ChatConversationStatuses.Open)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Select(c => new ChatConversationListItemViewModel
                {
                    ConversationId = c.Id,
                    CustomerName = c.Customer.FullName,
                    CustomerEmail = c.Customer.Email,
                    CustomerPhoneNumber = c.Customer.PhoneNumber,
                    Status = c.Status,
                    LastMessageAt = c.LastMessageAt,
                    LastMessagePreview = c.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.MessageType == ChatMessageTypes.Image
                            ? "[Image]"
                            : (m.Content ?? ""))
                        .FirstOrDefault() ?? "No messages yet",
                    UnreadCount = c.Messages.Count(m =>
                        !m.IsRead
                        && m.Sender.Role == UserRoles.Customer)
                })
                .ToListAsync();
        }

        public Task<int> GetCustomerUnreadCountAsync(long customerId)
        {
            return _context.ChatMessages
                .AsNoTracking()
                .CountAsync(m =>
                    m.Conversation.CustomerId == customerId
                    && m.Conversation.Status == ChatConversationStatuses.Open
                    && !m.IsRead
                    && m.Sender.Role == UserRoles.Receptionist);
        }

        public Task<int> GetReceptionistUnreadCountAsync()
        {
            return _context.ChatMessages
                .AsNoTracking()
                .CountAsync(m =>
                    m.Conversation.Status == ChatConversationStatuses.Open
                    && !m.IsRead
                    && m.Sender.Role == UserRoles.Customer);
        }

        public async Task<List<ChatMessageViewModel>> GetMessagesAsync(long conversationId, long currentUserId)
        {
            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageViewModel
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    SenderRole = m.Sender.Role,
                    MessageType = m.MessageType,
                    Content = m.Content,
                    ImageUrl = m.ImageUrl,
                    OriginalFileName = m.OriginalFileName,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsMine = message.SenderId == currentUserId;
            }

            return messages;
        }

        public async Task<ChatRealtimeMessageViewModel> SendTextMessageAsync(
            long conversationId,
            long senderId,
            string senderRole,
            string content)
        {
            content = string.IsNullOrWhiteSpace(content) ? string.Empty : content.Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Message content is required.");
            }

            if (content.Length > 2000)
            {
                throw new InvalidOperationException("Message content cannot exceed 2000 characters.");
            }

            var (conversation, sender) = await ValidateMessageContextAsync(
                conversationId,
                senderId,
                senderRole,
                "You cannot send messages in this conversation.");

            if (senderRole == UserRoles.Receptionist && conversation.AssignedReceptionistId == null)
            {
                conversation.AssignedReceptionistId = senderId;
            }

            var now = DateTime.Now;
            var message = new ChatMessage
            {
                ConversationId = conversationId,
                Conversation = conversation,
                SenderId = senderId,
                MessageType = ChatMessageTypes.Text,
                Content = content,
                IsRead = false,
                CreatedAt = now
            };

            _context.ChatMessages.Add(message);
            conversation.LastMessageAt = now;

            await _context.SaveChangesAsync();

            return MapRealtimeMessage(message, sender);
        }

        public async Task<ChatRealtimeMessageViewModel> SendImageMessageAsync(
            long conversationId,
            long senderId,
            string senderRole,
            IFormFile image)
        {
            var (conversation, sender) = await ValidateMessageContextAsync(
                conversationId,
                senderId,
                senderRole,
                "You cannot send images in this conversation.");

            if (image == null || image.Length == 0)
            {
                throw new InvalidOperationException("Please choose an image.");
            }

            if (image.Length > MaxImageFileSize)
            {
                throw new InvalidOperationException("Images cannot exceed 5MB.");
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!AllowedImageExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Only .jpg, .jpeg, .png, and .webp images are allowed.");
            }

            var contentType = image.ContentType?.ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(contentType)
                && !AllowedImageContentTypes.Contains(contentType))
            {
                throw new InvalidOperationException("Invalid image format.");
            }

            if (senderRole == UserRoles.Receptionist && conversation.AssignedReceptionistId == null)
            {
                conversation.AssignedReceptionistId = senderId;
            }

            var monthFolder = DateTime.Now.ToString("yyyyMM");
            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "chat",
                monthFolder);

            Directory.CreateDirectory(uploadFolder);

            var safeFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadFolder, safeFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            var now = DateTime.Now;
            var message = new ChatMessage
            {
                ConversationId = conversationId,
                Conversation = conversation,
                SenderId = senderId,
                MessageType = ChatMessageTypes.Image,
                ImageUrl = $"/uploads/chat/{monthFolder}/{safeFileName}",
                OriginalFileName = Path.GetFileName(image.FileName),
                IsRead = false,
                CreatedAt = now
            };

            _context.ChatMessages.Add(message);
            conversation.LastMessageAt = now;

            await _context.SaveChangesAsync();

            return MapRealtimeMessage(message, sender);
        }

        public async Task MarkMessagesAsReadAsync(long conversationId, long currentUserId)
        {
            var messages = await _context.ChatMessages
                .Where(m =>
                    m.ConversationId == conversationId
                    && m.SenderId != currentUserId
                    && !m.IsRead)
                .ToListAsync();

            if (!messages.Any())
            {
                return;
            }

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<(ChatConversation Conversation, User Sender)> ValidateMessageContextAsync(
            long conversationId,
            long senderId,
            string senderRole,
            string unauthorizedMessage)
        {
            var canAccess = await CanUserAccessConversationAsync(conversationId, senderId, senderRole);

            if (!canAccess)
            {
                throw new UnauthorizedAccessException(unauthorizedMessage);
            }

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderId);

            if (sender == null)
            {
                throw new InvalidOperationException("Sender was not found.");
            }

            var conversation = await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
            {
                throw new InvalidOperationException("Conversation was not found.");
            }

            return (conversation, sender);
        }

        private static ChatRealtimeMessageViewModel MapRealtimeMessage(ChatMessage message, User sender)
        {
            return new ChatRealtimeMessageViewModel
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                CustomerId = message.Conversation.CustomerId,
                SenderId = message.SenderId,
                SenderName = sender.FullName,
                SenderRole = sender.Role,
                MessageType = message.MessageType,
                Content = message.Content,
                ImageUrl = message.ImageUrl,
                OriginalFileName = message.OriginalFileName,
                CreatedAtText = message.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            };
        }
    }
}
