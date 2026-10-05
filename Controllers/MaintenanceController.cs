using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Controllers
{
    [Authorize]
    public class MaintenanceController : Controller
    {
        private readonly AppDbContext _context;

        public MaintenanceController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await BuildViewModelAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Create([Bind(Prefix = "CreateModel")] MaintenanceCreateViewModel model)
        {
            var userId = GetCurrentUserId();
            var activeContract = await GetActiveContractAsync(userId);

            if (activeContract == null)
                ModelState.AddModelError(string.Empty, "Bạn cần có hợp đồng đang hiệu lực để gửi yêu cầu sửa chữa.");

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildViewModelAsync();
                viewModel.CreateModel = model;
                return View("Index", viewModel);
            }

            _context.MaintenanceRequests.Add(new MaintenanceRequest
            {
                UserId = userId,
                RoomId = activeContract!.RoomId,
                Title = model.Title.Trim(),
                Category = model.Category.Trim(),
                Description = model.Description.Trim(),
                Priority = model.Priority
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi yêu cầu sửa chữa. Chủ trọ sẽ sớm phản hồi cho bạn.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, MaintenanceStatus status, string? adminNote)
        {
            if (!Enum.IsDefined(status))
                return BadRequest();

            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            if (adminNote?.Length > 1000)
            {
                TempData["Error"] = "Ghi chú xử lý không được vượt quá 1000 ký tự.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = status;
            request.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
            request.UpdatedAt = DateTime.Now;
            request.CompletedAt = status == MaintenanceStatus.HoanThanh ? DateTime.Now : null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật yêu cầu sửa chữa.";
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private Task<Contract?> GetActiveContractAsync(int userId) =>
            _context.Contracts
                .Include(c => c.Room)
                .Where(c => c.UserId == userId && c.IsActive &&
                            c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync();

        private async Task<MaintenanceIndexViewModel> BuildViewModelAsync()
        {
            var isAdmin = User.IsInRole("Admin");
            var query = _context.MaintenanceRequests
                .Include(r => r.Room)
                .Include(r => r.User)
                .AsNoTracking();

            Contract? activeContract = null;
            if (!isAdmin)
            {
                var userId = GetCurrentUserId();
                query = query.Where(r => r.UserId == userId);
                activeContract = await GetActiveContractAsync(userId);
            }

            return new MaintenanceIndexViewModel
            {
                IsAdmin = isAdmin,
                ActiveContract = activeContract,
                Requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync()
            };
        }
    }
}
