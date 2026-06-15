namespace HotelManagement.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalRoomTypes { get; set; }

        public int TotalRooms { get; set; }

        public int AvailableRooms { get; set; }

        public int OccupiedRooms { get; set; }

        public int MaintenanceRooms { get; set; }

        public int TotalServices { get; set; }

        public int TotalBookings { get; set; }

        public int TodayBookings { get; set; }

        public decimal TodayRevenue { get; set; }

        public decimal MonthRevenue { get; set; }
    }
}
