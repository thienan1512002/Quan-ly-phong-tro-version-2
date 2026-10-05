using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models
{
    public enum UserNotificationType
    {
        Chung = 0,
        HopDong = 1,
        CatDienNuoc = 2
    }

    public class UserNotification
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public AppUser? User { get; set; }

        public UserNotificationType Type { get; set; } = UserNotificationType.Chung;

        [Required, MaxLength(160)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public DateTime? EventDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReadAt { get; set; }
    }
}
