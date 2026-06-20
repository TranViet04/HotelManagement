using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels.Admin
{
    public class EmployeeListItemViewModel
    {
        public long Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    public class CreateEmployeeViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class EditEmployeeViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        public string Status { get; set; } = "Active";
    }
}
