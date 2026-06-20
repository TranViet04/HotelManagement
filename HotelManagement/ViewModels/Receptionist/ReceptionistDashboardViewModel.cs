namespace HotelManagement.ViewModels.Receptionist
{
    public class ReceptionistDashboardViewModel
    {
        public int PendingBookingsCount { get; set; }

        public int ConfirmedBookingsCount { get; set; }

        public int TodayCheckInsCount { get; set; }

        public int TodayCheckOutsCount { get; set; }

        public int AvailableRoomsCount { get; set; }

        public int OccupiedRoomsCount { get; set; }

        public int CleaningRoomsCount { get; set; }

        public int MaintenanceRoomsCount { get; set; }

        public int TodayNewBookingsCount { get; set; }

        public List<ReceptionistDashboardBookingItemViewModel> PendingBookings { get; set; } = new();

        public List<ReceptionistDashboardBookingItemViewModel> TodayCheckIns { get; set; } = new();

        public List<ReceptionistDashboardBookingItemViewModel> TodayCheckOuts { get; set; } = new();

        public List<ReceptionistRoomStatusSummaryViewModel> RoomStatusSummaries { get; set; } = new();
    }

    public class ReceptionistDashboardBookingItemViewModel
    {
        public long BookingId { get; set; }

        public string BookingCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerPhoneNumber { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class ReceptionistRoomStatusSummaryViewModel
    {
        public string Status { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
