using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Admin
{
    public class EmployeeManagementService
    {
        private readonly HotelDbContext _context;

        public EmployeeManagementService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeListItemViewModel>> GetReceptionistsAsync()
        {
            return await _context.Users
                .Where(u => u.Role == UserRoles.Receptionist)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new EmployeeListItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<EditEmployeeViewModel?> GetForEditAsync(long id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRoles.Receptionist);

            if (user == null)
            {
                return null;
            }

            return new EditEmployeeViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Status = user.Status
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

        public async Task<long> CreateReceptionistAsync(CreateEmployeeViewModel model)
        {
            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLower(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = UserRoles.Receptionist,
                Status = UserStatuses.Active,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user.Id;
        }

        public async Task<bool> UpdateReceptionistAsync(EditEmployeeViewModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == model.Id && u.Role == UserRoles.Receptionist);

            if (user == null)
            {
                return false;
            }

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim().ToLower();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.Status = model.Status;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LockAsync(long id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRoles.Receptionist);

            if (user == null)
            {
                return false;
            }

            user.Status = UserStatuses.Locked;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlockAsync(long id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRoles.Receptionist);

            if (user == null)
            {
                return false;
            }

            user.Status = UserStatuses.Active;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(long id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRoles.Receptionist);

            if (user == null)
            {
                return false;
            }

            user.Status = UserStatuses.Inactive;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
