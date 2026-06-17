using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Admin
{
    public class AdminDashboardService
    {
        private readonly HotelDbContext _context;

        public AdminDashboardService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardViewModel> GetDashboardAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);

            var model = new AdminDashboardViewModel
            {
                TotalRoomTypes = await _context.RoomTypes.CountAsync(),

                TotalRooms = await _context.Rooms.CountAsync(),

                AvailableRooms = await _context.Rooms
                    .CountAsync(r => r.Status == RoomStatuses.Available),

                OccupiedRooms = await _context.Rooms
                    .CountAsync(r => r.Status == RoomStatuses.Occupied),

                MaintenanceRooms = await _context.Rooms
                    .CountAsync(r => r.Status == RoomStatuses.Maintenance),

                TotalServices = await _context.Services.CountAsync(),

                TotalBookings = await _context.Bookings.CountAsync(),

                TodayBookings = await _context.Bookings
                    .CountAsync(b => b.CreatedAt >= today && b.CreatedAt < tomorrow),

                TodayRevenue = await _context.Payments
                    .Where(p => p.Status == PaymentStatuses.Paid
                        && p.PaidAt.HasValue
                        && p.PaidAt >= today
                        && p.PaidAt < tomorrow)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,

                MonthRevenue = await _context.Payments
                    .Where(p => p.Status == PaymentStatuses.Paid
                        && p.PaidAt.HasValue
                        && p.PaidAt >= firstDayOfMonth
                        && p.PaidAt < firstDayOfNextMonth)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0
            };

            return model;
        }
    }
}
