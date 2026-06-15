using HotelManagement.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Customer)]
    public class CustomerController : Controller
    {
        public IActionResult Profile()
        {
            return View();
        }
    }
}
