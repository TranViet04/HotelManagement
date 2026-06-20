namespace HotelManagement.ViewModels.Admin
{
    public class ActivityLogViewModel
    {
        public long Id { get; set; }

        public string UserName { get; set; } = "System";

        public string? UserRole { get; set; }

        public string Action { get; set; } = string.Empty;

        public string? EntityName { get; set; }

        public long? EntityId { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
