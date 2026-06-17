namespace HotelManagement.ViewModels.Public
{
    public class RoomTypeCarouselItemViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Capacity { get; set; }

        public string BedType { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = "/images/rooms/default-room.jpg";

        public int AvailableRoomCount { get; set; }
    }
}
