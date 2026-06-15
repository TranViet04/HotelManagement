using HotelManagement.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult RoomTypes()
        {
            return View("Placeholder", "Quản lý loại phòng");
        }

        public IActionResult Rooms()
        {
            return View("Placeholder", "Quản lý phòng");
        }

        public IActionResult Services()
        {
            return View("Placeholder", "Quản lý dịch vụ");
        }

        public IActionResult Employees()
        {
            return View("Placeholder", "Quản lý nhân viên");
        }

        public IActionResult Customers()
        {
            return View("Placeholder", "Quản lý khách hàng");
        }

        public IActionResult Bookings()
        {
            return View("Placeholder", "Theo dõi đặt phòng");
        }

        public IActionResult Invoices()
        {
            return View("Placeholder", "Theo dõi hóa đơn");
        }

        public IActionResult Reports()
        {
            return View("Placeholder", "Báo cáo doanh thu");
        }

        public IActionResult ActivityLogs()
        {
            return View("Placeholder", "Nhật ký hoạt động");
        }
    }
}
