using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho form đăng ký tài khoản
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MaxLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel cho form đăng nhập
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// ViewModel cho Dashboard - hiển thị số liệu tổng quan
    /// </summary>
    public class DashboardViewModel
    {
        // Admin stats
        public int TotalRooms { get; set; }
        public int EmptyRooms { get; set; }
        public int WaitingRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int PendingRequests { get; set; }
        public int ActiveContracts { get; set; }

        // Admin chart: yêu cầu theo trạng thái
        public int ReqChoDuyet { get; set; }
        public int ReqDaDuyet { get; set; }
        public int ReqTuChoi { get; set; }
        public int ReqDaHuy { get; set; }

        // Admin chart: hợp đồng sắp hết hạn
        public int ContractsExpiring30 { get; set; }
        public int ContractsExpiring60 { get; set; }
        public int ContractsExpiring90 { get; set; }

        // User-specific data
        public bool IsUser { get; set; }
        public QuanLyPhongTro.Models.Contract? UserActiveContract { get; set; }
        public List<QuanLyPhongTro.Models.RentalRequest> UserRecentRequests { get; set; } = new();
    }

    /// <summary>
    /// ViewModel dùng cho form tìm kiếm phòng
    /// </summary>
    public class RoomSearchViewModel
    {
        [Display(Name = "Giá tối thiểu")]
        public decimal? MinPrice { get; set; }

        [Display(Name = "Giá tối đa")]
        public decimal? MaxPrice { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }
    }

    /// <summary>
    /// ViewModel cho form tạo/sửa hợp đồng
    /// </summary>
    public class ContractCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng")]
        [Display(Name = "Phòng")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn người thuê")]
        [Display(Name = "Người thuê")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(6);

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Display(Name = "Tiền cọ")]
        public decimal? Deposit { get; set; }

        [Display(Name = "Ngày thanh toán hàng tháng (1-28)")]
        public int? PaymentDay { get; set; }

        [Display(Name = "Phí quản lý")]
        public decimal? ManagementFee { get; set; }

        [Display(Name = "Phí gửi xe")]
        public decimal? ParkingFee { get; set; }

        [Display(Name = "Chỉ số điện đầu kỳ")]
        public int? ElectricMeter { get; set; }

        [Display(Name = "Chỉ số nước đầu kỳ")]
        public int? WaterMeter { get; set; }
    }

    public class ContractRenewViewModel
    {
        [Required]
        public int ContractId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc mới")]
        [Display(Name = "Ngày kết thúc mới")]
        [DataType(DataType.Date)]
        public DateTime NewEndDate { get; set; }

        [Display(Name = "Giá thuê mới")]
        public decimal? MonthlyRent { get; set; }

        [Display(Name = "Ghi chú gia hạn")]
        [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
        public string? Note { get; set; }
    }
}
