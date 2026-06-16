namespace HotelManagement.Services.Receptionist
{
    public class ReceptionistInvoiceResult
    {
        public bool Succeeded { get; set; }

        public string Message { get; set; } = string.Empty;

        public long? InvoiceId { get; set; }

        public string? InvoiceCode { get; set; }

        public static ReceptionistInvoiceResult Success(long invoiceId, string invoiceCode, string message)
        {
            return new ReceptionistInvoiceResult
            {
                Succeeded = true,
                InvoiceId = invoiceId,
                InvoiceCode = invoiceCode,
                Message = message
            };
        }

        public static ReceptionistInvoiceResult Failure(string message)
        {
            return new ReceptionistInvoiceResult
            {
                Succeeded = false,
                Message = message
            };
        }
    }
}
