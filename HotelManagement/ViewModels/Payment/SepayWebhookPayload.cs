using System.Text.Json.Serialization;

namespace HotelManagement.ViewModels.Payment
{
    public class SepayWebhookPayload
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("gateway")]
        public string? Gateway { get; set; }

        [JsonPropertyName("transactionDate")]
        public string? TransactionDate { get; set; }

        [JsonPropertyName("accountNumber")]
        public string? AccountNumber { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("transferType")]
        public string? TransferType { get; set; }

        [JsonPropertyName("transferAmount")]
        public decimal TransferAmount { get; set; }

        [JsonPropertyName("referenceCode")]
        public string? ReferenceCode { get; set; }
    }
}
