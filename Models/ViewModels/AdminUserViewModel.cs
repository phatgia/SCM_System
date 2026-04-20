using SCM_System.Models;

namespace SCM_System.Models.ViewModels
{
    public class AdminUserViewModel
    {
        public List<UserViewModel> Users { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
    }

    public class UserViewModel
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "Đang hoạt động"; // Chờ duyệt, Đang hoạt động, Đã khóa
    }
}
