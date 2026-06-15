using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Auth;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class AuthService
    {
        private readonly HotelDbContext _context;

        public AuthService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<User?> ValidateLoginAsync(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Status == UserStatuses.Active);

            if (user == null)
            {
                return null;
            }

            var isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isValidPassword)
            {
                return null;
            }

            return user;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<User> RegisterCustomerAsync(RegisterViewModel model)
        {
            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                IdentityNumber = model.IdentityNumber,
                Address = model.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = UserRoles.Customer,
                Status = UserStatuses.Active,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}