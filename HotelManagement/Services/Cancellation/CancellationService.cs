namespace HotelManagement.Services.Cancellation
{
    public class CancellationResult
    {
        public decimal RefundPercent { get; set; }

        public decimal RefundAmount { get; set; }

        public decimal CancellationFee { get; set; }

        public string PolicyDescription { get; set; } = string.Empty;

        public bool IsPaidBooking { get; set; }
    }

    public class CancellationService
    {
        public CancellationResult CalculateRefund(
            decimal totalAmount,
            DateTime checkInDate,
            DateTime cancelTime,
            DateTime? paidAt)
        {
            var isPaid = paidAt.HasValue;
            var paidAmount = isPaid ? totalAmount : 0m;

            if (!isPaid)
            {
                return new CancellationResult
                {
                    RefundPercent = 0,
                    RefundAmount = 0,
                    CancellationFee = 0,
                    PolicyDescription = "Đơn chưa thanh toán — không phát sinh hoàn tiền.",
                    IsPaidBooking = false
                };
            }

            if ((cancelTime - paidAt!.Value).TotalHours <= 24)
            {
                return new CancellationResult
                {
                    RefundPercent = 100,
                    RefundAmount = paidAmount,
                    CancellationFee = 0,
                    PolicyDescription = "Hoàn 100% — hủy trong vòng 24 giờ kể từ lúc thanh toán.",
                    IsPaidBooking = true
                };
            }

            var daysUntilCheckIn = (checkInDate.Date - cancelTime.Date).Days;

            if (daysUntilCheckIn > 7)
            {
                return new CancellationResult
                {
                    RefundPercent = 100,
                    RefundAmount = paidAmount,
                    CancellationFee = 0,
                    PolicyDescription = "Hoàn 100% — hủy trước ngày nhận phòng hơn 7 ngày.",
                    IsPaidBooking = true
                };
            }

            if (daysUntilCheckIn >= 4 && daysUntilCheckIn <= 7)
            {
                var refund = Math.Round(paidAmount * 0.5m, 0);
                return new CancellationResult
                {
                    RefundPercent = 50,
                    RefundAmount = refund,
                    CancellationFee = paidAmount - refund,
                    PolicyDescription = "Hoàn 50% — hủy từ 4 đến 7 ngày trước ngày nhận phòng.",
                    IsPaidBooking = true
                };
            }

            return new CancellationResult
            {
                RefundPercent = 0,
                RefundAmount = 0,
                CancellationFee = paidAmount,
                PolicyDescription = "Không hoàn tiền — hủy trong vòng 3 ngày trước ngày nhận phòng.",
                IsPaidBooking = true
            };
        }
    }
}
