namespace HotelManagement.ViewModels.Customer
{
    public class RoomListViewModel
    {
        public bool HasAvailabilitySearch { get; set; }

        public DateTime CheckInDate { get; set; } = DateTime.Today;

        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

        public int Adults { get; set; } = 1;

        public int Children { get; set; } = 0;

        public List<RoomCardViewModel> Rooms { get; set; } = new();
    }
}
