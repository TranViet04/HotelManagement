using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Admin
{
    public class CustomerManagementService
    {
        private readonly HotelDbContext _context;

        public CustomerManagementService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<CustomerListItemViewModel>> GetCustomersAsync()
        {
            return await _context.Users
                .Where(u => u.Role == UserRoles.Customer)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new CustomerListItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    IdentityNumber = u.IdentityNumber,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt,
                    BookingCount = _context.Bookings.Count(b => b.CustomerId == u.Id)
                })
                .ToListAsync();
        }

        public async Task<CustomerDetailViewModel?> GetDetailAsync(long id)
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRoles.Customer);

            if (customer == null)
            {
                return null;
            }

            var recentBookings = await _context.Bookings
                .Where(b => b.CustomerId == id)
                .OrderByDescending(b => b.CreatedAt)
                .Take(20)
                .Select(b => new CustomerBookingHistoryItemViewModel
                {
                    Id = b.Id,
                    BookingCode = b.BookingCode,
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : string.Empty,
                    RoomTypeName = b.Room != null && b.Room.RoomType != null ? b.Room.RoomType.Name : string.Empty,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            var bookingCount = await _context.Bookings
                .CountAsync(b => b.CustomerId == id);

            var totalBookingAmount = await _context.Bookings
                .Where(b => b.CustomerId == id && b.Status != BookingStatuses.Cancelled)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            return new CustomerDetailViewModel
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                IdentityNumber = customer.IdentityNumber,
                Address = customer.Address,
                Status = customer.Status,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,
                BookingCount = bookingCount,
                TotalBookingAmount = totalBookingAmount,
                RecentBookings = recentBookings
            };
        }

        public async Task<EditCustomerViewModel?> GetForEditAsync(long id)
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRoles.Customer);

            if (customer == null)
            {
                return null;
            }

            return new EditCustomerViewModel
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                IdentityNumber = customer.IdentityNumber,
                Address = customer.Address,
                Status = customer.Status
            };
        }

        public async Task<bool> IsEmailExistsAsync(string email, long? ignoreId = null)
        {
            var normalizedEmail = email.Trim().ToLower();
            var query = _context.Users.AsQueryable();

            if (ignoreId.HasValue)
            {
                query = query.Where(u => u.Id != ignoreId.Value);
            }

            return await query.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        }

        public async Task<bool> UpdateCustomerAsync(EditCustomerViewModel model)
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == model.Id && u.Role == UserRoles.Customer);

            if (customer == null)
            {
                return false;
            }

            customer.FullName = model.FullName.Trim();
            customer.Email = model.Email.Trim().ToLower();
            customer.PhoneNumber = model.PhoneNumber?.Trim();
            customer.IdentityNumber = model.IdentityNumber?.Trim();
            customer.Address = model.Address?.Trim();
            customer.Status = model.Status;
            customer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LockAsync(long id)
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRoles.Customer);

            if (customer == null)
            {
                return false;
            }

            customer.Status = UserStatuses.Locked;
            customer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlockAsync(long id)
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRoles.Customer);

            if (customer == null)
            {
                return false;
            }

            customer.Status = UserStatuses.Active;
            customer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(long id)
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRoles.Customer);

            if (customer == null)
            {
                return false;
            }

            customer.Status = UserStatuses.Inactive;
            customer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
