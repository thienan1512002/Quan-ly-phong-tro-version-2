using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Services
{
    /// <summary>
    /// Service xử lý logic yêu cầu thuê phòng
    /// </summary>
    public class RentalRequestService
    {
        private readonly AppDbContext _context;

        public RentalRequestService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả yêu cầu (Admin dùng)
        /// </summary>
        public async Task<List<RentalRequest>> GetAllAsync()
        {
            return await _context.RentalRequests
                .Include(r => r.Room)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy yêu cầu của một user cụ thể
        /// </summary>
        public async Task<List<RentalRequest>> GetByUserAsync(int userId)
        {
            return await _context.RentalRequests
                .Include(r => r.Room)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy số yêu cầu đang chờ duyệt
        /// </summary>
        public async Task<int> GetPendingCountAsync(int? userId = null)
        {
            var query = _context.RentalRequests
                .Where(r => r.Status == RequestStatus.ChoDuyet);

            if (userId.HasValue)
                query = query.Where(r => r.UserId == userId.Value);

            return await query.CountAsync();
        }

        /// <summary>
        /// Tạo yêu cầu thuê mới
        /// </summary>
        public async Task<bool> CreateAsync(int roomId, int userId, string? note,
            DateTime? desiredStartDate = null, DateTime? desiredEndDate = null,
            decimal? deposit = null)
        {
            // Kiểm tra phòng có trống không
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null || room.Status != RoomStatus.Trong)
                return false;

            // Kiểm tra user đã gửi yêu cầu cho phòng này chưa (đang chờ duyệt)
            var existing = await _context.RentalRequests
                .AnyAsync(r => r.RoomId == roomId && r.UserId == userId && r.Status == RequestStatus.ChoDuyet);

            if (existing) return false;

            var request = new RentalRequest
            {
                RoomId = roomId,
                UserId = userId,
                Note = note,
                DesiredStartDate = desiredStartDate,
                DesiredEndDate = desiredEndDate,
                Deposit = deposit,
                Status = RequestStatus.ChoDuyet,
                CreatedAt = DateTime.Now
            };

            _context.RentalRequests.Add(request);

            // Đặt phòng vào trạng thái Đang chờ duyệt
            room.Status = RoomStatus.DangChoDuyet;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Lấy yêu cầu theo id (kèm Room và User)
        /// </summary>
        public async Task<RentalRequest?> GetByIdAsync(int id)
        {
            return await _context.RentalRequests
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// Admin duyệt yêu cầu
        /// </summary>
        public async Task<bool> ApproveAsync(int requestId, string? adminNote)
        {
            var request = await _context.RentalRequests.FindAsync(requestId);
            if (request == null) return false;

            request.Status = RequestStatus.DaDuyet;
            request.AdminNote = adminNote;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Admin từ chối yêu cầu
        /// </summary>
        public async Task<bool> RejectAsync(int requestId, string? adminNote)
        {
            var request = await _context.RentalRequests
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return false;

            request.Status = RequestStatus.TuChoi;
            request.AdminNote = adminNote;

            // Trả phòng về trống nếu đang ở trạng thái chờ duyệt
            if (request.Room != null && request.Room.Status == RoomStatus.DangChoDuyet)
                request.Room.Status = RoomStatus.Trong;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAsync(int requestId, int userId)
        {
            var request = await _context.RentalRequests
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            // Chỉ cho hủy nếu đúng chủ sở hữu và đang ở trạng thái Chờ duyệt
            if (request == null || request.UserId != userId || request.Status != RequestStatus.ChoDuyet)
                return false;

            request.Status = RequestStatus.DaHuy;

            // Trả phòng về trống
            if (request.Room != null && request.Room.Status == RoomStatus.DangChoDuyet)
                request.Room.Status = RoomStatus.Trong;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(int choDuyet, int daDuyet, int tuChoi, int daHuy)> GetStatusCountsAsync()
        {
            var list = await _context.RentalRequests
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            return (
                list.FirstOrDefault(c => c.Status == RequestStatus.ChoDuyet)?.Count ?? 0,
                list.FirstOrDefault(c => c.Status == RequestStatus.DaDuyet)?.Count  ?? 0,
                list.FirstOrDefault(c => c.Status == RequestStatus.TuChoi)?.Count   ?? 0,
                list.FirstOrDefault(c => c.Status == RequestStatus.DaHuy)?.Count    ?? 0
            );
        }
    }
}
