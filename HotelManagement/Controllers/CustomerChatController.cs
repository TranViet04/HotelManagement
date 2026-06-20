using System.Security.Claims;
using HotelManagement.Constants;
using HotelManagement.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Customer)]
    [Route("Customer/Chat")]
    public class CustomerChatController : Controller
    {
        private readonly ChatService _chatService;

        public CustomerChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home", new { openChat = true });
        }

        [HttpGet("Messages")]
        public async Task<IActionResult> Messages()
        {
            var customerId = GetCurrentUserId();
            var model = await _chatService.GetCustomerChatAsync(customerId);

            return Json(new
            {
                model.ConversationId,
                Messages = model.Messages.Select(message => new
                {
                    message.Id,
                    message.ConversationId,
                    message.SenderId,
                    message.SenderName,
                    message.SenderRole,
                    message.MessageType,
                    message.Content,
                    message.ImageUrl,
                    message.OriginalFileName,
                    message.IsMine,
                    CreatedAtText = message.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
            });
        }

        [HttpGet("Summary")]
        public async Task<IActionResult> Summary()
        {
            var customerId = GetCurrentUserId();
            var unreadCount = await _chatService.GetCustomerUnreadCountAsync(customerId);

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
