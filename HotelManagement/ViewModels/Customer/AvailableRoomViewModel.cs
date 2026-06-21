namespace HotelManagement.ViewModels.Customer
{
    public class AvailableRoomViewModel
    {
        public long RoomId { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public int? Floor { get; set; }

        public long RoomTypeId { get; set; }

        public string RoomTypeName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal PricePerNight { get; set; }

        public int Capacity { get; set; }

        public string? BedType { get; set; }

        public List<string> ImageUrls { get; set; } = new();

        public int Nights { get; set; }

        public decimal TotalRoomAmount { get; set; }
    }
}
