using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.ViewModels.Receptionist
{
    public class CreateWalkInBookingViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên khách hàng")]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20)]
        public string CustomerPhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(100)]
        public string? CustomerEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số CCCD/CMND")]
        [MaxLength(50)]
        public string CustomerIdentityNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phòng")]
        public long RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng")]
        public DateTime CheckInDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng")]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "Cash";

        [Range(1, 10)]
        public int Adults { get; set; } = 1;

        [Range(0, 10)]
        public int Children { get; set; } = 0;

        public decimal TotalAmount { get; set; }

        public List<SelectListItem> RoomOptions { get; set; } = new();
        public List<SelectListItem> PaymentMethodOptions { get; set; } = new()
        {
            new SelectListItem { Value = "Cash", Text = "Tiền mặt" },
            new SelectListItem { Value = "SePay", Text = "Chuyển khoản (SePay QR)" }
        };
    }
}
