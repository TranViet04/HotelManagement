using HotelManagement.Constants;
using HotelManagement.Services.Admin;
using HotelManagement.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly AdminDashboardService _dashboardService;
        private readonly RoomTypeManagementService _roomTypeService;
        private readonly RoomManagementService _roomService;
        private readonly IWebHostEnvironment _environment;

        public AdminController(
            AdminDashboardService dashboardService,
            RoomTypeManagementService roomTypeService,
            RoomManagementService roomService,
            IWebHostEnvironment environment)
        {
            _dashboardService = dashboardService;
            _roomTypeService = roomTypeService;
            _roomService = roomService;
            _environment = environment;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = await _dashboardService.GetDashboardAsync();
            return View(model);
        }

        public async Task<IActionResult> RoomTypes()
        {
            var model = await _roomTypeService.GetAllAsync();
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateRoomType()
        {
            return View(new RoomTypeFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoomType(RoomTypeFormViewModel model)
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

            await _roomTypeService.CreateAsync(model);

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

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoomType(RoomTypeFormViewModel model)
        {
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

            TempData["SuccessMessage"] = "Đã ngưng sử dụng loại phòng";
            return RedirectToAction(nameof(RoomTypes));
        }

        public async Task<IActionResult> Rooms()
        {
            var model = await _roomService.GetAllAsync();
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
