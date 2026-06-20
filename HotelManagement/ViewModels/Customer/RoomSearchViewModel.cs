using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels.Customer
{
    public class RoomSearchViewModel : IValidatableObject
    {
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

        [Range(1, 20, ErrorMessage = "Số người lớn phải từ 1 đến 20")]
        public int Adults { get; set; } = 1;

        [Range(0, 20, ErrorMessage = "Số trẻ em phải từ 0 đến 20")]
        public int Children { get; set; } = 0;

        public bool HasSearched { get; set; }

        public List<AvailableRoomTypeViewModel> Results { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var today = DateTime.Today;

            if (CheckInDate.Date < today)
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

            if (Adults + Children <= 0)
            {
                yield return new ValidationResult(
                    "Tổng số khách phải lớn hơn 0",
                    new[] { nameof(Adults), nameof(Children) }
                );
            }
        }
    }
}
