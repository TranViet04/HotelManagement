using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels.Customer
{
    public class SelectServicesViewModel : IValidatableObject
    {
        public long RoomId { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string RoomTypeName { get; set; } = string.Empty;

        public decimal PricePerNight { get; set; }

        public int Capacity { get; set; }

        public string? ThumbnailUrl { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        public int Adults { get; set; } = 1;

        public int Children { get; set; } = 0;

        public string? SpecialRequest { get; set; }

        public int Nights { get; set; }

        public decimal TotalRoomAmount { get; set; }

        public List<ServiceSelectionItemViewModel> AvailableServices { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RoomId <= 0)
            {
                yield return new ValidationResult("Phòng không hợp lệ", new[] { nameof(RoomId) });
            }

            if (CheckOutDate.Date <= CheckInDate.Date)
            {
                yield return new ValidationResult(
                    "Ngày trả phòng phải lớn hơn ngày nhận phòng",
                    new[] { nameof(CheckOutDate) });
            }
        }
    }

    public class ServiceSelectionItemViewModel
    {
        public long ServiceId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? Unit { get; set; }

        public decimal Price { get; set; }

        public bool IsSelected { get; set; }

        public int Quantity { get; set; } = 1;
    }
}
