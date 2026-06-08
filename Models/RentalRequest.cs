using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// Trạng thái yêu cầu thuê phòng
    /// </summary>
    public enum RequestStatus
    {
        ChoDuyet = 0,   // Đang chờ admin duyệt
        DaDuyet = 1,    // Đã được duyệt
        TuChoi = 2,     // Bị từ chối
        DaHuy = 3       // Người dùng tự hủy
    }

    /// <summary>
    /// Entity đại diện cho yêu cầu thuê phòng của người dùng
    /// </summary>
    public class RentalRequest
    {
        public int Id { get; set; }

        // Khóa ngoại đến phòng
        [Required]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        // Khóa ngoại đến người dùng
        [Required]
        public int UserId { get; set; }
        public AppUser? User { get; set; }

        [Display(Name = "Ghi chú")]
        [MaxLength(500)]
        public string? Note { get; set; }

        // Trạng thái yêu cầu
        [Display(Name = "Trạng thái")]
        public RequestStatus Status { get; set; } = RequestStatus.ChoDuyet;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Thời hạn thuê mong muốn (user chọn khi gửi yêu cầu)
        public DateTime? DesiredStartDate { get; set; }
        public DateTime? DesiredEndDate { get; set; }

        // Admin ghi chú khi duyệt/từ chối
        [MaxLength(500)]
        public string? AdminNote { get; set; }
    }
}
