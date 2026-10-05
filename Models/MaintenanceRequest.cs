using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models
{
    public enum MaintenanceStatus
    {
        Moi = 0,
        DangXuLy = 1,
        HoanThanh = 2,
        TuChoi = 3
    }

    public enum MaintenancePriority
    {
        Thap = 0,
        BinhThuong = 1,
        Cao = 2,
        KhanCap = 3
    }

    public class MaintenanceRequest
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public AppUser? User { get; set; }

        [Required]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public MaintenancePriority Priority { get; set; } = MaintenancePriority.BinhThuong;
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Moi;

        [MaxLength(1000)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }
}
