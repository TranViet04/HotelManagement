using System.Security.Claims;
using HotelManagement.Constants;
using HotelManagement.Services.Chat;
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

        public override async Task OnConnectedAsync()
        {
            var role = Context.User?.FindFirstValue(ClaimTypes.Role);

            if (role == UserRoles.Receptionist)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ReceptionistsGroupName);
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

        public async Task SendTextMessage(long conversationId, string content)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var message = await _chatService.SendTextMessageAsync(conversationId, userId, role, content);

            await Clients
                .Group(GetConversationGroupName(conversationId))
                .SendAsync("ReceiveMessage", message);

            await Clients
                .Group(ReceptionistsGroupName)
                .SendAsync("ConversationUpdated", conversationId);
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
