using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services
{
    /// <summary>
    /// Service xử lý logic hợp đồng thuê phòng
    /// </summary>
    public class ContractService
    {
        private readonly AppDbContext _context;

        public ContractService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả hợp đồng
        /// </summary>
        public async Task<List<Contract>> GetAllAsync()
        {
            return await _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy hợp đồng theo id
        /// </summary>
        public async Task<Contract?> GetByIdAsync(int id)
        {
            return await _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Lấy số hợp đồng đang hiệu lực
        /// </summary>
        public async Task<int> GetActiveCountAsync()
        {
            return await _context.Contracts.CountAsync(c => c.IsActive);
        }

        /// <summary>
        /// Lấy hợp đồng đang hiệu lực của một user cụ thể
        /// </summary>
        public async Task<Contract?> GetActiveByUserAsync(int userId)
        {
            return await _context.Contracts
                .Include(c => c.Room)
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Tạo hợp đồng mới
        /// Khi tạo hợp đồng → tự động cập nhật phòng thành "Đã thuê"
        /// </summary>
        public async Task<(bool success, string message)> CreateAsync(ContractCreateViewModel model)
        {
            // Validate ngày
            if (model.EndDate <= model.StartDate)
                return (false, "Ngày kết thúc phải sau ngày bắt đầu");

            // Lấy thông tin phòng
            var room = await _context.Rooms.FindAsync(model.RoomId);
            if (room == null)
                return (false, "Không tìm thấy phòng");

            if (room.Status == RoomStatus.DaThue)
                return (false, "Phòng này đã có người thuê");

            // Kiểm tra người thuê tồn tại
            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null)
                return (false, "Không tìm thấy người thuê");

            // Tạo hợp đồng
            var contract = new Contract
            {
                RoomId = model.RoomId,
                UserId = model.UserId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                MonthlyRent = room.Price,
                Note = model.Note,
                Deposit = model.Deposit,
                PaymentDay = model.PaymentDay,
                ManagementFee = model.ManagementFee,
                ParkingFee = model.ParkingFee,
                ElectricMeter = model.ElectricMeter,
                WaterMeter = model.WaterMeter,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Contracts.Add(contract);

            // Cập nhật trạng thái phòng → Đã thuê
            room.Status = RoomStatus.DaThue;

            await _context.SaveChangesAsync();
            return (true, "Tạo hợp đồng thành công");
        }

        /// <summary>
        /// Kết thúc hợp đồng (Admin thực hiện)
        /// Khi kết thúc → phòng trở lại "Trống"
        /// </summary>
        public async Task<bool> TerminateAsync(int contractId)
        {
            var contract = await _context.Contracts
                .Include(c => c.Room)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null) return false;

            // Đánh dấu hợp đồng không còn hiệu lực
            contract.IsActive = false;

            // Phòng trở về trạng thái trống
            if (contract.Room != null)
                contract.Room.Status = RoomStatus.Trong;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(int within30, int within60, int within90)> GetExpiringCountsAsync()
        {
            var today = DateTime.Today;
            var d30   = today.AddDays(30);
            var d60   = today.AddDays(60);
            var d90   = today.AddDays(90);
            var dates = await _context.Contracts
                .Where(c => c.IsActive && c.EndDate >= today && c.EndDate <= d90)
                .Select(c => c.EndDate)
                .ToListAsync();
            return (
                dates.Count(d => d <= d30),
                dates.Count(d => d > d30 && d <= d60),
                dates.Count(d => d > d60)
            );
        }
    }
}
