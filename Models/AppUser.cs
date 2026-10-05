using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// Entity đại diện cho người dùng trong hệ thống
    /// </summary>
    public class AppUser
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        // Role: "Admin" hoặc "User"
        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<RentalRequest> RentalRequests { get; set; } = new List<RentalRequest>();
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
        public ICollection<UserNotification> Notifications { get; set; } = new List<UserNotification>();
    }
}
