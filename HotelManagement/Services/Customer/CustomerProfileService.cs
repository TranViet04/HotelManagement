using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Customer;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Customer
{
    public class CustomerProfileService
    {
        private readonly HotelDbContext _context;

        public CustomerProfileService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerProfileViewModel?> GetMyProfileAsync(long customerId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == customerId && u.Role == UserRoles.Customer)
                .Select(u => new CustomerProfileViewModel
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    IdentityNumber = u.IdentityNumber,
                    Address = u.Address,
                    Role = u.Role,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Succeeded, string Message)> UpdateMyProfileAsync(
            CustomerProfileViewModel model,
            long customerId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == customerId && u.Role == UserRoles.Customer);

            if (user == null)
            {
                return (false, "Không tìm thấy tài khoản khách hàng.");
            }

            if (user.Status != UserStatuses.Active)
            {
                return (false, "Tài khoản không ở trạng thái hoạt động.");
            }

            var fullName = model.FullName.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                return (false, "Vui lòng nhập họ tên.");
            }

            user.FullName = fullName;
            user.PhoneNumber = NormalizeNullableText(model.PhoneNumber);
            user.IdentityNumber = NormalizeNullableText(model.IdentityNumber);
            user.Address = NormalizeNullableText(model.Address);
            user.UpdatedAt = DateTime.Now;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = customerId,
                Action = "UpdateProfile",
                EntityName = "User",
                EntityId = user.Id,
                Description = $"Customer cập nhật hồ sơ cá nhân: {user.Email}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return (true, "Cập nhật hồ sơ thành công.");
        }

        private static string? NormalizeNullableText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}
