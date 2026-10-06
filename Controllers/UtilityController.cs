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
            if (input.BillingMonth == default || input.BillingMonth > DateTime.Today.AddMonths(1))
            {
                ModelState.AddModelError("Input.BillingMonth", "Kỳ thu không hợp lệ.");
                var invalidPeriodModel = await BuildViewModelAsync(input.RoomId);
                invalidPeriodModel.Input = input;
                return View("Index", invalidPeriodModel);
            }
            var month = new DateTime(input.BillingMonth.Year, input.BillingMonth.Month, 1);
            var contracts = await _context.Contracts.Where(c => c.RoomId == input.RoomId && c.IsActive &&
                c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today &&
                c.StartDate < month.AddMonths(1) && c.EndDate >= month).ToListAsync();
            if (contracts.Count != 1)
                ModelState.AddModelError("", "Phòng phải có một hợp đồng đang thuê phù hợp với kỳ thu.");
            if (input.PreviousElectricityMeter < 0 || input.CurrentElectricityMeter < input.PreviousElectricityMeter ||
                input.CurrentElectricityMeter > 999999999 || input.ElectricityUnitPrice < 0 ||
                input.ElectricityUnitPrice > 9999999 || input.WaterUsage < 0 || input.WaterUsage > 999999999 ||
                (input.Note?.Length ?? 0) > 500 ||
                decimal.Round(input.PreviousElectricityMeter, 2) != input.PreviousElectricityMeter ||
                decimal.Round(input.CurrentElectricityMeter, 2) != input.CurrentElectricityMeter ||
                decimal.Round(input.ElectricityUnitPrice, 2) != input.ElectricityUnitPrice)
                ModelState.AddModelError("", "Chỉ số, đơn giá phải không âm, tối đa hai số lẻ; chỉ số mới không được nhỏ hơn chỉ số cũ.");

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildViewModelAsync(input.RoomId);
                viewModel.Input = input;
                return View("Index", viewModel);
            }

            var reading = await _context.UtilityReadings
                .FirstOrDefaultAsync(r => r.RoomId == input.RoomId && r.BillingMonth == month);

            if (reading == null)
            {
                reading = new UtilityReading { RoomId = input.RoomId, BillingMonth = month };
                _context.UtilityReadings.Add(reading);
            }

            if (reading.ElectricityPaidAt.HasValue || (reading.ContractId.HasValue && reading.ContractId != contracts[0].Id))
                return Conflict("Khoản điện đã thu hoặc thuộc hợp đồng khác, không thể sửa.");
            reading.ContractId = contracts[0].Id;
            reading.PreviousElectricityMeter = input.PreviousElectricityMeter;
            reading.CurrentElectricityMeter = input.CurrentElectricityMeter;
            reading.ElectricityUnitPrice = input.ElectricityUnitPrice;
            reading.ElectricityUsage = input.CurrentElectricityMeter - input.PreviousElectricityMeter;
            reading.WaterUsage = input.WaterUsage;
            reading.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
            reading.UpdatedAt = DateTime.Now;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Conflict("Dữ liệu đã thay đổi. Vui lòng tải lại."); }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            { return Conflict("Kỳ thu đã tồn tại. Vui lòng tải lại."); }

            TempData["Success"] = "Đã lưu số liệu điện nước của tháng.";
            return RedirectToAction(nameof(Index), new { roomId = input.RoomId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Collect(int id)
        {
            var reading = await _context.UtilityReadings.FindAsync(id);
            if (reading == null) return NotFound();
            if (!reading.ContractId.HasValue || !reading.ElectricityUnitPrice.HasValue)
                return BadRequest("Cần lập khoản điện trước khi thu.");
            if (!reading.ElectricityPaidAt.HasValue)
            {
                reading.ElectricityPaidAt = DateTime.Now;
                try { await _context.SaveChangesAsync(); }
                catch (DbUpdateConcurrencyException) { return Conflict("Khoản điện đã thay đổi. Vui lòng tải lại."); }
            }
            return RedirectToAction(nameof(Index), new { roomId = reading.RoomId });
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

            var currentUserId = isAdmin ? 0 : GetCurrentUserId();
            var readings = selectedRoom == null
                ? new List<UtilityReading>()
                : await _context.UtilityReadings.AsNoTracking()
                    .Where(r => r.RoomId == selectedRoom.Id && (isAdmin || (r.Contract != null && r.Contract.UserId == currentUserId)))
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
