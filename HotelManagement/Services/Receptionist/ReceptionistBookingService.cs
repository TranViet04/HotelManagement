using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Receptionist;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Receptionist
{
    public class ReceptionistBookingService
    {
        private readonly HotelDbContext _context;

        public ReceptionistBookingService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<ReceptionistBookingListViewModel> GetBookingsAsync(
            string? keyword,
            string? status,
            DateTime? checkInFrom,
            DateTime? checkInTo)
        {
            keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
            status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();

            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(b =>
                    b.BookingCode.Contains(keyword)
                    || (b.Customer != null && b.Customer.FullName.Contains(keyword))
                    || (b.Customer != null && b.Customer.Email.Contains(keyword))
                    || (b.Customer != null && b.Customer.PhoneNumber != null && b.Customer.PhoneNumber.Contains(keyword))
                    || (b.Room != null && b.Room.RoomNumber.Contains(keyword))
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (checkInFrom.HasValue)
            {
                var from = checkInFrom.Value.Date;
                query = query.Where(b => b.CheckInDate >= from);
            }

            if (checkInTo.HasValue)
            {
                var to = checkInTo.Value.Date.AddDays(1);
                query = query.Where(b => b.CheckInDate < to);
            }

            var bookingRows = await query
                .OrderByDescending(b => b.CreatedAt)
                .ThenByDescending(b => b.Id)
                .Select(b => new
                {
                    BookingId = b.Id,
                    BookingCode = b.BookingCode,
                    CustomerName = b.Customer != null ? b.Customer.FullName : "Không xác định",
                    CustomerPhoneNumber = b.Customer != null ? b.Customer.PhoneNumber : null,
                    CustomerEmail = b.Customer != null ? b.Customer.Email : null,
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : "Không xác định",
                    RoomTypeName = b.Room != null && b.Room.RoomType != null
                        ? b.Room.RoomType.Name
                        : "Không xác định",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            var bookings = bookingRows
                .Select(b => new ReceptionistBookingListItemViewModel
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    CustomerName = b.CustomerName,
                    CustomerPhoneNumber = b.CustomerPhoneNumber,
                    CustomerEmail = b.CustomerEmail,
                    RoomNumber = b.RoomNumber,
                    RoomTypeName = b.RoomTypeName,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Nights = Math.Max(0, (b.CheckOutDate.Date - b.CheckInDate.Date).Days),
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToList();

            return new ReceptionistBookingListViewModel
            {
                Keyword = keyword,
                Status = status,
                CheckInFrom = checkInFrom,
                CheckInTo = checkInTo,
                StatusOptions = GetStatusOptions(status),
                Bookings = bookings
            };
        }

        public async Task<ReceptionistBookingDetailViewModel?> GetBookingDetailAsync(long bookingId)
        {
            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.CreatedByUser)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return null;
            }

            var canConfirm = booking.Status == BookingStatuses.Pending;
            var checkInAvailability = GetCheckInAvailability(booking);
            var addServiceAvailability = GetAddServiceAvailability(booking);
            var checkOutAvailability = GetCheckOutAvailability(booking);

            return new ReceptionistBookingDetailViewModel
            {
                BookingId = booking.Id,
                BookingCode = booking.BookingCode,
                CustomerName = booking.Customer?.FullName ?? "Không xác định",
                CustomerEmail = booking.Customer?.Email,
                CustomerPhoneNumber = booking.Customer?.PhoneNumber,
                CustomerIdentityNumber = booking.Customer?.IdentityNumber,
                CustomerAddress = booking.Customer?.Address,
                RoomId = booking.RoomId,
                RoomNumber = booking.Room?.RoomNumber ?? "Không xác định",
                Floor = booking.Room?.Floor,
                RoomTypeName = booking.Room?.RoomType?.Name ?? "Không xác định",
                PricePerNight = booking.Room?.RoomType?.Price ?? 0,
                Capacity = booking.Room?.RoomType?.Capacity ?? 0,
                BedType = booking.Room?.RoomType?.BedType,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Nights = Math.Max(0, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days),
                Adults = booking.Adults,
                Children = booking.Children,
                Status = booking.Status,
                TotalRoomAmount = booking.TotalRoomAmount,
                TotalServiceAmount = booking.TotalServiceAmount,
                TotalAmount = booking.TotalAmount,
                SpecialRequest = booking.SpecialRequest,
                CancelReason = booking.CancelReason,
                ConfirmedAt = booking.ConfirmedAt,
                CheckedInAt = booking.CheckedInAt,
                CheckedOutAt = booking.CheckedOutAt,
                CancelledAt = booking.CancelledAt,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                CanConfirm = canConfirm,
                ConfirmBlockReason = canConfirm
                    ? null
                    : "Chỉ có thể xác nhận booking ở trạng thái Chờ xác nhận.",
                CanCheckIn = checkInAvailability.CanCheckIn,
                CheckInBlockReason = checkInAvailability.Reason,
                CanAddService = addServiceAvailability.CanAddService,
                AddServiceBlockReason = addServiceAvailability.Reason,
                CanCheckOut = checkOutAvailability.CanCheckOut,
                CheckOutBlockReason = checkOutAvailability.Reason,
                Services = booking.BookingServices
                    .OrderByDescending(bs => bs.UsedAt)
                    .Select(bs => new ReceptionistBookingServiceItemViewModel
                    {
                        BookingServiceId = bs.Id,
                        ServiceName = bs.Service?.Name ?? "Dịch vụ không xác định",
                        Category = bs.Service?.Category,
                        Unit = bs.Service?.Unit,
                        Quantity = bs.Quantity,
                        UnitPrice = bs.UnitPrice,
                        TotalPrice = bs.TotalPrice,
                        UsedAt = bs.UsedAt,
                        Note = bs.Note,
                        CreatedByName = bs.CreatedByUser?.FullName
                    })
                    .ToList()
            };
        }

        public async Task<List<ReceptionistBookingListItemViewModel>> GetCheckedInBookingsForServiceAsync()
        {
            var bookingRows = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Where(b => b.Status == BookingStatuses.CheckedIn)
                .OrderByDescending(b => b.CheckedInAt)
                .ThenByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    BookingId = b.Id,
                    BookingCode = b.BookingCode,
                    CustomerName = b.Customer != null ? b.Customer.FullName : "Không xác định",
                    CustomerPhoneNumber = b.Customer != null ? b.Customer.PhoneNumber : null,
                    CustomerEmail = b.Customer != null ? b.Customer.Email : null,
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : "Không xác định",
                    RoomTypeName = b.Room != null && b.Room.RoomType != null
                        ? b.Room.RoomType.Name
                        : "Không xác định",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return bookingRows
                .Select(b => new ReceptionistBookingListItemViewModel
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    CustomerName = b.CustomerName,
                    CustomerPhoneNumber = b.CustomerPhoneNumber,
                    CustomerEmail = b.CustomerEmail,
                    RoomNumber = b.RoomNumber,
                    RoomTypeName = b.RoomTypeName,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Nights = Math.Max(0, (b.CheckOutDate.Date - b.CheckInDate.Date).Days),
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToList();
        }

        public async Task<List<ReceptionistBookingListItemViewModel>> GetTodayCheckOutsAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var bookingRows = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Where(b =>
                    b.Status == BookingStatuses.CheckedIn
                    && b.CheckOutDate >= today
                    && b.CheckOutDate < tomorrow
                )
                .OrderBy(b => b.CheckOutDate)
                .ThenBy(b => b.CreatedAt)
                .Select(b => new
                {
                    BookingId = b.Id,
                    BookingCode = b.BookingCode,
                    CustomerName = b.Customer != null ? b.Customer.FullName : "Không xác định",
                    CustomerPhoneNumber = b.Customer != null ? b.Customer.PhoneNumber : null,
                    CustomerEmail = b.Customer != null ? b.Customer.Email : null,
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : "Không xác định",
                    RoomTypeName = b.Room != null && b.Room.RoomType != null
                        ? b.Room.RoomType.Name
                        : "Không xác định",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return bookingRows
                .Select(b => new ReceptionistBookingListItemViewModel
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    CustomerName = b.CustomerName,
                    CustomerPhoneNumber = b.CustomerPhoneNumber,
                    CustomerEmail = b.CustomerEmail,
                    RoomNumber = b.RoomNumber,
                    RoomTypeName = b.RoomTypeName,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Nights = Math.Max(0, (b.CheckOutDate.Date - b.CheckInDate.Date).Days),
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToList();
        }

        public async Task<AddBookingServiceViewModel?> PrepareAddServiceAsync(long bookingId)
        {
            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.CreatedByUser)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return null;
            }

            var addServiceAvailability = GetAddServiceAvailability(booking);

            var serviceOptions = await _context.Services
                .AsNoTracking()
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name + " - " + s.Price.ToString("N0") + " VND" + (s.Unit != null ? " / " + s.Unit : "")
                })
                .ToListAsync();

            serviceOptions.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Chọn dịch vụ --"
            });

            return new AddBookingServiceViewModel
            {
                BookingId = booking.Id,
                BookingCode = booking.BookingCode,
                CustomerName = booking.Customer?.FullName ?? "Không xác định",
                RoomNumber = booking.Room?.RoomNumber ?? "Không xác định",
                RoomTypeName = booking.Room?.RoomType?.Name ?? "Không xác định",
                BookingStatus = booking.Status,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                TotalRoomAmount = booking.TotalRoomAmount,
                TotalServiceAmount = booking.TotalServiceAmount,
                TotalAmount = booking.TotalAmount,
                Quantity = 1,
                CanAddService = addServiceAvailability.CanAddService,
                AddServiceBlockReason = addServiceAvailability.Reason,
                ServiceOptions = serviceOptions,
                ExistingServices = booking.BookingServices
                    .OrderByDescending(bs => bs.UsedAt)
                    .Select(bs => new ReceptionistBookingServiceItemViewModel
                    {
                        BookingServiceId = bs.Id,
                        ServiceName = bs.Service?.Name ?? "Dịch vụ không xác định",
                        Category = bs.Service?.Category,
                        Unit = bs.Service?.Unit,
                        Quantity = bs.Quantity,
                        UnitPrice = bs.UnitPrice,
                        TotalPrice = bs.TotalPrice,
                        UsedAt = bs.UsedAt,
                        Note = bs.Note,
                        CreatedByName = bs.CreatedByUser?.FullName
                    })
                    .ToList()
            };
        }

        public async Task<List<ReceptionistBookingListItemViewModel>> GetTodayCheckInsAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var bookingRows = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Where(b =>
                    b.Status == BookingStatuses.Confirmed
                    && b.CheckInDate >= today
                    && b.CheckInDate < tomorrow
                )
                .OrderBy(b => b.CheckInDate)
                .ThenBy(b => b.CreatedAt)
                .Select(b => new
                {
                    BookingId = b.Id,
                    BookingCode = b.BookingCode,
                    CustomerName = b.Customer != null ? b.Customer.FullName : "Không xác định",
                    CustomerPhoneNumber = b.Customer != null ? b.Customer.PhoneNumber : null,
                    CustomerEmail = b.Customer != null ? b.Customer.Email : null,
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : "Không xác định",
                    RoomTypeName = b.Room != null && b.Room.RoomType != null
                        ? b.Room.RoomType.Name
                        : "Không xác định",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return bookingRows
                .Select(b => new ReceptionistBookingListItemViewModel
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    CustomerName = b.CustomerName,
                    CustomerPhoneNumber = b.CustomerPhoneNumber,
                    CustomerEmail = b.CustomerEmail,
                    RoomNumber = b.RoomNumber,
                    RoomTypeName = b.RoomTypeName,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Nights = Math.Max(0, (b.CheckOutDate.Date - b.CheckInDate.Date).Days),
                    Adults = b.Adults,
                    Children = b.Children,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToList();
        }

        public async Task<ReceptionistBookingResult> ConfirmBookingAsync(long bookingId, long receptionistId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return ReceptionistBookingResult.Failure("Không tìm thấy booking.");
            }

            if (booking.Status != BookingStatuses.Pending)
            {
                return ReceptionistBookingResult.Failure("Chỉ có thể xác nhận booking ở trạng thái Chờ xác nhận.");
            }

            if (booking.Room == null || booking.Room.RoomType == null)
            {
                return ReceptionistBookingResult.Failure("Booking không có thông tin phòng hợp lệ.");
            }

            if (booking.Room.Status == RoomStatuses.Maintenance || booking.Room.Status == RoomStatuses.Inactive)
            {
                return ReceptionistBookingResult.Failure("Phòng đang bảo trì hoặc ngưng sử dụng, không thể xác nhận booking.");
            }

            if (booking.Room.RoomType.Status != "Active")
            {
                return ReceptionistBookingResult.Failure("Loại phòng không còn hoạt động.");
            }

            var hasOverlappingBooking = await HasOverlappingActiveBookingAsync(
                booking.RoomId,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.Id
            );

            if (hasOverlappingBooking)
            {
                return ReceptionistBookingResult.Failure("Phòng đã có booking khác trùng thời gian, không thể xác nhận.");
            }

            var now = DateTime.Now;
            booking.Status = BookingStatuses.Confirmed;
            booking.ConfirmedAt = now;
            booking.UpdatedAt = now;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = receptionistId,
                Action = "ConfirmBooking",
                EntityName = "Booking",
                EntityId = booking.Id,
                Description = $"Receptionist xác nhận booking {booking.BookingCode}",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return ReceptionistBookingResult.Success(
                booking.Id,
                booking.BookingCode,
                $"Đã xác nhận booking {booking.BookingCode}."
            );
        }

        public async Task<ReceptionistBookingResult> CheckInBookingAsync(long bookingId, long receptionistId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return ReceptionistBookingResult.Failure("Không tìm thấy booking.");
            }

            var checkInAvailability = GetCheckInAvailability(booking);

            if (!checkInAvailability.CanCheckIn)
            {
                return ReceptionistBookingResult.Failure(checkInAvailability.Reason ?? "Booking không thể check-in.");
            }

            if (booking.Room == null || booking.Room.RoomType == null)
            {
                return ReceptionistBookingResult.Failure("Booking không có thông tin phòng hợp lệ.");
            }

            if (booking.Room.Status != RoomStatuses.Available)
            {
                return ReceptionistBookingResult.Failure("Phòng hiện không ở trạng thái trống, không thể check-in.");
            }

            if (booking.Room.RoomType.Status != "Active")
            {
                return ReceptionistBookingResult.Failure("Loại phòng không còn hoạt động.");
            }

            var hasOverlappingCheckedInOrConfirmed = await HasOverlappingCheckedInOrConfirmedBookingAsync(
                booking.RoomId,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.Id
            );

            if (hasOverlappingCheckedInOrConfirmed)
            {
                return ReceptionistBookingResult.Failure("Phòng đang có booking đã xác nhận hoặc đã check-in trùng thời gian.");
            }

            var now = DateTime.Now;
            booking.Status = BookingStatuses.CheckedIn;
            booking.CheckedInAt = now;
            booking.UpdatedAt = now;

            booking.Room.Status = RoomStatuses.Occupied;
            booking.Room.UpdatedAt = now;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = receptionistId,
                Action = "CheckInBooking",
                EntityName = "Booking",
                EntityId = booking.Id,
                Description = $"Receptionist check-in booking {booking.BookingCode}, phòng {booking.Room.RoomNumber}",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return ReceptionistBookingResult.Success(
                booking.Id,
                booking.BookingCode,
                $"Đã check-in booking {booking.BookingCode}."
            );
        }

        public async Task<ReceptionistBookingResult> AddServiceToBookingAsync(
            AddBookingServiceViewModel model,
            long receptionistId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingServices)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null)
            {
                return ReceptionistBookingResult.Failure("Không tìm thấy booking.");
            }

            var addServiceAvailability = GetAddServiceAvailability(booking);

            if (!addServiceAvailability.CanAddService)
            {
                return ReceptionistBookingResult.Failure(addServiceAvailability.Reason ?? "Không thể thêm dịch vụ cho booking này.");
            }

            if (model.ServiceId <= 0)
            {
                return ReceptionistBookingResult.Failure("Vui lòng chọn dịch vụ.");
            }

            if (model.Quantity <= 0)
            {
                return ReceptionistBookingResult.Failure("Số lượng dịch vụ phải lớn hơn 0.");
            }

            if (model.Quantity > 100)
            {
                return ReceptionistBookingResult.Failure("Số lượng dịch vụ tối đa là 100.");
            }

            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == model.ServiceId && s.Status == "Active");

            if (service == null)
            {
                return ReceptionistBookingResult.Failure("Dịch vụ không tồn tại hoặc không còn hoạt động.");
            }

            var now = DateTime.Now;
            var unitPrice = service.Price;
            var totalPrice = unitPrice * model.Quantity;

            _context.BookingServices.Add(new BookingService
            {
                BookingId = booking.Id,
                ServiceId = service.Id,
                Quantity = model.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = totalPrice,
                UsedAt = now,
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
                CreatedByUserId = receptionistId
            });

            booking.TotalServiceAmount += totalPrice;
            booking.TotalAmount = booking.TotalRoomAmount + booking.TotalServiceAmount;
            booking.UpdatedAt = now;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = receptionistId,
                Action = "AddBookingService",
                EntityName = "Booking",
                EntityId = booking.Id,
                Description = $"Receptionist thêm dịch vụ {service.Name} x{model.Quantity} cho booking {booking.BookingCode}",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return ReceptionistBookingResult.Success(
                booking.Id,
                booking.BookingCode,
                $"Đã thêm dịch vụ {service.Name} cho booking {booking.BookingCode}."
            );
        }

        public async Task<ReceptionistBookingResult> CheckOutBookingAsync(long bookingId, long receptionistId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return ReceptionistBookingResult.Failure("Không tìm thấy booking.");
            }

            var checkOutAvailability = GetCheckOutAvailability(booking);

            if (!checkOutAvailability.CanCheckOut)
            {
                return ReceptionistBookingResult.Failure(checkOutAvailability.Reason ?? "Booking không thể check-out.");
            }

            if (booking.Room == null)
            {
                return ReceptionistBookingResult.Failure("Booking không có thông tin phòng hợp lệ.");
            }

            if (booking.Room.Status != RoomStatuses.Occupied)
            {
                return ReceptionistBookingResult.Failure("Phòng không ở trạng thái đang ở, không thể check-out.");
            }

            var now = DateTime.Now;
            booking.Status = BookingStatuses.CheckedOut;
            booking.CheckedOutAt = now;
            booking.UpdatedAt = now;

            booking.Room.Status = RoomStatuses.Cleaning;
            booking.Room.UpdatedAt = now;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = receptionistId,
                Action = "CheckOutBooking",
                EntityName = "Booking",
                EntityId = booking.Id,
                Description = $"Receptionist check-out booking {booking.BookingCode}, phòng {booking.Room.RoomNumber}",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return ReceptionistBookingResult.Success(
                booking.Id,
                booking.BookingCode,
                $"Đã check-out booking {booking.BookingCode}."
            );
        }

        private static List<SelectListItem> GetStatusOptions(string? selectedStatus)
        {
            return new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "",
                    Text = "Tất cả trạng thái",
                    Selected = string.IsNullOrWhiteSpace(selectedStatus)
                },
                new SelectListItem
                {
                    Value = BookingStatuses.Pending,
                    Text = "Chờ xác nhận",
                    Selected = selectedStatus == BookingStatuses.Pending
                },
                new SelectListItem
                {
                    Value = BookingStatuses.Confirmed,
                    Text = "Đã xác nhận",
                    Selected = selectedStatus == BookingStatuses.Confirmed
                },
                new SelectListItem
                {
                    Value = BookingStatuses.CheckedIn,
                    Text = "Đã nhận phòng",
                    Selected = selectedStatus == BookingStatuses.CheckedIn
                },
                new SelectListItem
                {
                    Value = BookingStatuses.CheckedOut,
                    Text = "Đã trả phòng",
                    Selected = selectedStatus == BookingStatuses.CheckedOut
                },
                new SelectListItem
                {
                    Value = BookingStatuses.Cancelled,
                    Text = "Đã hủy",
                    Selected = selectedStatus == BookingStatuses.Cancelled
                }
            };
        }

        private async Task<bool> HasOverlappingActiveBookingAsync(
            long roomId,
            DateTime checkInDate,
            DateTime checkOutDate,
            long excludedBookingId)
        {
            var activeStatuses = new[]
            {
                BookingStatuses.Pending,
                BookingStatuses.Confirmed,
                BookingStatuses.CheckedIn
            };

            return await _context.Bookings
                .AsNoTracking()
                .AnyAsync(b =>
                    b.Id != excludedBookingId
                    && b.RoomId == roomId
                    && activeStatuses.Contains(b.Status)
                    && b.CheckInDate < checkOutDate
                    && b.CheckOutDate > checkInDate
                );
        }

        private static (bool CanCheckIn, string? Reason) GetCheckInAvailability(Booking booking)
        {
            if (booking.Status != BookingStatuses.Confirmed)
            {
                return (false, "Chỉ có thể check-in booking ở trạng thái Đã xác nhận.");
            }

            var today = DateTime.Today;

            if (booking.CheckInDate.Date > today)
            {
                return (false, "Chưa đến ngày nhận phòng, không thể check-in.");
            }

            if (booking.CheckOutDate.Date <= today)
            {
                return (false, "Booking đã quá ngày lưu trú, không thể check-in.");
            }

            return (true, null);
        }

        private static (bool CanAddService, string? Reason) GetAddServiceAvailability(Booking booking)
        {
            if (booking.Status != BookingStatuses.CheckedIn)
            {
                return (false, "Chỉ có thể thêm dịch vụ cho booking đang ở trạng thái Đã nhận phòng.");
            }

            return (true, null);
        }

        private static (bool CanCheckOut, string? Reason) GetCheckOutAvailability(Booking booking)
        {
            if (booking.Status != BookingStatuses.CheckedIn)
            {
                return (false, "Chỉ có thể check-out booking ở trạng thái Đã nhận phòng.");
            }

            return (true, null);
        }

        private async Task<bool> HasOverlappingCheckedInOrConfirmedBookingAsync(
            long roomId,
            DateTime checkInDate,
            DateTime checkOutDate,
            long excludedBookingId)
        {
            var activeStatuses = new[]
            {
                BookingStatuses.Confirmed,
                BookingStatuses.CheckedIn
            };

            return await _context.Bookings
                .AsNoTracking()
                .AnyAsync(b =>
                    b.Id != excludedBookingId
                    && b.RoomId == roomId
                    && activeStatuses.Contains(b.Status)
                    && b.CheckInDate < checkOutDate
                    && b.CheckOutDate > checkInDate
                );
        }
    }
}
