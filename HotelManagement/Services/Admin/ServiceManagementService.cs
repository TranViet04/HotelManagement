using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Admin
{
    public class ServiceManagementService
    {
        private readonly HotelDbContext _context;

        public ServiceManagementService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<ServiceListItemViewModel>> GetAllAsync()
        {
            return await _context.Services
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ServiceListItemViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Category = s.Category,
                    Unit = s.Unit,
                    Price = s.Price,
                    Status = s.Status
                })
                .ToListAsync();
        }

        public async Task<ServiceFormViewModel?> GetForEditAsync(long id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                return null;
            }

            return new ServiceFormViewModel
            {
                Id = service.Id,
                Name = service.Name,
                Category = service.Category,
                Unit = service.Unit,
                Price = service.Price,
                Status = service.Status
            };
        }

        public async Task<bool> IsNameExistsAsync(string name, long? ignoreId = null)
        {
            var normalizedName = name.Trim();
            var query = _context.Services.AsQueryable();

            if (ignoreId.HasValue)
            {
                query = query.Where(s => s.Id != ignoreId.Value);
            }

            return await query.AnyAsync(s => s.Name == normalizedName);
        }

        public async Task<long> CreateAsync(ServiceFormViewModel model)
        {
            var service = new Service
            {
                Name = model.Name.Trim(),
                Category = model.Category?.Trim(),
                Unit = model.Unit?.Trim(),
                Price = model.Price,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return service.Id;
        }

        public async Task<bool> UpdateAsync(ServiceFormViewModel model)
        {
            var service = await _context.Services.FindAsync(model.Id);

            if (service == null)
            {
                return false;
            }

            service.Name = model.Name.Trim();
            service.Category = model.Category?.Trim();
            service.Unit = model.Unit?.Trim();
            service.Price = model.Price;
            service.Status = model.Status;
            service.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(long id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                return false;
            }

            service.Status = "Inactive";
            service.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
