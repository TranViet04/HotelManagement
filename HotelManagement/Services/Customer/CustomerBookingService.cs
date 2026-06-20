using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services.Cancellation;
using HotelManagement.Services.Payments;
using HotelManagement.ViewModels.Customer;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Customer
{
    public class CustomerBookingService
    {
        private readonly HotelDbContext _context;
        private readonly CancellationService _cancellationService;
        private readonly PaymentService _paymentService;

        public CustomerBookingService(
            HotelDbContext context,
            CancellationService cancellationService,
            PaymentService paymentService)
        {
            _context = context;
            _cancellationService = cancellationService;
            _paymentService = paymentService;
        }

        public async Task<CreateBookingViewModel?> PrepareCreateBookingAsync(
            long roomId,
            DateTime? checkInDate,
            DateTime? checkOutDate,
            int adults,
            int children)
        {
            var room = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.RoomType == null)
            {
                return null;
            }

            if (room.Status != RoomStatuses.Available)
            {
                return null;
            }

            if (room.RoomType.Status != "Active")
            {
                return null;
            }

            var finalCheckInDate = (checkInDate ?? DateTime.Today).Date;
            var finalCheckOutDate = (checkOutDate ?? DateTime.Today.AddDays(1)).Date;
            var finalAdults = adults <= 0 ? 1 : adults;
            var finalChildren = children < 0 ? 0 : children;
            var nights = Math.Max(1, (finalCheckOutDate - finalCheckInDate).Days);

            var model = new CreateBookingViewModel
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                Floor = room.Floor,
                RoomTypeId = room.RoomTypeId,
                RoomTypeName = room.RoomType.Name,
                Description = room.RoomType.Description,
                PricePerNight = room.RoomType.Price,
                Capacity = room.RoomType.Capacity,
                BedType = room.RoomType.BedType,
                ThumbnailUrl = room.RoomType.ThumbnailUrl,
                CheckInDate = finalCheckInDate,
                CheckOutDate = finalCheckOutDate,
                Adults = finalAdults,
                Children = finalChildren,
                Nights = nights,
                TotalRoomAmount = nights * room.RoomType.Price
            };

            if (finalCheckInDate < DateTime.Today)
            {
                model.IsAvailableForSelectedDates = false;
                model.AvailabilityMessage = "Ngày nhận phòng không được nhỏ hơn hôm nay.";
                return model;
            }

            if (finalCheckOutDate <= finalCheckInDate)
            {
                model.IsAvailableForSelectedDates = false;
                model.AvailabilityMessage = "Ngày trả phòng phải lớn hơn ngày nhận phòng.";
                return model;
            }

            if (finalAdults + finalChildren > room.RoomType.Capacity)
            {
                model.IsAvailableForSelectedDates = false;
                model.AvailabilityMessage = "Số lượng khách vượt quá sức chứa của phòng.";
                return model;
            }

            var isAvailable = await IsRoomAvailableAsync(room.Id, finalCheckInDate, finalCheckOutDate);

            model.IsAvailableForSelectedDates = isAvailable;
            model.AvailabilityMessage = isAvailable
                ? "Phòng còn trống trong khoảng thời gian đã chọn."
                : "Phòng không còn trống trong khoảng thời gian đã chọn.";

            return model;
        }

        public async Task<CustomerBookingResult> CreateBookingAsync(CreateBookingViewModel model, long customerId)
        {
            var selectModel = await PrepareSelectServicesAsync(
                model.RoomId,
                model.CheckInDate,
                model.CheckOutDate,
                model.Adults,
                model.Children);

            if (selectModel == null)
            {
                return CustomerBookingResult.Failure("Không tìm thấy phòng hoặc phòng không thể đặt.");
            }

            selectModel.SpecialRequest = model.SpecialRequest;

            return await CreateBookingWithServicesAsync(selectModel, customerId);
        }

        public async Task<SelectServicesViewModel?> PrepareSelectServicesAsync(
            long roomId,
            DateTime? checkInDate,
            DateTime? checkOutDate,
            int adults,
            int children)
        {
            var bookingModel = await PrepareCreateBookingAsync(
                roomId,
                checkInDate,
                checkOutDate,
                adults,
                children);

            if (bookingModel == null || !bookingModel.IsAvailableForSelectedDates)
            {
                return null;
            }

            var services = await _context.Services
                .AsNoTracking()
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Name)
                .Select(s => new ServiceSelectionItemViewModel
                {
                    ServiceId = s.Id,
                    Name = s.Name,
                    Category = s.Category,
                    Unit = s.Unit,
                    Price = s.Price,
                    IsSelected = false,
                    Quantity = 1
                })
                .ToListAsync();

            return new SelectServicesViewModel
            {
                RoomId = bookingModel.RoomId,
                RoomNumber = bookingModel.RoomNumber,
                RoomTypeName = bookingModel.RoomTypeName,
                PricePerNight = bookingModel.PricePerNight,
                Capacity = bookingModel.Capacity,
                ThumbnailUrl = bookingModel.ThumbnailUrl,
                CheckInDate = bookingModel.CheckInDate,
                CheckOutDate = bookingModel.CheckOutDate,
                Adults = bookingModel.Adults,
                Children = bookingModel.Children,
                Nights = bookingModel.Nights,
                TotalRoomAmount = bookingModel.TotalRoomAmount,
                AvailableServices = services
            };
        }

        public async Task<CustomerBookingResult> CreateBookingWithServicesAsync(
            SelectServicesViewModel model,
            long customerId)
        {
            var checkInDate = model.CheckInDate.Date;
            var checkOutDate = model.CheckOutDate.Date;

            if (model.RoomId <= 0)
            {
                return CustomerBookingResult.Failure("Phòng không hợp lệ.");
            }

            if (checkInDate < DateTime.Today)
            {
                return CustomerBookingResult.Failure("Ngày nhận phòng không được nhỏ hơn hôm nay.");
            }

            if (checkOutDate <= checkInDate)
            {
                return CustomerBookingResult.Failure("Ngày trả phòng phải lớn hơn ngày nhận phòng.");
            }

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == model.RoomId);

            if (room == null || room.RoomType == null)
            {
                return CustomerBookingResult.Failure("Không tìm thấy phòng.");
            }

            if (room.Status != RoomStatuses.Available)
            {
                return CustomerBookingResult.Failure("Phòng hiện không ở trạng thái có thể đặt.");
            }

            if (model.Adults + model.Children > room.RoomType.Capacity)
            {
                return CustomerBookingResult.Failure("Số lượng khách vượt quá sức chứa của phòng.");
            }

            var isAvailable = await IsRoomAvailableAsync(room.Id, checkInDate, checkOutDate);

            if (!isAvailable)
            {
                return CustomerBookingResult.Failure("Phòng đã có người đặt trong khoảng thời gian này.");
            }

            var nights = (checkOutDate - checkInDate).Days;
            var totalRoomAmount = nights * room.RoomType.Price;
            var bookingCode = await GenerateBookingCodeAsync();
            var now = DateTime.Now;

            var selectedServices = model.AvailableServices
                .Where(s => s.IsSelected && s.Quantity > 0)
                .ToList();

            var activeServiceIds = await _context.Services
                .Where(s => s.Status == "Active")
                .Select(s => new { s.Id, s.Price })
                .ToListAsync();

            decimal totalServiceAmount = 0;
            var bookingServices = new List<BookingService>();

            foreach (var selected in selectedServices)
            {
                var service = activeServiceIds.FirstOrDefault(s => s.Id == selected.ServiceId);

                if (service == null)
                {
                    continue;
                }

                var lineTotal = service.Price * selected.Quantity;
                totalServiceAmount += lineTotal;

                bookingServices.Add(new BookingService
                {
                    ServiceId = service.Id,
                    Quantity = selected.Quantity,
                    UnitPrice = service.Price,
                    TotalPrice = lineTotal,
                    CreatedByUserId = customerId,
                    UsedAt = now
                });
            }

            var totalAmount = totalRoomAmount + totalServiceAmount;

            var booking = new Booking
            {
                BookingCode = bookingCode,
                CustomerId = customerId,
                RoomId = room.Id,
                CreatedByUserId = customerId,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                Adults = model.Adults,
                Children = model.Children,
                Status = BookingStatuses.Pending,
                TotalRoomAmount = totalRoomAmount,
                TotalServiceAmount = totalServiceAmount,
                TotalAmount = totalAmount,
                SpecialRequest = model.SpecialRequest,
                CreatedAt = now
            };

            foreach (var bs in bookingServices)
            {
                booking.BookingServices.Add(bs);
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var invoiceCode = await GenerateInvoiceCodeAsync();
            var invoice = new Invoice
            {
                InvoiceCode = invoiceCode,
                BookingId = booking.Id,
                RoomAmount = totalRoomAmount,
                ServiceAmount = totalServiceAmount,
                TotalAmount = totalAmount,
                PaidAmount = 0,
                RemainingAmount = totalAmount,
                Status = InvoiceStatuses.Unpaid,
                IssuedByUserId = customerId,
                IssuedAt = now
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            await _paymentService.CreatePendingSepayPaymentAsync(invoice, customerId);

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = customerId,
                Action = "CreateBooking",
                EntityName = "Booking",
                EntityId = booking.Id,
                Description = $"Customer tạo booking online cho phòng {room.RoomNumber}, chờ thanh toán Sepay",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return CustomerBookingResult.Success(booking.Id, booking.BookingCode);
        }

        public async Task<List<MyBookingListItemViewModel>> GetMyBookingsAsync(long customerId)
        {
            var cancellableStatuses = new[]
            {
                BookingStatuses.Pending,
                BookingStatuses.Confirmed
            };

            var bookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new MyBookingListItemViewModel
                {
                    BookingId = b.Id,
                    BookingCode = b.BookingCode,
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : "Không xác định",
                    RoomTypeName = b.Room != null && b.Room.RoomType != null
                        ? b.Room.RoomType.Name
                        : "Không xác định",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Adults = b.Adults,
                    Children = b.Children,
                    TotalAmount = b.TotalAmount,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    CanCancel = cancellableStatuses.Contains(b.Status)
                })
                .ToListAsync();

            foreach (var booking in bookings)
            {
                booking.Nights = Math.Max(0, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days);
            }

            return bookings;
        }

        public async Task<BookingDetailViewModel?> GetMyBookingDetailAsync(long bookingId, long customerId)
        {
            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

            if (booking == null)
            {
                return null;
            }

            return new BookingDetailViewModel
            {
                BookingId = booking.Id,
                BookingCode = booking.BookingCode,
                CustomerName = booking.Customer?.FullName ?? "Không xác định",
                CustomerEmail = booking.Customer?.Email,
                CustomerPhoneNumber = booking.Customer?.PhoneNumber,
                RoomId = booking.RoomId,
                RoomNumber = booking.Room?.RoomNumber ?? "Không xác định",
                Floor = booking.Room?.Floor,
                RoomTypeName = booking.Room?.RoomType?.Name ?? "Không xác định",
                RoomDescription = booking.Room?.RoomType?.Description,
                PricePerNight = booking.Room?.RoomType?.Price ?? 0,
                Capacity = booking.Room?.RoomType?.Capacity ?? 0,
                BedType = booking.Room?.RoomType?.BedType,
                ThumbnailUrl = booking.Room?.RoomType?.ThumbnailUrl,
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
                CanCancel = CanCancelBookingStatus(booking.Status),
                Services = booking.BookingServices
                    .OrderByDescending(bs => bs.UsedAt)
                    .Select(bs => new BookingServiceItemViewModel
                    {
                        ServiceName = bs.Service?.Name ?? "Dịch vụ không xác định",
                        Category = bs.Service?.Category,
                        Unit = bs.Service?.Unit,
                        Quantity = bs.Quantity,
                        UnitPrice = bs.UnitPrice,
                        TotalPrice = bs.TotalPrice,
                        UsedAt = bs.UsedAt,
                        Note = bs.Note
                    })
                    .ToList()
            };
        }

        public async Task<CancelBookingViewModel?> PrepareCancelBookingAsync(long bookingId, long customerId)
        {
            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Include(b => b.Invoice)
                    .ThenInclude(i => i!.Payments)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

            if (booking == null)
            {
                return null;
            }

            var canCancel = CanCancelBookingStatus(booking.Status);
            var paidAt = booking.Invoice?.PaidAt
                ?? booking.Invoice?.Payments
                    .Where(p => p.Status == PaymentStatuses.Paid)
                    .OrderByDescending(p => p.PaidAt)
                    .Select(p => p.PaidAt)
                    .FirstOrDefault();

            var cancellation = _cancellationService.CalculateRefund(
                booking.TotalAmount,
                booking.CheckInDate,
                DateTime.Now,
                paidAt);

            return new CancelBookingViewModel
            {
                BookingId = booking.Id,
                BookingCode = booking.BookingCode,
                RoomNumber = booking.Room?.RoomNumber ?? "Không xác định",
                RoomTypeName = booking.Room?.RoomType?.Name ?? "Không xác định",
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Nights = Math.Max(0, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days),
                Adults = booking.Adults,
                Children = booking.Children,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,
                CanCancel = canCancel,
                CancelBlockReason = canCancel
                    ? null
                    : "Chỉ có thể hủy đặt phòng ở trạng thái Chờ xác nhận hoặc Đã xác nhận.",
                RefundPercent = cancellation.RefundPercent,
                RefundAmount = cancellation.RefundAmount,
                CancellationFee = cancellation.CancellationFee,
                PolicyDescription = cancellation.PolicyDescription,
                IsPaidBooking = cancellation.IsPaidBooking
            };
        }

        public async Task<CustomerBookingResult> CancelMyBookingAsync(
            long bookingId,
            long customerId,
            string? cancelReason)
        {
            var booking = await _context.Bookings
                .Include(b => b.Invoice)
                    .ThenInclude(i => i!.Payments)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

            if (booking == null)
            {
                return CustomerBookingResult.Failure("Không tìm thấy đặt phòng hoặc bạn không có quyền hủy đặt phòng này.");
            }

            if (!CanCancelBookingStatus(booking.Status))
            {
                return CustomerBookingResult.Failure("Đặt phòng này không còn được phép hủy.");
            }

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                return CustomerBookingResult.Failure("Vui lòng nhập lý do hủy đặt phòng.");
            }

            cancelReason = cancelReason.Trim();

            if (cancelReason.Length > 500)
            {
                return CustomerBookingResult.Failure("Lý do hủy tối đa 500 ký tự.");
            }

            var now = DateTime.Now;
            var paidAt = booking.Invoice?.PaidAt
                ?? booking.Invoice?.Payments
                    .Where(p => p.Status == PaymentStatuses.Paid)
                    .OrderByDescending(p => p.PaidAt)
                    .Select(p => p.PaidAt)
                    .FirstOrDefault();

            var cancellation = _cancellationService.CalculateRefund(
                booking.TotalAmount,
                booking.CheckInDate,
                now,
                paidAt);

            booking.Status = BookingStatuses.Cancelled;
            booking.CancelReason = cancelReason;
            booking.CancelledAt = now;
            booking.UpdatedAt = now;
            booking.RefundAmount = cancellation.RefundAmount;

            if (booking.Invoice != null && cancellation.IsPaidBooking)
            {
                var paidPayment = booking.Invoice.Payments
                    .FirstOrDefault(p => p.Status == PaymentStatuses.Paid);

                if (paidPayment != null && cancellation.RefundPercent == 100)
                {
                    paidPayment.Status = PaymentStatuses.Refunded;
                    booking.Invoice.Status = InvoiceStatuses.Cancelled;
                }
                else if (paidPayment != null && cancellation.RefundPercent > 0)
                {
                    paidPayment.Note = $"Hoàn tiền {cancellation.RefundAmount:N0} VND ({cancellation.RefundPercent}%)";
                    booking.Invoice.Status = InvoiceStatuses.PartiallyPaid;
                    booking.Invoice.RemainingAmount = cancellation.CancellationFee;
                }
                else if (paidPayment != null)
                {
                    booking.Invoice.Status = InvoiceStatuses.Paid;
                }
            }
            else if (booking.Invoice != null)
            {
                booking.Invoice.Status = InvoiceStatuses.Cancelled;

                foreach (var payment in booking.Invoice.Payments
                             .Where(p => p.Status == PaymentStatuses.Pending))
                {
                    payment.Status = PaymentStatuses.Cancelled;
                }
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = customerId,
                Action = "CancelBooking",
                EntityName = "Booking",
                EntityId = booking.Id,
                Description = $"Customer hủy booking {booking.BookingCode}. Lý do: {cancelReason}. Hoàn tiền: {cancellation.RefundAmount:N0} VND ({cancellation.PolicyDescription})",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            var message = cancellation.IsPaidBooking
                ? $"Đã hủy đặt phòng. {cancellation.PolicyDescription} Số tiền hoàn: {cancellation.RefundAmount:N0} VND."
                : "Đã hủy đặt phòng thành công.";

            return CustomerBookingResult.Success(booking.Id, booking.BookingCode, message);
        }

        private async Task<bool> IsRoomAvailableAsync(long roomId, DateTime checkInDate, DateTime checkOutDate)
        {
            var unavailableBookingStatuses = new[]
            {
                BookingStatuses.Pending,
                BookingStatuses.Confirmed,
                BookingStatuses.CheckedIn
            };

            var hasOverlappingBooking = await _context.Bookings
                .AnyAsync(b =>
                    b.RoomId == roomId
                    && unavailableBookingStatuses.Contains(b.Status)
                    && b.CheckInDate < checkOutDate
                    && b.CheckOutDate > checkInDate
                );

            return !hasOverlappingBooking;
        }

        private static bool CanCancelBookingStatus(string status)
        {
            return status == BookingStatuses.Pending
                || status == BookingStatuses.Confirmed;
        }

        private async Task<string> GenerateInvoiceCodeAsync()
        {
            for (var i = 0; i < 5; i++)
            {
                var code = $"INV{DateTime.Now:yyyyMMddHHmmssfff}{Random.Shared.Next(100, 999)}";
                var exists = await _context.Invoices.AnyAsync(i => i.InvoiceCode == code);

                if (!exists)
                {
                    return code;
                }
            }

            return $"INV{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
        }

        private async Task<string> GenerateBookingCodeAsync()
        {
            for (var i = 0; i < 5; i++)
            {
                var code = $"BK{DateTime.Now:yyyyMMddHHmmssfff}{Random.Shared.Next(100, 999)}";
                var exists = await _context.Bookings.AnyAsync(b => b.BookingCode == code);

                if (!exists)
                {
                    return code;
                }
            }

            return $"BK{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
        }
    }
}
