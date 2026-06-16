namespace HotelManagement.ViewModels.Admin
{
    public class RevenueReportViewModel
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public decimal RangeRevenue { get; set; }

        public decimal TodayRevenue { get; set; }

        public decimal MonthRevenue { get; set; }

        public decimal TotalRevenue { get; set; }

        public int PaymentCount { get; set; }

        public int BookingCount { get; set; }

        public int TotalInvoices { get; set; }

        public int PaidInvoices { get; set; }

        public int UnpaidInvoices { get; set; }

        public int PartiallyPaidInvoices { get; set; }

        public int PendingBookings { get; set; }

        public int ConfirmedBookings { get; set; }

        public int CheckedInBookings { get; set; }

        public int CheckedOutBookings { get; set; }

        public int CancelledBookings { get; set; }

        public List<RevenueByDayViewModel> RevenueByDays { get; set; } = new();

        public List<RevenueByPaymentMethodViewModel> RevenueByPaymentMethods { get; set; } = new();
    }

    public class RevenueByDayViewModel
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public int PaymentCount { get; set; }
    }

    public class RevenueByPaymentMethodViewModel
    {
        public string PaymentMethod { get; set; } = string.Empty;

        public decimal Revenue { get; set; }

        public int PaymentCount { get; set; }
    }
}
