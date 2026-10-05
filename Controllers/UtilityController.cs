using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Controllers
{
    [Authorize]
    public class UtilityController : Controller
    {
        private readonly AppDbContext _context;

        public UtilityController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? roomId)
        {
            return View(await BuildViewModelAsync(roomId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Save([Bind(Prefix = "Input")] UtilityReadingInputViewModel input)
        {
            if (!await _context.Rooms.AnyAsync(r => r.Id == input.RoomId))
                ModelState.AddModelError("Input.RoomId", "Phòng không tồn tại.");

            if (input.BillingMonth > DateTime.Today.AddMonths(1))
                ModelState.AddModelError("Input.BillingMonth", "Không thể nhập dữ liệu quá xa trong tương lai.");

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildViewModelAsync(input.RoomId);
                viewModel.Input = input;
                return View("Index", viewModel);
            }

            var month = new DateTime(input.BillingMonth.Year, input.BillingMonth.Month, 1);
            var reading = await _context.UtilityReadings
                .FirstOrDefaultAsync(r => r.RoomId == input.RoomId && r.BillingMonth == month);

            if (reading == null)
            {
                reading = new UtilityReading { RoomId = input.RoomId, BillingMonth = month };
                _context.UtilityReadings.Add(reading);
            }

            reading.ElectricityUsage = input.ElectricityUsage;
            reading.WaterUsage = input.WaterUsage;
            reading.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
            reading.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã lưu số liệu điện nước của tháng.";
            return RedirectToAction(nameof(Index), new { roomId = input.RoomId });
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<UtilityIndexViewModel> BuildViewModelAsync(int? requestedRoomId)
        {
            var isAdmin = User.IsInRole("Admin");
            var roomsQuery = _context.Rooms.AsNoTracking();

            if (!isAdmin)
            {
                var userId = GetCurrentUserId();
                var activeRoomIds = _context.Contracts
                    .Where(c => c.UserId == userId && c.IsActive &&
                                c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today)
                    .Select(c => c.RoomId);
                roomsQuery = roomsQuery.Where(r => activeRoomIds.Contains(r.Id));
            }

            var rooms = await roomsQuery.OrderBy(r => r.Name).ToListAsync();
            var selectedRoom = isAdmin
                ? rooms.FirstOrDefault(r => r.Id == requestedRoomId) ?? rooms.FirstOrDefault()
                : rooms.FirstOrDefault();

            var readings = selectedRoom == null
                ? new List<UtilityReading>()
                : await _context.UtilityReadings.AsNoTracking()
                    .Where(r => r.RoomId == selectedRoom.Id)
                    .OrderByDescending(r => r.BillingMonth)
                    .Take(24)
                    .ToListAsync();

            var chartReadings = readings.OrderBy(r => r.BillingMonth).TakeLast(12).ToList();
            return new UtilityIndexViewModel
            {
                IsAdmin = isAdmin,
                SelectedRoomId = selectedRoom?.Id,
                RoomName = selectedRoom?.Name ?? string.Empty,
                Rooms = rooms.Select(r => new SelectListItem(r.Name, r.Id.ToString(), r.Id == selectedRoom?.Id)).ToList(),
                Input = new UtilityReadingInputViewModel
                {
                    RoomId = selectedRoom?.Id ?? 0,
                    BillingMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
                },
                Readings = readings,
                ChartLabels = chartReadings.Select(r => r.BillingMonth.ToString("MM/yyyy")).ToList(),
                ElectricityData = chartReadings.Select(r => r.ElectricityUsage).ToList(),
                WaterData = chartReadings.Select(r => r.WaterUsage).ToList()
            };
        }
    }
}
