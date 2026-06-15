using HotelManagement.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Receptionist)]
    public class ReceptionistController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Bookings()
        {
            return View("Placeholder", "Danh sách đặt phòng");
        }

        public IActionResult CreateWalkInBooking()
        {
            return View("Placeholder", "Tạo đặt phòng tại quầy");
        }

        public IActionResult TodayCheckIns()
        {
            return View("Placeholder", "Khách nhận phòng hôm nay");
        }

        public IActionResult TodayCheckOuts()
        {
            return View("Placeholder", "Khách trả phòng hôm nay");
        }

        public IActionResult Rooms()
        {
            return View("Placeholder", "Tình trạng phòng");
        }

        public IActionResult BookingServices()
        {
            return View("Placeholder", "Dịch vụ phát sinh");
        }

        public IActionResult Invoices()
        {
            return View("Placeholder", "Hóa đơn");
        }

        public IActionResult Payments()
        {
            return View("Placeholder", "Thanh toán");
        }
    }
}
