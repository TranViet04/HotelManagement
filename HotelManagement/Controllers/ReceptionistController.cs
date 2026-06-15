using HotelManagement.Constants;
using HotelManagement.Services.Receptionist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Receptionist)]
    public class ReceptionistController : Controller
    {
        private readonly ReceptionistDashboardService _dashboardService;

        public ReceptionistController(ReceptionistDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = await _dashboardService.GetDashboardAsync();
            return View(model);
        }

        public IActionResult Bookings()
        {
            return View("Placeholder", "Quản lý đặt phòng");
        }

        public IActionResult CreateWalkInBooking()
        {
            return View("Placeholder", "Tạo đặt phòng trực tiếp");
        }

        public IActionResult TodayCheckIns()
        {
            return View("Placeholder", "Danh sách khách nhận phòng hôm nay");
        }

        public IActionResult TodayCheckOuts()
        {
            return View("Placeholder", "Danh sách khách trả phòng hôm nay");
        }

        public IActionResult Rooms()
        {
            return View("Placeholder", "Theo dõi phòng");
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
