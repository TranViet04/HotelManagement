using System.Security.Claims;
using HotelManagement.Hubs;
using HotelManagement.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HotelManagement.Controllers
{
    [Authorize]
    [Route("Chat")]
    public class ChatUploadController : Controller
    {
        private readonly ChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatUploadController> _logger;

        public ChatUploadController(
            ChatService chatService,
            IHubContext<ChatHub> hubContext,
            ILogger<ChatUploadController> logger)
        {
            _chatService = chatService;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("UploadImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(long conversationId, IFormFile image)
        {
            try
            {
                var userId = GetCurrentUserId();
                var role = GetCurrentUserRole();
                var message = await _chatService.SendImageMessageAsync(
                    conversationId,
                    userId,
                    role,
                    image);

                try
                {
                    await _hubContext.Clients
                        .Group(ChatHub.GetConversationGroupName(conversationId))
                        .SendAsync("ReceiveMessage", message);

                    await _hubContext.Clients
                        .Group(ChatHub.GetCustomerGroupName(message.CustomerId))
                        .SendAsync("CustomerMessageReceived", message);

                    await _hubContext.Clients
                        .Group(ChatHub.ReceptionistsGroupName)
                        .SendAsync("ConversationUpdated", conversationId);

                    var unreadCount = await _chatService.GetCustomerUnreadCountAsync(message.CustomerId);
                    await _hubContext.Clients
                        .Group(ChatHub.GetCustomerGroupName(message.CustomerId))
                        .SendAsync("CustomerUnreadCountChanged", unreadCount, conversationId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Image message {MessageId} was saved, but realtime notification failed.",
                        message.Id);
                }

                return Json(new
                {
                    success = true,
                    message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        private long GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(userIdValue, out var userId))
            {
                throw new InvalidOperationException("Cannot identify the current user.");
            }

            return userId;
        }

        private string GetCurrentUserRole()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new InvalidOperationException("Cannot identify the current user role.");
            }

            return role;
        }
    }
}
