using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// Trạng thái phòng trọ
    /// </summary>
    public enum RoomStatus
    {
        Trong = 0,          // Phòng trống
        DaThue = 1,         // Đã có người thuê
        DangChoDuyet = 2    // Đang có yêu cầu chờ duyệt
    }

    /// <summary>
    /// Entity đại diện cho phòng trọ
    /// </summary>
    public class Room
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên phòng không được để trống")]
        [MaxLength(100)]
        [Display(Name = "Tên phòng")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá thuê không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá thuê (VNĐ/tháng)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Diện tích không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Diện tích phải lớn hơn 0")]
        [Display(Name = "Diện tích (m²)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Area { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [MaxLength(200)]
        [Display(Name = "Địa chỉ (số nhà, tên đường)")]
        public string Address { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Phường / Quận")]
        public string? Ward { get; set; }

        [MaxLength(100)]
        [Display(Name = "Thành phố / Tỉnh")]
        public string? City { get; set; }

        [Display(Name = "Trạng thái")]
        public RoomStatus Status { get; set; } = RoomStatus.Trong;

        [MaxLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<RentalRequest> RentalRequests { get; set; } = new List<RentalRequest>();
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
        public ICollection<UtilityReading> UtilityReadings { get; set; } = new List<UtilityReading>();
    }
}
