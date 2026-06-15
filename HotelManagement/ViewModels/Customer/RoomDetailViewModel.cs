namespace HotelManagement.ViewModels.Customer
{
    public class RoomDetailViewModel
    {
        public long RoomTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Capacity { get; set; }

        public string? BedType { get; set; }

        public string? ThumbnailUrl { get; set; }

        public int TotalRoomCount { get; set; }

        public int AvailableRoomCount { get; set; }

        public DateTime? CheckInDate { get; set; }

        public DateTime? CheckOutDate { get; set; }

        public int Adults { get; set; } = 1;

        public int Children { get; set; } = 0;

        public bool HasSearchCriteria { get; set; }

        public bool HasValidSearchCriteria { get; set; }

        public string? SearchMessage { get; set; }

        public List<AvailableRoomViewModel> AvailableRooms { get; set; } = new();
    }
}
