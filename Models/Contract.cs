using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// Entity đại diện cho hợp đồng thuê phòng
    /// </summary>
    public class Contract
    {
        public int Id { get; set; }

        // Khóa ngoại đến phòng
        [Required]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        // Khóa ngoại đến người thuê
        [Required]
        public int UserId { get; set; }
        public AppUser? User { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá thuê")]
        public decimal MonthlyRent { get; set; }

        [MaxLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tiền cọ")]
        public decimal? Deposit { get; set; }

        [Display(Name = "Ngày thanh toán hàng tháng")]
        public int? PaymentDay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Phí quản lý")]
        public decimal? ManagementFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Phí gửi xe")]
        public decimal? ParkingFee { get; set; }

        [Display(Name = "Chỉ số điện đầu kỳ")]
        public int? ElectricMeter { get; set; }

        [Display(Name = "Chỉ số nước đầu kỳ")]
        public int? WaterMeter { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Hợp đồng còn hiệu lực hay không
        public bool IsActive { get; set; } = true;
    }
}
