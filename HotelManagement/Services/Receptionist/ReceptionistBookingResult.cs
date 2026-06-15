namespace HotelManagement.Services.Receptionist
{
    public class ReceptionistBookingResult
    {
        public bool Succeeded { get; set; }

        public string Message { get; set; } = string.Empty;

        public long? BookingId { get; set; }

        public string? BookingCode { get; set; }

        public static ReceptionistBookingResult Success(long bookingId, string bookingCode, string message)
        {
            return new ReceptionistBookingResult
            {
                Succeeded = true,
                BookingId = bookingId,
                BookingCode = bookingCode,
                Message = message
            };
        }

        public static ReceptionistBookingResult Failure(string message)
        {
            return new ReceptionistBookingResult
            {
                Succeeded = false,
                Message = message
            };
        }
    }
}
