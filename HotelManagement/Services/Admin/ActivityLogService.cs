using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Admin
{
    public class ActivityLogService
    {
        private readonly HotelDbContext _context;

        public ActivityLogService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<ActivityLogViewModel>> GetLogsAsync(
            string? keyword,
            string? action,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _context.ActivityLogs
                .AsNoTracking()
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();

                query = query.Where(a =>
                    a.Action.Contains(normalizedKeyword) ||
                    (a.EntityName != null && a.EntityName.Contains(normalizedKeyword)) ||
                    (a.Description != null && a.Description.Contains(normalizedKeyword)) ||
                    (a.User != null && a.User.FullName.Contains(normalizedKeyword)));
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(a => a.CreatedAt >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(a => a.CreatedAt < to);
            }

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(200)
                .Select(a => new ActivityLogViewModel
                {
                    Id = a.Id,
                    UserName = a.User != null ? a.User.FullName : "System",
                    UserRole = a.User != null ? a.User.Role : null,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
        }

        public async Task AddAsync(
            long? userId,
            string action,
            string entityName,
            long? entityId,
            string description)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
