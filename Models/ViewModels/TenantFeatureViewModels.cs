using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuanLyPhongTro.Models.ViewModels
{
    public class MaintenanceCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [MaxLength(120)]
        [Display(Name = "Sự cố cần sửa")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn hạng mục")]
        [Display(Name = "Hạng mục")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng mô tả sự cố")]
        [MaxLength(1000)]
        [Display(Name = "Mô tả chi tiết")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Mức độ ưu tiên")]
        public MaintenancePriority Priority { get; set; } = MaintenancePriority.BinhThuong;
    }

    public class MaintenanceIndexViewModel
    {
        public bool IsAdmin { get; set; }
        public Contract? ActiveContract { get; set; }
        public MaintenanceCreateViewModel CreateModel { get; set; } = new();
        public List<MaintenanceRequest> Requests { get; set; } = new();
    }

    public class NotificationCreateViewModel
    {
        [Display(Name = "Người nhận")]
        public int? TargetUserId { get; set; }

        [Display(Name = "Loại thông báo")]
        public UserNotificationType Type { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [MaxLength(160)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [MaxLength(2000)]
        [Display(Name = "Nội dung")]
        public string Content { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        [Display(Name = "Thời gian áp dụng / dự kiến")]
        public DateTime? EventDate { get; set; }
    }

    public class NotificationIndexViewModel
    {
        public bool IsAdmin { get; set; }
        public NotificationCreateViewModel CreateModel { get; set; } = new();
        public List<SelectListItem> Users { get; set; } = new();
        public List<UserNotification> Notifications { get; set; } = new();
    }

    public class UtilityReadingInputViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng")]
        [Display(Name = "Phòng")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tháng")]
        [DataType(DataType.Date)]
        [Display(Name = "Tháng ghi nhận")]
        public DateTime BillingMonth { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);

        [Display(Name = "Chỉ số điện cũ")]
        [Range(typeof(decimal), "0", "999999999")]
        public decimal PreviousElectricityMeter { get; set; }

        [Display(Name = "Chỉ số điện mới")]
        [Range(typeof(decimal), "0", "999999999")]
        public decimal CurrentElectricityMeter { get; set; }

        [Display(Name = "Đơn giá điện (đ/kWh)")]
        [Range(typeof(decimal), "0", "9999999")]
        public decimal ElectricityUnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số điện không được âm")]
        [Display(Name = "Điện tiêu thụ (kWh)")]
        public decimal ElectricityUsage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số nước không được âm")]
        [Display(Name = "Nước tiêu thụ (m³)")]
        public decimal WaterUsage { get; set; }

        [MaxLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }

    public class UtilityIndexViewModel
    {
        public bool IsAdmin { get; set; }
        public int? SelectedRoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public List<SelectListItem> Rooms { get; set; } = new();
        public UtilityReadingInputViewModel Input { get; set; } = new();
        public List<UtilityReading> Readings { get; set; } = new();
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ElectricityData { get; set; } = new();
        public List<decimal> WaterData { get; set; } = new();
    }
}
