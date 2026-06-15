using HotelManagement.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Customer)]
    public class BookingsController : Controller
    {
        public IActionResult MyBookings()
        {
            return View("Placeholder", "Lịch sử đặt phòng");
        }

        public IActionResult Create()
        {
            return View("Placeholder", "Tạo đặt phòng");
        }
    }
}
