using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Receptionist;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services.Receptionist
{
    public class ReceptionistDashboardService
    {
        private readonly HotelDbContext _context;

        public ReceptionistDashboardService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<ReceptionistDashboardViewModel> GetDashboardAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var pendingBookingsCount = await _context.Bookings
                .AsNoTracking()
                .CountAsync(b => b.Status == BookingStatuses.Pending);

            var confirmedBookingsCount = await _context.Bookings
                .AsNoTracking()
                .CountAsync(b => b.Status == BookingStatuses.Confirmed);

            var todayCheckInsCount = await _context.Bookings
                .AsNoTracking()
                .CountAsync(b =>
                    b.Status == BookingStatuses.Confirmed
                    && b.CheckInDate >= today
                    && b.CheckInDate < tomorrow
                );

            var todayCheckOutsCount = await _context.Bookings
                .AsNoTracking()
                .CountAsync(b =>
                    b.Status == BookingStatuses.CheckedIn
                    && b.CheckOutDate >= today
                    && b.CheckOutDate < tomorrow
                );

            var todayNewBookingsCount = await _context.Bookings
                .AsNoTracking()
                .CountAsync(b =>
                    b.CreatedAt >= today
                    && b.CreatedAt < tomorrow
                );

            var availableRoomsCount = await _context.Rooms
                .AsNoTracking()
                .CountAsync(r => r.Status == RoomStatuses.Available);

            var occupiedRoomsCount = await _context.Rooms
                .AsNoTracking()
                .CountAsync(r => r.Status == RoomStatuses.Occupied);

            var cleaningRoomsCount = await _context.Rooms
                .AsNoTracking()
                .CountAsync(r => r.Status == RoomStatuses.Cleaning);

            var maintenanceRoomsCount = await _context.Rooms
                .AsNoTracking()
                .CountAsync(r => r.Status == RoomStatuses.Maintenance);

            var pendingBookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomType)
                .Where(b => b.Status == BookingStatuses.Pending)
                .OrderBy(b => b.CheckInDate)
                .ThenBy(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();

            var todayCheckIns = await _context.Bookings
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
                .Take(5)
                .ToListAsync();

            var todayCheckOuts = await _context.Bookings
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
                .Take(5)
                .ToListAsync();

            var roomStatusSummaries = await _context.Rooms
                .AsNoTracking()
                .GroupBy(r => r.Status)
                .Select(g => new ReceptionistRoomStatusSummaryViewModel
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Status)
                .ToListAsync();

            return new ReceptionistDashboardViewModel
            {
                PendingBookingsCount = pendingBookingsCount,
                ConfirmedBookingsCount = confirmedBookingsCount,
                TodayCheckInsCount = todayCheckInsCount,
                TodayCheckOutsCount = todayCheckOutsCount,
                TodayNewBookingsCount = todayNewBookingsCount,
                AvailableRoomsCount = availableRoomsCount,
                OccupiedRoomsCount = occupiedRoomsCount,
                CleaningRoomsCount = cleaningRoomsCount,
                MaintenanceRoomsCount = maintenanceRoomsCount,
                PendingBookings = pendingBookings.Select(MapBookingItem).ToList(),
                TodayCheckIns = todayCheckIns.Select(MapBookingItem).ToList(),
                TodayCheckOuts = todayCheckOuts.Select(MapBookingItem).ToList(),
                RoomStatusSummaries = roomStatusSummaries
            };
        }

        private static ReceptionistDashboardBookingItemViewModel MapBookingItem(Booking booking)
        {
            return new ReceptionistDashboardBookingItemViewModel
            {
                BookingId = booking.Id,
                BookingCode = booking.BookingCode,
                CustomerName = booking.Customer?.FullName ?? "Không xác định",
                CustomerPhoneNumber = booking.Customer?.PhoneNumber,
                RoomNumber = booking.Room?.RoomNumber ?? "Không xác định",
                RoomTypeName = booking.Room?.RoomType?.Name ?? "Không xác định",
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Status = booking.Status,
                TotalAmount = booking.TotalAmount,
                CreatedAt = booking.CreatedAt
            };
        }
    }
}
