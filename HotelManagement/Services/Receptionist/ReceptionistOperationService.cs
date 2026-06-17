using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.ViewModels.Receptionist;
using HotelManagement.Constants;

namespace HotelManagement.Services.Receptionist
{
    public class ReceptionistOperationService
    {
        private readonly HotelDbContext _context;

        public ReceptionistOperationService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<RoomStatusBoardViewModel> GetRoomStatusBoardAsync(DateTime? targetDate)
        {
            var date = targetDate?.Date ?? DateTime.Today;

            var model = new RoomStatusBoardViewModel
            {
                TargetDate = date
            };

            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status != RoomStatuses.Maintenance && r.RoomType != null && r.RoomType.Status == "Active")
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();

            var activeStatuses = new string[]
            {
                BookingStatuses.Pending,
                BookingStatuses.Confirmed,
                BookingStatuses.CheckedIn
            };

            // Get all bookings that overlap with the target date
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Where(b => activeStatuses.Contains(b.Status)
                            && b.CheckInDate.Date <= date
                            && b.CheckOutDate.Date > date)
                .ToListAsync();

            var groupedByFloor = rooms.GroupBy(r => r.Floor ?? 0).OrderBy(g => g.Key);

            foreach (var group in groupedByFloor)
            {
                var floorModel = new RoomStatusFloorViewModel { Floor = group.Key };

                foreach (var room in group)
                {
                    var activeBooking = bookings.FirstOrDefault(b => b.RoomId == room.Id);

                    var item = new RoomStatusItemViewModel
                    {
                        RoomId = room.Id,
                        RoomNumber = room.RoomNumber,
                        RoomTypeName = room.RoomType?.Name ?? "N/A"
                    };

                    if (activeBooking != null)
                    {
                        if (activeBooking.Status == BookingStatuses.CheckedIn)
                        {
                            item.Status = "Đang ở";
                            item.StatusColorClass = "bg-primary text-white"; // Blue
                        }
                        else
                        {
                            item.Status = "Đã đặt";
                            item.StatusColorClass = "bg-warning text-dark"; // Yellow
                        }

                        item.BookingId = activeBooking.Id;
                        item.CustomerName = activeBooking.Customer?.FullName ?? "N/A";
                        item.CheckInDate = activeBooking.CheckInDate;
                        item.CheckOutDate = activeBooking.CheckOutDate;
                    }
                    else
                    {
                        item.Status = "Trống";
                        item.StatusColorClass = "bg-success text-white"; // Green
                    }

                    floorModel.Rooms.Add(item);
                }

                model.Floors.Add(floorModel);
            }

            return model;
        }
    }
}
