using System.Security.Claims;
using HotelManagement.Constants;
using HotelManagement.Services.Chat;
using HotelManagement.ViewModels.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HotelManagement.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public const string ReceptionistsGroupName = "receptionists";

        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        public static string GetConversationGroupName(long conversationId)
        {
            return $"conversation-{conversationId}";
        }

        public static string GetCustomerGroupName(long customerId)
        {
            return $"customer-{customerId}";
        }

        public override async Task OnConnectedAsync()
        {
            var role = Context.User?.FindFirstValue(ClaimTypes.Role);

            if (role == UserRoles.Receptionist)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ReceptionistsGroupName);
            }
            else if (role == UserRoles.Customer)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetCustomerGroupName(GetCurrentUserId()));
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinConversation(long conversationId)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var canAccess = await _chatService.CanUserAccessConversationAsync(conversationId, userId, role);

            if (!canAccess)
            {
                throw new HubException("You cannot access this conversation.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        }

        public async Task<ChatRealtimeMessageViewModel> SendTextMessage(long conversationId, string content)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var message = await _chatService.SendTextMessageAsync(conversationId, userId, role, content);

            await NotifyConversationChangedAsync(message);

            return message;
        }

        public async Task MarkConversationAsRead(long conversationId)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var canAccess = await _chatService.CanUserAccessConversationAsync(conversationId, userId, role);

            if (!canAccess)
            {
                throw new HubException("You cannot access this conversation.");
            }

            await _chatService.MarkMessagesAsReadAsync(conversationId, userId);

            if (role == UserRoles.Customer)
            {
                var unreadCount = await _chatService.GetCustomerUnreadCountAsync(userId);
                await Clients
                    .Group(GetCustomerGroupName(userId))
                    .SendAsync("CustomerUnreadCountChanged", unreadCount, conversationId);
            }
            else if (role == UserRoles.Receptionist)
            {
                await Clients
                    .Group(ReceptionistsGroupName)
                    .SendAsync("ConversationUpdated", conversationId);
            }
        }

        private async Task NotifyConversationChangedAsync(ChatRealtimeMessageViewModel message)
        {
            await Clients
                .Group(GetConversationGroupName(message.ConversationId))
                .SendAsync("ReceiveMessage", message);

            await Clients
                .Group(GetCustomerGroupName(message.CustomerId))
                .SendAsync("CustomerMessageReceived", message);

            await Clients
                .Group(ReceptionistsGroupName)
                .SendAsync("ConversationUpdated", message.ConversationId);

            var unreadCount = await _chatService.GetCustomerUnreadCountAsync(message.CustomerId);
            await Clients
                .Group(GetCustomerGroupName(message.CustomerId))
                .SendAsync("CustomerUnreadCountChanged", unreadCount, message.ConversationId);
        }

        private long GetCurrentUserId()
        {
            var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(userIdValue, out var userId))
            {
                throw new HubException("Cannot identify the current user.");
            }

            return userId;
        }

        private string GetCurrentUserRole()
        {
            var role = Context.User?.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new HubException("Cannot identify the current user role.");
            }

            return role;
        }
    }
}
