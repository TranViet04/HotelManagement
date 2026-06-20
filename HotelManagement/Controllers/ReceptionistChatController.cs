using System.Security.Claims;
using HotelManagement.Constants;
using HotelManagement.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Receptionist)]
    [Route("Receptionist/Chats")]
    public class ReceptionistChatController : Controller
    {
        private readonly ChatService _chatService;

        public ReceptionistChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(long? conversationId)
        {
            var receptionistId = GetCurrentUserId();
            var model = await _chatService.GetReceptionistChatAsync(receptionistId, conversationId);

            return View(model);
        }

        [HttpGet("Conversations")]
        public async Task<IActionResult> Conversations()
        {
            var receptionistId = GetCurrentUserId();
            var conversations = await _chatService.GetReceptionistConversationsAsync(receptionistId);

            return Json(conversations);
        }

        [HttpGet("UnreadSummary")]
        public async Task<IActionResult> UnreadSummary()
        {
            var unreadCount = await _chatService.GetReceptionistUnreadCountAsync();
            return Json(new { unreadCount });
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
    }
}
