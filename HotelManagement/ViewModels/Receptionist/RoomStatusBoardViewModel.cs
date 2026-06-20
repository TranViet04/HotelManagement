using System;
using System.Collections.Generic;

namespace HotelManagement.ViewModels.Receptionist
{
    public class RoomStatusBoardViewModel
    {
        public DateTime TargetDate { get; set; }
        public List<RoomStatusFloorViewModel> Floors { get; set; } = new();
    }

    public class RoomStatusFloorViewModel
    {
        public int Floor { get; set; }
        public List<RoomStatusItemViewModel> Rooms { get; set; } = new();
    }

    public class RoomStatusItemViewModel
    {
        public long RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Trống, Đang ở, Đã đặt
        public string StatusColorClass { get; set; } = string.Empty;
        
        public long? BookingId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
    }
}
