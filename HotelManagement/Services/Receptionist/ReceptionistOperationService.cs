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
                .Where(r => r.Status != RoomStatuses.Inactive && r.RoomType != null && r.RoomType.Status == "Active")
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
                        RoomTypeName = room.RoomType?.Name ?? "N/A",
                        RoomStatus = room.Status
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
                        item.Status = GetRoomStatusText(room.Status);
                        item.StatusColorClass = GetRoomStatusColorClass(room.Status);
                    }

                    floorModel.Rooms.Add(item);
                }

                model.Floors.Add(floorModel);
            }

            return model;
        }

        public async Task<ReceptionistRoomStatusResult> UpdateRoomStatusAsync(long roomId, string status, long receptionistId)
        {
            status = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();

            if (!AllowedReceptionistRoomStatuses.Contains(status))
            {
                return ReceptionistRoomStatusResult.Failure("Trạng thái phòng không hợp lệ.");
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == roomId && r.Status != RoomStatuses.Inactive);

            if (room == null)
            {
                return ReceptionistRoomStatusResult.Failure("Không tìm thấy phòng.");
            }

            var hasCheckedInBooking = await _context.Bookings.AnyAsync(b =>
                b.RoomId == roomId && b.Status == BookingStatuses.CheckedIn);

            if (hasCheckedInBooking && status != RoomStatuses.Occupied)
            {
                return ReceptionistRoomStatusResult.Failure("Phòng đang có khách lưu trú, chỉ có thể giữ trạng thái Occupied.");
            }

            var now = DateTime.Now;
            var oldStatus = room.Status;
            room.Status = status;
            room.UpdatedAt = now;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = receptionistId,
                Action = "UpdateRoomStatus",
                EntityName = "Room",
                EntityId = room.Id,
                Description = $"Lễ tân cập nhật trạng thái phòng {room.RoomNumber}: {oldStatus} -> {status}",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return ReceptionistRoomStatusResult.Success($"Đã cập nhật trạng thái phòng {room.RoomNumber} thành công.");
        }

        private static readonly string[] AllowedReceptionistRoomStatuses =
        {
            RoomStatuses.Available,
            RoomStatuses.Occupied,
            RoomStatuses.Cleaning,
            RoomStatuses.Maintenance
        };

        private static string GetRoomStatusText(string status)
        {
            return status switch
            {
                RoomStatuses.Available => "Trống",
                RoomStatuses.Occupied => "Đang ở",
                RoomStatuses.Cleaning => "Đang dọn",
                RoomStatuses.Maintenance => "Bảo trì",
                _ => status
            };
        }

        private static string GetRoomStatusColorClass(string status)
        {
            return status switch
            {
                RoomStatuses.Available => "bg-success text-white",
                RoomStatuses.Occupied => "bg-primary text-white",
                RoomStatuses.Cleaning => "bg-info text-dark",
                RoomStatuses.Maintenance => "bg-danger text-white",
                _ => "bg-secondary text-white"
            };
        }
    }

    public class ReceptionistRoomStatusResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ReceptionistRoomStatusResult Success(string message)
        {
            return new ReceptionistRoomStatusResult
            {
                Succeeded = true,
                Message = message
            };
        }

        public static ReceptionistRoomStatusResult Failure(string message)
        {
            return new ReceptionistRoomStatusResult
            {
                Succeeded = false,
                Message = message
            };
        }
    }
}
