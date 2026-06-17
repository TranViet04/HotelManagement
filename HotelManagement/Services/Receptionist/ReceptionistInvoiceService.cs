using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Receptionist;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Receptionist
{
    public class ReceptionistInvoiceService
    {
        private readonly HotelDbContext _context;

        public ReceptionistInvoiceService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<ReceptionistInvoiceListViewModel> GetInvoicesAsync(string? keyword, string? status)
        {
            keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
            status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();

            var query = _context.Invoices
                .AsNoTracking()
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Room)
                        .ThenInclude(r => r!.RoomType)
                .Include(i => i.IssuedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(i =>
                    i.InvoiceCode.Contains(keyword)
                    || (i.Booking != null && i.Booking.BookingCode.Contains(keyword))
                    || (i.Booking != null && i.Booking.Customer != null && i.Booking.Customer.FullName.Contains(keyword))
                    || (i.Booking != null && i.Booking.Customer != null && i.Booking.Customer.PhoneNumber != null && i.Booking.Customer.PhoneNumber.Contains(keyword))
                    || (i.Booking != null && i.Booking.Room != null && i.Booking.Room.RoomNumber.Contains(keyword))
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(i => i.Status == status);
            }

            var invoices = await query
                .OrderByDescending(i => i.IssuedAt)
                .ThenByDescending(i => i.Id)
                .Select(i => new ReceptionistInvoiceListItemViewModel
                {
                    InvoiceId = i.Id,
                    InvoiceCode = i.InvoiceCode,
                    BookingId = i.BookingId,
                    BookingCode = i.Booking != null ? i.Booking.BookingCode : "Không xác định",
                    CustomerName = i.Booking != null && i.Booking.Customer != null
                        ? i.Booking.Customer.FullName
                        : "Không xác định",
                    CustomerPhoneNumber = i.Booking != null && i.Booking.Customer != null
                        ? i.Booking.Customer.PhoneNumber
                        : null,
                    RoomNumber = i.Booking != null && i.Booking.Room != null
                        ? i.Booking.Room.RoomNumber
                        : "Không xác định",
                    RoomTypeName = i.Booking != null && i.Booking.Room != null && i.Booking.Room.RoomType != null
                        ? i.Booking.Room.RoomType.Name
                        : "Không xác định",
                    TotalAmount = i.TotalAmount,
                    PaidAmount = i.PaidAmount,
                    RemainingAmount = i.RemainingAmount,
                    Status = i.Status,
                    IssuedAt = i.IssuedAt,
                    IssuedByName = i.IssuedByUser != null ? i.IssuedByUser.FullName : null
                })
                .ToListAsync();

            return new ReceptionistInvoiceListViewModel
            {
                Keyword = keyword,
                Status = status,
                StatusOptions = GetInvoiceStatusOptions(status),
                Invoices = invoices
            };
        }

        public async Task<ReceptionistInvoiceDetailViewModel?> GetInvoiceDetailAsync(long invoiceId)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.IssuedByUser)
                .Include(i => i.Payments)
                    .ThenInclude(p => p.CreatedByUser)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Room)
                        .ThenInclude(r => r!.RoomType)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.BookingServices)
                        .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null || invoice.Booking == null)
            {
                return null;
            }

            var booking = invoice.Booking;
            var paymentAvailability = GetRecordPaymentAvailability(invoice);

            return new ReceptionistInvoiceDetailViewModel
            {
                InvoiceId = invoice.Id,
                InvoiceCode = invoice.InvoiceCode,
                Status = invoice.Status,
                IssuedAt = invoice.IssuedAt,
                IssuedByName = invoice.IssuedByUser?.FullName,
                Note = invoice.Note,
                BookingId = booking.Id,
                BookingCode = booking.BookingCode,
                BookingStatus = booking.Status,
                CustomerName = booking.Customer?.FullName ?? "Không xác định",
                CustomerEmail = booking.Customer?.Email,
                CustomerPhoneNumber = booking.Customer?.PhoneNumber,
                CustomerIdentityNumber = booking.Customer?.IdentityNumber,
                CustomerAddress = booking.Customer?.Address,
                RoomNumber = booking.Room?.RoomNumber ?? "Không xác định",
                RoomTypeName = booking.Room?.RoomType?.Name ?? "Không xác định",
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Nights = Math.Max(0, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days),
                RoomAmount = invoice.RoomAmount,
                ServiceAmount = invoice.ServiceAmount,
                DiscountAmount = invoice.DiscountAmount,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.PaidAmount,
                RemainingAmount = invoice.RemainingAmount,
                PaidAt = invoice.PaidAt,
                CanRecordPayment = paymentAvailability.CanRecordPayment,
                RecordPaymentBlockReason = paymentAvailability.Reason,
                Services = booking.BookingServices
                    .OrderBy(bs => bs.UsedAt)
                    .Select(bs => new ReceptionistInvoiceServiceLineViewModel
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
                    .ToList(),
                Payments = invoice.Payments
                    .OrderByDescending(p => p.PaidAt)
                    .Select(p => new ReceptionistInvoicePaymentLineViewModel
                    {
                        PaymentCode = p.PaymentCode,
                        PaymentMethod = p.PaymentMethod,
                        Amount = p.Amount,
                        Status = p.Status,
                        PaidAt = p.PaidAt,
                        CreatedByName = p.CreatedByUser?.FullName,
                        Note = p.Note
                    })
                    .ToList()
            };
        }

        public async Task<ReceptionistInvoiceResult> CreateInvoiceAsync(long bookingId, long receptionistId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Invoice)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return ReceptionistInvoiceResult.Failure("Không tìm thấy booking.");
            }

            if (booking.Invoice != null)
            {
                return ReceptionistInvoiceResult.Success(
                    booking.Invoice.Id,
                    booking.Invoice.InvoiceCode,
                    $"Booking {booking.BookingCode} đã có hóa đơn {booking.Invoice.InvoiceCode}."
                );
            }

            if (booking.Status != BookingStatuses.CheckedOut)
            {
                return ReceptionistInvoiceResult.Failure("Chỉ có thể tạo hóa đơn cho booking đã trả phòng.");
            }

            var now = DateTime.Now;
            var invoice = new Invoice
            {
                InvoiceCode = await GenerateInvoiceCodeAsync(),
                BookingId = booking.Id,
                RoomAmount = booking.TotalRoomAmount,
                ServiceAmount = booking.TotalServiceAmount,
                DiscountAmount = 0,
                TaxAmount = 0,
                TotalAmount = booking.TotalAmount,
                PaidAmount = 0,
                RemainingAmount = booking.TotalAmount,
                Status = InvoiceStatuses.Unpaid,
                IssuedByUserId = receptionistId,
                IssuedAt = now,
                PaidAt = null,
                Note = $"Hóa đơn tạo từ booking {booking.BookingCode}"
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = receptionistId,
                Action = "CreateInvoice",
                EntityName = "Invoice",
                EntityId = invoice.Id,
                Description = $"Receptionist tạo hóa đơn {invoice.InvoiceCode} cho booking {booking.BookingCode}",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return ReceptionistInvoiceResult.Success(
                invoice.Id,
                invoice.InvoiceCode,
                $"Đã tạo hóa đơn {invoice.InvoiceCode} cho booking {booking.BookingCode}."
            );
        }

        public async Task<ReceptionistPaymentListViewModel> GetPaymentInvoicesAsync(string? keyword)
        {
            keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

            var payableStatuses = new[]
            {
                InvoiceStatuses.Unpaid,
                InvoiceStatuses.PartiallyPaid
            };

            var query = _context.Invoices
                .AsNoTracking()
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Room)
                        .ThenInclude(r => r!.RoomType)
                .Where(i => payableStatuses.Contains(i.Status) && i.RemainingAmount > 0)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(i =>
                    i.InvoiceCode.Contains(keyword)
                    || (i.Booking != null && i.Booking.BookingCode.Contains(keyword))
                    || (i.Booking != null && i.Booking.Customer != null && i.Booking.Customer.FullName.Contains(keyword))
                    || (i.Booking != null && i.Booking.Customer != null && i.Booking.Customer.PhoneNumber != null && i.Booking.Customer.PhoneNumber.Contains(keyword))
                    || (i.Booking != null && i.Booking.Room != null && i.Booking.Room.RoomNumber.Contains(keyword))
                );
            }

            var invoices = await query
                .OrderByDescending(i => i.IssuedAt)
                .ThenByDescending(i => i.Id)
                .Select(i => new ReceptionistPaymentInvoiceItemViewModel
                {
                    InvoiceId = i.Id,
                    InvoiceCode = i.InvoiceCode,
                    BookingId = i.BookingId,
                    BookingCode = i.Booking != null ? i.Booking.BookingCode : "Không xác định",
                    CustomerName = i.Booking != null && i.Booking.Customer != null
                        ? i.Booking.Customer.FullName
                        : "Không xác định",
                    CustomerPhoneNumber = i.Booking != null && i.Booking.Customer != null
                        ? i.Booking.Customer.PhoneNumber
                        : null,
                    RoomNumber = i.Booking != null && i.Booking.Room != null
                        ? i.Booking.Room.RoomNumber
                        : "Không xác định",
                    RoomTypeName = i.Booking != null && i.Booking.Room != null && i.Booking.Room.RoomType != null
                        ? i.Booking.Room.RoomType.Name
                        : "Không xác định",
                    TotalAmount = i.TotalAmount,
                    PaidAmount = i.PaidAmount,
                    RemainingAmount = i.RemainingAmount,
                    Status = i.Status,
                    IssuedAt = i.IssuedAt
                })
                .ToListAsync();

            return new ReceptionistPaymentListViewModel
            {
                Keyword = keyword,
                Invoices = invoices
            };
        }

        public async Task<RecordPaymentViewModel?> PrepareRecordPaymentAsync(long invoiceId)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Payments)
                    .ThenInclude(p => p.CreatedByUser)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Room)
                        .ThenInclude(r => r!.RoomType)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.BookingServices)
                        .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null || invoice.Booking == null)
            {
                return null;
            }

            var paymentAvailability = GetRecordPaymentAvailability(invoice);

            return new RecordPaymentViewModel
            {
                InvoiceId = invoice.Id,
                InvoiceCode = invoice.InvoiceCode,
                BookingId = invoice.BookingId,
                BookingCode = invoice.Booking.BookingCode,
                CustomerName = invoice.Booking.Customer?.FullName ?? "Không xác định",
                CustomerPhoneNumber = invoice.Booking.Customer?.PhoneNumber,
                RoomNumber = invoice.Booking.Room?.RoomNumber ?? "Không xác định",
                RoomTypeName = invoice.Booking.Room?.RoomType?.Name ?? "Không xác định",
                TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.PaidAmount,
                RemainingAmount = invoice.RemainingAmount,
                InvoiceStatus = invoice.Status,
                Amount = invoice.RemainingAmount > 0 ? invoice.RemainingAmount : 0,
                CanRecordPayment = paymentAvailability.CanRecordPayment,
                RecordPaymentBlockReason = paymentAvailability.Reason,
                PaymentMethodOptions = GetPaymentMethodOptions(),
                UnpaidServices = invoice.Booking.BookingServices
                    .OrderBy(bs => bs.UsedAt)
                    .Select(bs => new ReceptionistBookingServiceItemViewModel
                    {
                        BookingServiceId = bs.Id,
                        ServiceName = bs.Service?.Name ?? "Dịch vụ",
                        Category = bs.Service?.Category,
                        Unit = bs.Service?.Unit,
                        Quantity = bs.Quantity,
                        UnitPrice = bs.UnitPrice,
                        TotalPrice = bs.TotalPrice,
                        UsedAt = bs.UsedAt,
                        Note = bs.Note
                    })
                    .ToList(),
                ExistingPayments = invoice.Payments
                    .OrderByDescending(p => p.PaidAt)
                    .Select(p => new ReceptionistInvoicePaymentLineViewModel
                    {
                        PaymentCode = p.PaymentCode,
                        PaymentMethod = p.PaymentMethod,
                        Amount = p.Amount,
                        Status = p.Status,
                        PaidAt = p.PaidAt,
                        CreatedByName = p.CreatedByUser?.FullName,
                        Note = p.Note
                    })
                    .ToList()
            };
        }

        public async Task<ReceptionistInvoiceResult> RecordPaymentAsync(
            RecordPaymentViewModel model,
            long receptionistId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                .FirstOrDefaultAsync(i => i.Id == model.InvoiceId);

            if (invoice == null)
            {
                return ReceptionistInvoiceResult.Failure("Không tìm thấy hóa đơn.");
            }

            var paymentAvailability = GetRecordPaymentAvailability(invoice);

            if (!paymentAvailability.CanRecordPayment)
            {
                return ReceptionistInvoiceResult.Failure(paymentAvailability.Reason ?? "Không thể ghi nhận thanh toán cho hóa đơn này.");
            }

            if (model.Amount <= 0)
            {
                return ReceptionistInvoiceResult.Failure("Số tiền thanh toán phải lớn hơn 0.");
            }

            if (model.Amount > invoice.RemainingAmount)
            {
                return ReceptionistInvoiceResult.Failure("Số tiền thanh toán không được vượt quá số tiền còn lại.");
            }

            var validPaymentMethods = new[]
            {
                PaymentMethods.Cash,
                PaymentMethods.BankTransfer
            };

            if (string.IsNullOrWhiteSpace(model.PaymentMethod) || !validPaymentMethods.Contains(model.PaymentMethod))
            {
                return ReceptionistInvoiceResult.Failure("Phương thức thanh toán không hợp lệ.");
            }

            var now = DateTime.Now;
            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                PaymentCode = await GeneratePaymentCodeAsync(),
                PaymentMethod = model.PaymentMethod,
                Amount = model.Amount,
                Status = PaymentStatuses.Paid,
                PaidAt = now,
                CreatedByUserId = receptionistId,
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim()
            };

            _context.Payments.Add(payment);

            invoice.PaidAmount += model.Amount;
            invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;

            if (invoice.RemainingAmount < 0)
            {
                invoice.RemainingAmount = 0;
            }

            if (invoice.RemainingAmount == 0)
            {
                invoice.Status = InvoiceStatuses.Paid;
                invoice.PaidAt = now;
            }
            else
            {
                invoice.Status = InvoiceStatuses.PartiallyPaid;
                invoice.PaidAt = null;
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = receptionistId,
                Action = "RecordPayment",
                EntityName = "Invoice",
                EntityId = invoice.Id,
                Description = $"Receptionist ghi nhận thanh toán {payment.Amount:N0} VND cho hóa đơn {invoice.InvoiceCode}",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return ReceptionistInvoiceResult.Success(
                invoice.Id,
                invoice.InvoiceCode,
                $"Đã ghi nhận thanh toán {payment.Amount:N0} VND cho hóa đơn {invoice.InvoiceCode}."
            );
        }

        private async Task<string> GenerateInvoiceCodeAsync()
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var suffix = attempt == 0 ? string.Empty : attempt.ToString("00");
                var code = "INV" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + suffix;

                var exists = await _context.Invoices
                    .AsNoTracking()
                    .AnyAsync(i => i.InvoiceCode == code);

                if (!exists)
                {
                    return code;
                }
            }

            return "INV" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
        }

        private async Task<string> GeneratePaymentCodeAsync()
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var suffix = attempt == 0 ? string.Empty : attempt.ToString("00");
                var code = "PAY" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + suffix;

                var exists = await _context.Payments
                    .AsNoTracking()
                    .AnyAsync(p => p.PaymentCode == code);

                if (!exists)
                {
                    return code;
                }
            }

            return "PAY" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
        }

        private static List<SelectListItem> GetPaymentMethodOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "",
                    Text = "-- Chọn phương thức --"
                },
                new SelectListItem
                {
                    Value = PaymentMethods.Cash,
                    Text = "Tiền mặt"
                },
                new SelectListItem
                {
                    Value = PaymentMethods.BankTransfer,
                    Text = "Chuyển khoản"
                }
            };
        }

        private static (bool CanRecordPayment, string? Reason) GetRecordPaymentAvailability(Invoice invoice)
        {
            if (invoice.Status == InvoiceStatuses.Cancelled)
            {
                return (false, "Không thể thanh toán hóa đơn đã hủy.");
            }

            if (invoice.Status == InvoiceStatuses.Paid)
            {
                return (false, "Hóa đơn này đã thanh toán đủ.");
            }

            if (invoice.RemainingAmount <= 0)
            {
                return (false, "Hóa đơn không còn số tiền cần thanh toán.");
            }

            return (true, null);
        }

        private static List<SelectListItem> GetInvoiceStatusOptions(string? selectedStatus)
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
                    Value = InvoiceStatuses.Unpaid,
                    Text = "Chưa thanh toán",
                    Selected = selectedStatus == InvoiceStatuses.Unpaid
                },
                new SelectListItem
                {
                    Value = InvoiceStatuses.PartiallyPaid,
                    Text = "Thanh toán một phần",
                    Selected = selectedStatus == InvoiceStatuses.PartiallyPaid
                },
                new SelectListItem
                {
                    Value = InvoiceStatuses.Paid,
                    Text = "Đã thanh toán",
                    Selected = selectedStatus == InvoiceStatuses.Paid
                },
                new SelectListItem
                {
                    Value = InvoiceStatuses.Cancelled,
                    Text = "Đã hủy",
                    Selected = selectedStatus == InvoiceStatuses.Cancelled
                }
            };
        }
    }
}
