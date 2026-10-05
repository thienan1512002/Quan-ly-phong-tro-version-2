using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Hubs;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationController(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            return View(await BuildViewModelAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind(Prefix = "CreateModel")] NotificationCreateViewModel model)
        {
            if (!Enum.IsDefined(model.Type))
                ModelState.AddModelError("CreateModel.Type", "Loại thông báo không hợp lệ.");

            var recipientIds = model.TargetUserId.HasValue
                ? await _context.Users
                    .Where(u => u.Id == model.TargetUserId && u.Role == "User")
                    .Select(u => u.Id)
                    .ToListAsync()
                : await _context.Users
                    .Where(u => u.Role == "User")
                    .Select(u => u.Id)
                    .ToListAsync();

            if (recipientIds.Count == 0)
                ModelState.AddModelError("CreateModel.TargetUserId", "Không tìm thấy người nhận phù hợp.");

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildViewModelAsync();
                viewModel.CreateModel = model;
                return View("Index", viewModel);
            }

            var createdAt = DateTime.Now;
            var notifications = recipientIds.Select(userId => new UserNotification
            {
                UserId = userId,
                Type = model.Type,
                Title = model.Title.Trim(),
                Content = model.Content.Trim(),
                EventDate = model.EventDate,
                CreatedAt = createdAt
            }).ToList();

            _context.UserNotifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Users(recipientIds.Select(id => id.ToString()))
                .SendAsync("ownerNotification", model.Title);

            TempData["Success"] = $"Đã gửi thông báo đến {recipientIds.Count} người thuê.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = GetCurrentUserId();
            var notification = await _context.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                return NotFound();

            notification.ReadAt ??= DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetCurrentUserId();
            var unread = await _context.UserNotifications
                .Where(n => n.UserId == userId && n.ReadAt == null)
                .ToListAsync();

            var now = DateTime.Now;
            foreach (var notification in unread)
                notification.ReadAt = now;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = GetCurrentUserId();
            var count = await _context.UserNotifications.CountAsync(n => n.UserId == userId && n.ReadAt == null);
            return Json(new { count });
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<NotificationIndexViewModel> BuildViewModelAsync()
        {
            var isAdmin = User.IsInRole("Admin");
            IQueryable<UserNotification> query = _context.UserNotifications
                .Include(n => n.User)
                .AsNoTracking();

            if (!isAdmin)
            {
                var userId = GetCurrentUserId();
                query = query.Where(n => n.UserId == userId);
            }

            var users = isAdmin
                ? await _context.Users.Where(u => u.Role == "User")
                    .OrderBy(u => u.FullName)
                    .Select(u => new SelectListItem(u.FullName + " (" + u.Username + ")", u.Id.ToString()))
                    .ToListAsync()
                : new List<SelectListItem>();

            if (isAdmin)
                users.Insert(0, new SelectListItem("Tất cả người thuê", string.Empty));

            return new NotificationIndexViewModel
            {
                IsAdmin = isAdmin,
                Users = users,
                Notifications = await query.OrderByDescending(n => n.CreatedAt).Take(200).ToListAsync()
            };
        }
    }
}
