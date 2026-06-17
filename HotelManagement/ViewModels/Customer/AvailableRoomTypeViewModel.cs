namespace HotelManagement.ViewModels.Customer
{
    public class AvailableRoomTypeViewModel
    {
        public long RoomTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal PricePerNight { get; set; }

        public int Capacity { get; set; }

        public string? BedType { get; set; }

        public string? ThumbnailUrl { get; set; }

        public int AvailableRoomCount { get; set; }

        public int Nights { get; set; }

        public decimal TotalRoomAmount { get; set; }
    }
}
