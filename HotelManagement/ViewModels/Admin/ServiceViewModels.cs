using System.ComponentModel.DataAnnotations;

namespace HotelManagement.ViewModels.Admin
{
    public class ServiceListItemViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? Unit { get; set; }

        public decimal Price { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public class ServiceFormViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ")]
        [MaxLength(150, ErrorMessage = "Tên dịch vụ không được vượt quá 150 ký tự")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Danh mục không được vượt quá 100 ký tự")]
        public string? Category { get; set; }

        [MaxLength(50, ErrorMessage = "Đơn vị không được vượt quá 50 ký tự")]
        public string? Unit { get; set; }

        [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá dịch vụ không hợp lệ")]
        public decimal Price { get; set; }

        [Required]
        public string Status { get; set; } = "Active";
    }
}
