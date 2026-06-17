namespace HotelManagement.Services.Customer
{
    public class CustomerBookingResult
    {
        public bool Succeeded { get; set; }

        public string Message { get; set; } = string.Empty;

        public long? BookingId { get; set; }

        public string? BookingCode { get; set; }

        public static CustomerBookingResult Success(long bookingId, string bookingCode, string? message = null)
        {
            return new CustomerBookingResult
            {
                Succeeded = true,
                BookingId = bookingId,
                BookingCode = bookingCode,
                Message = message ?? "Đặt phòng thành công"
            };
        }

        public static CustomerBookingResult Failure(string message)
        {
            return new CustomerBookingResult
            {
                Succeeded = false,
                Message = message
            };
        }
    }
}
