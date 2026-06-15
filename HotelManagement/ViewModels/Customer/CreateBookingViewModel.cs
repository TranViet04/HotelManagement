using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels.Customer
{
    public class CreateBookingViewModel : IValidatableObject
    {
        public long RoomId { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public long RoomTypeId { get; set; }

        public string RoomTypeName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal PricePerNight { get; set; }

        public int Capacity { get; set; }

        public string? BedType { get; set; }

        public string? ThumbnailUrl { get; set; }

        public int? Floor { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

        [Range(1, 20, ErrorMessage = "Số người lớn phải từ 1 đến 20")]
        public int Adults { get; set; } = 1;

        [Range(0, 20, ErrorMessage = "Số trẻ em phải từ 0 đến 20")]
        public int Children { get; set; } = 0;

        [MaxLength(1000, ErrorMessage = "Yêu cầu đặc biệt tối đa 1000 ký tự")]
        public string? SpecialRequest { get; set; }

        public int Nights { get; set; }

        public decimal TotalRoomAmount { get; set; }

        public bool IsAvailableForSelectedDates { get; set; }

        public string? AvailabilityMessage { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RoomId <= 0)
            {
                yield return new ValidationResult(
                    "Phòng không hợp lệ",
                    new[] { nameof(RoomId) }
                );
            }

            if (CheckInDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Ngày nhận phòng không được nhỏ hơn hôm nay",
                    new[] { nameof(CheckInDate) }
                );
            }

            if (CheckOutDate.Date <= CheckInDate.Date)
            {
                yield return new ValidationResult(
                    "Ngày trả phòng phải lớn hơn ngày nhận phòng",
                    new[] { nameof(CheckOutDate) }
                );
            }

            if (Adults + Children > Capacity && Capacity > 0)
            {
                yield return new ValidationResult(
                    "Số lượng khách vượt quá sức chứa của phòng",
                    new[] { nameof(Adults), nameof(Children) }
                );
            }
        }
    }
}
