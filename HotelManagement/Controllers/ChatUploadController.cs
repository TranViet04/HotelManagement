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

        public ChatUploadController(
            ChatService chatService,
            IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
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

                await _hubContext.Clients
                    .Group(ChatHub.GetConversationGroupName(conversationId))
                    .SendAsync("ReceiveMessage", message);

                await _hubContext.Clients
                    .Group(ChatHub.ReceptionistsGroupName)
                    .SendAsync("ConversationUpdated", conversationId);

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
