using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Services.Admin;
using HotelManagement.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly AdminDashboardService _dashboardService;
        private readonly RoomTypeManagementService _roomTypeService;
        private readonly RoomManagementService _roomService;
        private readonly ServiceManagementService _serviceManagementService;
        private readonly EmployeeManagementService _employeeService;
        private readonly CustomerManagementService _customerService;
        private readonly BookingTrackingService _bookingTrackingService;
        private readonly InvoiceTrackingService _invoiceTrackingService;
        private readonly RevenueReportService _revenueReportService;
        private readonly ActivityLogService _activityLogService;
        private readonly HotelDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminController(
            AdminDashboardService dashboardService,
            RoomTypeManagementService roomTypeService,
            RoomManagementService roomService,
            ServiceManagementService serviceManagementService,
            EmployeeManagementService employeeService,
            CustomerManagementService customerService,
            BookingTrackingService bookingTrackingService,
            InvoiceTrackingService invoiceTrackingService,
            RevenueReportService revenueReportService,
            ActivityLogService activityLogService,
            HotelDbContext context,
            IWebHostEnvironment environment)
        {
            _dashboardService = dashboardService;
            _roomTypeService = roomTypeService;
            _roomService = roomService;
            _serviceManagementService = serviceManagementService;
            _employeeService = employeeService;
            _customerService = customerService;
            _bookingTrackingService = bookingTrackingService;
            _invoiceTrackingService = invoiceTrackingService;
            _revenueReportService = revenueReportService;
            _activityLogService = activityLogService;
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = await _dashboardService.GetDashboardAsync();
            return View(model);
        }

        public async Task<IActionResult> RoomTypes()
        {
            var model = (await _roomTypeService.GetAllAsync())
                .Where(rt => rt.Status != "Inactive")
                .ToList();
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateRoomType()
        {
            return View(new RoomTypeFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoomType(RoomTypeFormViewModel model, List<IFormFile>? images)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nameExists = await _roomTypeService.IsNameExistsAsync(model.Name);

            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.Name), "Tên loại phòng đã tồn tại");
                return View(model);
            }

            var roomTypeId = await _roomTypeService.CreateAsync(model);
            var imageUrls = await SaveRoomTypeImagesAsync(roomTypeId, images);

            if (imageUrls.Any())
            {
                await _roomTypeService.SetThumbnailUrlAsync(roomTypeId, imageUrls.First());
            }

            await AddActivityLogAsync(
                "CreateRoomType",
                "RoomType",
                roomTypeId,
                $"Tạo loại phòng {model.Name.Trim()}");

            TempData["SuccessMessage"] = "Thêm loại phòng thành công";
            return RedirectToAction(nameof(RoomTypes));
        }

        [HttpGet]
        public async Task<IActionResult> EditRoomType(long id)
        {
            var model = await _roomTypeService.GetForEditAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy loại phòng";
                return RedirectToAction(nameof(RoomTypes));
            }

            model.ExistingImageUrls = GetRoomTypeImageUrls(id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoomType(RoomTypeFormViewModel model, List<IFormFile>? images)
        {
            model.ExistingImageUrls = GetRoomTypeImageUrls(model.Id);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nameExists = await _roomTypeService.IsNameExistsAsync(model.Name, model.Id);

            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.Name), "Tên loại phòng đã tồn tại");
                return View(model);
            }

            var success = await _roomTypeService.UpdateAsync(model);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy loại phòng";
                return RedirectToAction(nameof(RoomTypes));
            }

            var imageUrls = await SaveRoomTypeImagesAsync(model.Id, images);

            if (imageUrls.Any())
            {
                await _roomTypeService.SetThumbnailUrlAsync(model.Id, imageUrls.First());
            }

            await AddActivityLogAsync(
                "UpdateRoomType",
                "RoomType",
                model.Id,
                $"Cập nhật loại phòng {model.Name.Trim()}");

            TempData["SuccessMessage"] = "Cập nhật loại phòng thành công";
            return RedirectToAction(nameof(RoomTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateRoomType(long id)
        {
            var success = await _roomTypeService.DeactivateAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy loại phòng";
                return RedirectToAction(nameof(RoomTypes));
            }

            await AddActivityLogAsync(
                "DeactivateRoomType",
                "RoomType",
                id,
                $"Ngưng sử dụng loại phòng #{id}");

            TempData["SuccessMessage"] = "Đã ngưng sử dụng loại phòng";
            return RedirectToAction(nameof(RoomTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoomType(long id)
        {
            var roomType = await _context.RoomTypes.FindAsync(id);

            if (roomType == null)
            {
                TempData["Error"] = "Không tìm thấy loại phòng.";
                return RedirectToAction(nameof(RoomTypes));
            }

            var hasActiveRooms = await _context.Rooms
                .AnyAsync(r => r.RoomTypeId == id && r.Status != RoomStatuses.Inactive);

            if (hasActiveRooms)
            {
                TempData["Error"] = "Không thể xóa loại phòng vì vẫn còn phòng đang sử dụng loại này.";
                return RedirectToAction(nameof(RoomTypes));
            }

            roomType.Status = "Inactive";
            roomType.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await AddActivityLogAsync(
                "DeleteRoomType",
                "RoomType",
                id,
                $"Xóa mềm loại phòng {roomType.Name}");

            TempData["Success"] = "Đã xóa loại phòng.";
            return RedirectToAction(nameof(RoomTypes));
        }

        public async Task<IActionResult> Rooms()
        {
            var model = (await _roomService.GetAllAsync())
                .Where(r => r.Status != RoomStatuses.Inactive)
                .ToList();
            return View(model);
        }

        public async Task<IActionResult> RoomDetails(long id)
        {
            var model = await _roomService.GetDetailAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng";
                return RedirectToAction(nameof(Rooms));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateRoom()
        {
            var model = await _roomService.GetCreateModelAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoom(RoomFormViewModel model, List<IFormFile>? images)
        {
            model.RoomTypeOptions = await _roomService.GetRoomTypeOptionsAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!await _roomService.RoomTypeExistsAsync(model.RoomTypeId))
            {
                ModelState.AddModelError(nameof(model.RoomTypeId), "Loại phòng không tồn tại");
                return View(model);
            }

            if (await _roomService.IsRoomNumberExistsAsync(model.RoomNumber))
            {
                ModelState.AddModelError(nameof(model.RoomNumber), "Số phòng đã tồn tại");
                return View(model);
            }

            var roomId = await _roomService.CreateAsync(model);

            var imageUrls = await SaveRoomImagesAsync(images);
            await _roomService.AddImagesAsync(roomId, imageUrls);
            await AddActivityLogAsync(
                "CreateRoom",
                "Room",
                roomId,
                $"Tạo phòng {model.RoomNumber.Trim()}");

            TempData["SuccessMessage"] = "Thêm phòng thành công";
            return RedirectToAction(nameof(Rooms));
        }

        [HttpGet]
        public async Task<IActionResult> EditRoom(long id)
        {
            var model = await _roomService.GetForEditAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng";
                return RedirectToAction(nameof(Rooms));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoom(RoomFormViewModel model, List<IFormFile>? images)
        {
            model.RoomTypeOptions = await _roomService.GetRoomTypeOptionsAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!await _roomService.RoomTypeExistsAsync(model.RoomTypeId))
            {
                ModelState.AddModelError(nameof(model.RoomTypeId), "Loại phòng không tồn tại");
                return View(model);
            }

            if (await _roomService.IsRoomNumberExistsAsync(model.RoomNumber, model.Id))
            {
                ModelState.AddModelError(nameof(model.RoomNumber), "Số phòng đã tồn tại");
                return View(model);
            }

            var success = await _roomService.UpdateAsync(model);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng";
                return RedirectToAction(nameof(Rooms));
            }

            var imageUrls = await SaveRoomImagesAsync(images);
            await _roomService.AddImagesAsync(model.Id, imageUrls);
            await AddActivityLogAsync(
                "UpdateRoom",
                "Room",
                model.Id,
                $"Cập nhật phòng {model.RoomNumber.Trim()}");

            TempData["SuccessMessage"] = "Cập nhật phòng thành công";
            return RedirectToAction(nameof(Rooms));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoomImage(long imageId, long roomId)
        {
            var success = await _roomService.DeleteImageAsync(imageId);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Đã xóa hình ảnh" : "Không tìm thấy hình ảnh";

            return RedirectToAction(nameof(EditRoom), new { id = roomId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMainRoomImage(long imageId, long roomId)
        {
            var success = await _roomService.SetMainImageAsync(imageId);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Đã đặt ảnh chính" : "Không tìm thấy hình ảnh";

            return RedirectToAction(nameof(EditRoom), new { id = roomId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoom(long id)
        {
            var room = await _context.Rooms.FindAsync(id);

            if (room == null)
            {
                TempData["Error"] = "Không tìm thấy phòng.";
                return RedirectToAction(nameof(Rooms));
            }

            var hasActiveBooking = await _context.Bookings.AnyAsync(b =>
                b.RoomId == id &&
                (b.Status == BookingStatuses.Pending ||
                 b.Status == BookingStatuses.Confirmed ||
                 b.Status == BookingStatuses.CheckedIn));

            if (hasActiveBooking)
            {
                TempData["Error"] = "Không thể xóa phòng vì phòng đang có đặt phòng chưa hoàn tất.";
                return RedirectToAction(nameof(Rooms));
            }

            room.Status = RoomStatuses.Inactive;
            room.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await AddActivityLogAsync(
                "DeleteRoom",
                "Room",
                id,
                $"Xóa mềm phòng {room.RoomNumber}");

            TempData["Success"] = "Đã xóa phòng.";
            return RedirectToAction(nameof(Rooms));
        }

        public async Task<IActionResult> Services()
        {
            var model = (await _serviceManagementService.GetAllAsync())
                .Where(s => s.Status != "Inactive")
                .ToList();
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateService()
        {
            return View(new ServiceFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(ServiceFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nameExists = await _serviceManagementService.IsNameExistsAsync(model.Name);

            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.Name), "Tên dịch vụ đã tồn tại");
                return View(model);
            }

            var serviceId = await _serviceManagementService.CreateAsync(model);
            await AddActivityLogAsync(
                "CreateService",
                "Service",
                serviceId,
                $"Tạo dịch vụ {model.Name.Trim()}");

            TempData["SuccessMessage"] = "Thêm dịch vụ thành công";
            return RedirectToAction(nameof(Services));
        }

        [HttpGet]
        public async Task<IActionResult> EditService(long id)
        {
            var model = await _serviceManagementService.GetForEditAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ";
                return RedirectToAction(nameof(Services));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(ServiceFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nameExists = await _serviceManagementService.IsNameExistsAsync(model.Name, model.Id);

            if (nameExists)
            {
                ModelState.AddModelError(nameof(model.Name), "Tên dịch vụ đã tồn tại");
                return View(model);
            }

            var success = await _serviceManagementService.UpdateAsync(model);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ";
                return RedirectToAction(nameof(Services));
            }

            await AddActivityLogAsync(
                "UpdateService",
                "Service",
                model.Id,
                $"Cập nhật dịch vụ {model.Name.Trim()}");

            TempData["SuccessMessage"] = "Cập nhật dịch vụ thành công";
            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateService(long id)
        {
            var success = await _serviceManagementService.DeactivateAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ";
                return RedirectToAction(nameof(Services));
            }

            await AddActivityLogAsync(
                "DeactivateService",
                "Service",
                id,
                $"Ngưng sử dụng dịch vụ #{id}");

            TempData["SuccessMessage"] = "Đã ngưng sử dụng dịch vụ";
            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(long id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                TempData["Error"] = "Không tìm thấy dịch vụ.";
                return RedirectToAction(nameof(Services));
            }

            service.Status = "Inactive";
            service.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await AddActivityLogAsync(
                "DeleteService",
                "Service",
                id,
                $"Xóa mềm dịch vụ {service.Name}");

            TempData["Success"] = "Đã xóa dịch vụ.";
            return RedirectToAction(nameof(Services));
        }

        public async Task<IActionResult> Employees()
        {
            var model = await _employeeService.GetReceptionistsAsync();
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateEmployee()
        {
            return View(new CreateEmployeeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailExists = await _employeeService.IsEmailExistsAsync(model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng");
                return View(model);
            }

            var userId = await _employeeService.CreateReceptionistAsync(model);
            await AddActivityLogAsync(
                "CreateUser",
                "User",
                userId,
                $"Tạo tài khoản nhân viên {model.FullName.Trim()}");

            TempData["SuccessMessage"] = "Tạo tài khoản lễ tân thành công";
            return RedirectToAction(nameof(Employees));
        }

        [HttpGet]
        public async Task<IActionResult> EditEmployee(long id)
        {
            var model = await _employeeService.GetForEditAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên lễ tân";
                return RedirectToAction(nameof(Employees));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(EditEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailExists = await _employeeService.IsEmailExistsAsync(model.Email, model.Id);

            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng");
                return View(model);
            }

            var success = await _employeeService.UpdateReceptionistAsync(model);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên lễ tân";
                return RedirectToAction(nameof(Employees));
            }

            await AddActivityLogAsync(
                "UpdateUser",
                "User",
                model.Id,
                $"Cập nhật nhân viên {model.FullName.Trim()}");

            TempData["SuccessMessage"] = "Cập nhật nhân viên lễ tân thành công";
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockEmployee(long id)
        {
            var success = await _employeeService.LockAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên lễ tân";
                return RedirectToAction(nameof(Employees));
            }

            await AddActivityLogAsync(
                "LockUser",
                "User",
                id,
                $"Khóa tài khoản nhân viên #{id}");

            TempData["SuccessMessage"] = "Đã khóa tài khoản lễ tân";
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockEmployee(long id)
        {
            var success = await _employeeService.UnlockAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên lễ tân";
                return RedirectToAction(nameof(Employees));
            }

            await AddActivityLogAsync(
                "UnlockUser",
                "User",
                id,
                $"Mở khóa tài khoản nhân viên #{id}");

            TempData["SuccessMessage"] = "Đã mở khóa tài khoản lễ tân";
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateEmployee(long id)
        {
            var success = await _employeeService.DeactivateAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên lễ tân";
                return RedirectToAction(nameof(Employees));
            }

            await AddActivityLogAsync(
                "DeactivateUser",
                "User",
                id,
                $"Ngưng sử dụng tài khoản nhân viên #{id}");

            TempData["SuccessMessage"] = "Đã ngưng sử dụng tài khoản lễ tân";
            return RedirectToAction(nameof(Employees));
        }

        public async Task<IActionResult> Customers()
        {
            var model = await _customerService.GetCustomersAsync();
            return View(model);
        }

        public async Task<IActionResult> CustomerDetails(long id)
        {
            var model = await _customerService.GetDetailAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng";
                return RedirectToAction(nameof(Customers));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditCustomer(long id)
        {
            var model = await _customerService.GetForEditAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng";
                return RedirectToAction(nameof(Customers));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(EditCustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailExists = await _customerService.IsEmailExistsAsync(model.Email, model.Id);

            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng");
                return View(model);
            }

            var success = await _customerService.UpdateCustomerAsync(model);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng";
                return RedirectToAction(nameof(Customers));
            }

            await AddActivityLogAsync(
                "UpdateUser",
                "User",
                model.Id,
                $"Cập nhật khách hàng {model.FullName.Trim()}");

            TempData["SuccessMessage"] = "Cập nhật khách hàng thành công";
            return RedirectToAction(nameof(Customers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockCustomer(long id)
        {
            var success = await _customerService.LockAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng";
                return RedirectToAction(nameof(Customers));
            }

            await AddActivityLogAsync(
                "LockUser",
                "User",
                id,
                $"Khóa tài khoản khách hàng #{id}");

            TempData["SuccessMessage"] = "Đã khóa tài khoản khách hàng";
            return RedirectToAction(nameof(Customers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockCustomer(long id)
        {
            var success = await _customerService.UnlockAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng";
                return RedirectToAction(nameof(Customers));
            }

            await AddActivityLogAsync(
                "UnlockUser",
                "User",
                id,
                $"Mở khóa tài khoản khách hàng #{id}");

            TempData["SuccessMessage"] = "Đã mở khóa tài khoản khách hàng";
            return RedirectToAction(nameof(Customers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateCustomer(long id)
        {
            var success = await _customerService.DeactivateAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng";
                return RedirectToAction(nameof(Customers));
            }

            await AddActivityLogAsync(
                "DeactivateUser",
                "User",
                id,
                $"Ngưng sử dụng tài khoản khách hàng #{id}");

            TempData["SuccessMessage"] = "Đã ngưng sử dụng tài khoản khách hàng";
            return RedirectToAction(nameof(Customers));
        }

        public async Task<IActionResult> Bookings(
            string? keyword,
            string? status,
            DateTime? checkInDate)
        {
            var bookings = await _bookingTrackingService.GetBookingsAsync(keyword, status, checkInDate);

            ViewBag.Keyword = keyword?.Trim();
            ViewBag.Status = status;
            ViewBag.CheckInDate = checkInDate?.ToString("yyyy-MM-dd");

            return View(bookings);
        }

        public async Task<IActionResult> BookingDetails(long id)
        {
            var model = await _bookingTrackingService.GetDetailAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đặt phòng";
                return RedirectToAction(nameof(Bookings));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBooking(long id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Invoice)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy đặt phòng.";
                return RedirectToAction(nameof(Bookings));
            }

            if (booking.Status == BookingStatuses.CheckedIn || booking.Status == BookingStatuses.CheckedOut)
            {
                TempData["Error"] = "Không thể xóa đặt phòng đã check-in hoặc check-out.";
                return RedirectToAction(nameof(Bookings));
            }

            if (booking.Invoice != null && booking.Invoice.Status == InvoiceStatuses.Paid)
            {
                TempData["Error"] = "Không thể xóa đặt phòng đã có hóa đơn thanh toán.";
                return RedirectToAction(nameof(Bookings));
            }

            booking.Status = BookingStatuses.Cancelled;
            booking.CancelledAt = DateTime.Now;
            booking.CancelReason = "Admin hủy đặt phòng.";
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await AddActivityLogAsync(
                "DeleteBooking",
                "Booking",
                id,
                $"Hủy đặt phòng {booking.BookingCode}");

            TempData["Success"] = "Đã hủy đặt phòng.";
            return RedirectToAction(nameof(Bookings));
        }

        public async Task<IActionResult> Invoices(
            string? keyword,
            string? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var invoices = await _invoiceTrackingService.GetInvoicesAsync(keyword, status, fromDate, toDate);

            ViewBag.Keyword = keyword?.Trim();
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(invoices);
        }

        public async Task<IActionResult> InvoiceDetails(long id)
        {
            var model = await _invoiceTrackingService.GetDetailAsync(id);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn";
                return RedirectToAction(nameof(Invoices));
            }

            return View(model);
        }

        public async Task<IActionResult> RevenueReport(DateTime? fromDate, DateTime? toDate)
        {
            var model = await _revenueReportService.GetReportAsync(fromDate, toDate);
            return View(model);
        }

        public IActionResult Reports()
        {
            return View("Placeholder", "Báo cáo doanh thu");
        }

        public async Task<IActionResult> ActivityLogs(
            string? keyword,
            string? action,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var logs = await _activityLogService.GetLogsAsync(keyword, action, fromDate, toDate);

            ViewBag.Keyword = keyword?.Trim();
            ViewBag.Action = action;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(logs);
        }

        private async Task AddActivityLogAsync(
            string action,
            string entityName,
            long? entityId,
            string description)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            long? userId = null;

            if (long.TryParse(userIdString, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            await _activityLogService.AddAsync(userId, action, entityName, entityId, description);
        }

        private async Task<List<string>> SaveRoomTypeImagesAsync(long roomTypeId, List<IFormFile>? images)
        {
            var imageUrls = new List<string>();

            if (images == null || !images.Any())
            {
                return imageUrls;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var uploadFolder = Path.Combine(_environment.WebRootPath, "images", "room-types", roomTypeId.ToString());

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            foreach (var image in images)
            {
                if (image.Length <= 0)
                {
                    continue;
                }

                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    continue;
                }

                if (image.Length > 5 * 1024 * 1024)
                {
                    continue;
                }

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadFolder, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);

                imageUrls.Add($"/images/room-types/{roomTypeId}/{fileName}");
            }

            return imageUrls;
        }

        private List<string> GetRoomTypeImageUrls(long roomTypeId)
        {
            var uploadFolder = Path.Combine(_environment.WebRootPath, "images", "room-types", roomTypeId.ToString());

            if (!Directory.Exists(uploadFolder))
            {
                return new List<string>();
            }

            return Directory
                .GetFiles(uploadFolder)
                .Where(file =>
                {
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    return extension is ".jpg" or ".jpeg" or ".png" or ".webp";
                })
                .OrderBy(file => file)
                .Select(file => $"/images/room-types/{roomTypeId}/{Path.GetFileName(file)}")
                .ToList();
        }

        private async Task<List<string>> SaveRoomImagesAsync(List<IFormFile>? images)
        {
            var imageUrls = new List<string>();

            if (images == null || !images.Any())
            {
                return imageUrls;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var uploadFolder = Path.Combine(_environment.WebRootPath, "images", "rooms");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            foreach (var image in images)
            {
                if (image.Length <= 0)
                {
                    continue;
                }

                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    continue;
                }

                if (image.Length > 5 * 1024 * 1024)
                {
                    continue;
                }

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadFolder, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);

                imageUrls.Add($"/images/rooms/{fileName}");
            }

            return imageUrls;
        }
    }
}
