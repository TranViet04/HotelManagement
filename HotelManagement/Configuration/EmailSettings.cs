namespace HotelManagement.Configuration
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";

        public int SmtpPort { get; set; } = 587;

        public string SenderName { get; set; } = "Hotel Management";

        public string SenderEmail { get; set; } = string.Empty;

        public string AppPassword { get; set; } = string.Empty;
    }
}
