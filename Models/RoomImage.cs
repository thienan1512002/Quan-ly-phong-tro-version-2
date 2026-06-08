using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models
{
    public class RoomImage
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        [Required]
        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;  // tên file trong wwwroot/images/rooms/

        public bool IsMain { get; set; } = false;             // ảnh đại diện

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Navigation
        public Room? Room { get; set; }
    }
}
