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
        public async Task<IActionResult> Index()
        {
            var customerId = GetCurrentUserId();
            var model = await _chatService.GetCustomerChatAsync(customerId);

            return View(model);
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
