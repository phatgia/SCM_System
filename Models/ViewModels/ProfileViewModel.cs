using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int UserID { get; set; }

        public string RoleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự.")]
        public string Username { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        public string? PhoneNumber { get; set; }

        // Mật khẩu hiện tại (dùng để xác nhận khi đổi thông tin hoặc đổi mật khẩu)
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        // Đổi mật khẩu
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string? ConfirmNewPassword { get; set; }
    }
}
