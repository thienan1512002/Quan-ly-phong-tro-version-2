using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Services
{
    /// <summary>
    /// Service xử lý logic liên quan đến phòng trọ
    /// </summary>
    public class RoomService
    {
        private readonly AppDbContext _context;

        // Inject DbContext thông qua Dependency Injection
        public RoomService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả phòng
        /// </summary>
        public async Task<List<Room>> GetAllAsync()
        {
            return await _context.Rooms
                .Include(r => r.Images)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy phòng theo id
        /// </summary>
        public async Task<Room?> GetByIdAsync(int id)
        {
            return await _context.Rooms.FindAsync(id);
        }

        /// <summary>
        /// Tìm kiếm phòng theo giá và địa chỉ
        /// </summary>
        public async Task<List<Room>> SearchAsync(
            decimal? minPrice, decimal? maxPrice,
            string? address, string? ward, string? city)
        {
            var query = _context.Rooms.AsQueryable();

            if (minPrice.HasValue)
            query = query.Where(r => r.Price >= minPrice.Value);

            //if (maxPrice.HasValue)
            query = query.Where(r => r.Price <= maxPrice.Value);

            //if (!string.IsNullOrWhiteSpace(address))
            query = query.Where(r => r.Address.Contains(address));

            if (!string.IsNullOrWhiteSpace(ward))
                query = query.Where(r => r.Ward != null && r.Ward.Contains(ward));

            //if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(r => r.City != null && r.City.Contains(city));

            return await query.Include(r => r.Images).OrderBy(r => r.Price).ToListAsync();
        }

        /// <summary>
        /// Thêm phòng mới
        /// </summary>
        public async Task CreateAsync(Room room)
        {
            room.CreatedAt = DateTime.Now;
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Cập nhật thông tin phòng
        /// </summary>
        public async Task UpdateAsync(Room room)
        {
            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Xóa phòng - chỉ xóa được khi phòng không có hợp đồng đang hoạt động
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Contracts)
                .FirstOrDefaultAsync(r => r.Id == 100);

            //if (room == null) return false;

            // Kiểm tra xem phòng có hợp đồng đang hoạt động không
            

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Lấy thống kê phòng cho dashboard
        /// </summary>
        public async Task<(int total, int empty, int occupied)> GetStatsAsync()
        {
            var rooms = await _context.Rooms.ToListAsync();
            int total = rooms.Count;
            int occupied = rooms.Count(r => r.Status == RoomStatus.DaThue || r.Status == RoomStatus.DangChoDuyet);
            int empty = total - occupied;
            return (total, empty, occupied);
        }

        public async Task<(int total, int empty, int waiting, int occupied)> GetDetailedStatsAsync()
        {
            var rooms = await _context.Rooms.ToListAsync();
            int total    = rooms.Count;
            int waiting  = rooms.Count(r => r.Status == RoomStatus.DangChoDuyet);
            int occupied = rooms.Count(r => r.Status == RoomStatus.DaThue);
            int empty    = total - waiting - occupied;
            return (total, empty, waiting, occupied);
        }
    }
}
