namespace HotelManagement.Configuration
{
    public class SepaySettings
    {
        public string ApiKey { get; set; } = string.Empty;

        public string WebhookApiKey { get; set; } = string.Empty;

        public string BankId { get; set; } = string.Empty;

        public string BankAccount { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;

        public string BankName { get; set; } = "Ngân hàng";
    }
}
