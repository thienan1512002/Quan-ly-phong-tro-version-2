using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Hubs;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers
{
    /// <summary>
    /// Controller quản lý phòng trọ
    /// - Admin: CRUD đầy đủ
    /// - User: chỉ xem và tìm kiếm
    /// </summary>
    [Authorize]
    public class RoomController : Controller
    {
        private readonly RoomService _roomService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RoomController(RoomService roomService, IHubContext<NotificationHub> hubContext,
            AppDbContext context, IWebHostEnvironment env)
        {
            _roomService = roomService;
            _hubContext = hubContext;
            _context = context;
            _env = env;
        }

        // GET: /Room
        // Hiển thị trang danh sách phòng (có DevExtreme DataGrid)
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Room/GetAll
        // API endpoint trả về JSON cho DevExtreme DataGrid
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rooms = await _roomService.GetAllAsync();
            var result = rooms.Select(r => new
            {
                r.Id,
                r.Name,
                r.Price,
                r.Area,
                r.Address,
                r.Ward,
                r.City,
                r.Status,
                images = r.Images.Select(i => new { i.Id, i.FileName, i.IsMain }).ToList()
            });
            return Json(result);
        }

        // GET: /Room/Search
        // API tìm kiếm phòng bằng AJAX
        [HttpGet]
        public async Task<IActionResult> Search(
            decimal? minPrice, decimal? maxPrice,
            string? address, string? ward, string? city)
        {
            var rooms = await _roomService.SearchAsync(minPrice, maxPrice, address, ward, city);
            var result = rooms.Select(r => new
            {
                r.Id,
                r.Name,
                r.Price,
                r.Area,
                r.Address,
                r.Ward,
                r.City,
                r.Status,
                images = r.Images.Select(i => new { i.Id, i.FileName, i.IsMain }).ToList()
            });
            return Json(result);
        }

        // POST: /Room/Create (JSON - dùng cho modal popup)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Room room)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join("<br>", errors) });
            }

            await _roomService.CreateAsync(room);
            await _hubContext.Clients.All.SendAsync("reloadRooms");
            return Json(new { success = true, message = "Thêm phòng thành công!", roomId = room.Id });
        }

        // GET: /Room/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _roomService.GetByIdAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        // POST: /Room/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Room room)
        {
            if (!ModelState.IsValid)
                return View(room);

            await _roomService.UpdateAsync(room);

            // Thông báo SignalR: reload danh sách phòng
            await _hubContext.Clients.All.SendAsync("reloadRooms");

            TempData["Success"] = "Cập nhật phòng thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Room/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _roomService.DeleteAsync(id);
            if (!success)
            {
                TempData["Error"] = "Không thể xóa phòng đang có hợp đồng!";
            }
            else
            {
                // Thông báo SignalR khi xóa phòng
                await _hubContext.Clients.All.SendAsync("reloadRooms");
                TempData["Success"] = "Xóa phòng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Room/SearchPage - trang tìm kiếm phòng cho User
        public IActionResult SearchPage()
        {
            return View(new RoomSearchViewModel());
        }

        // GET: /Room/GetImages?roomId=5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetImages(int roomId)
        {
            var images = await _context.RoomImages
                .Where(i => i.RoomId == roomId)
                .OrderByDescending(i => i.IsMain)
                .ThenBy(i => i.UploadedAt)
                .Select(i => new
                {
                    i.Id,
                    i.FileName,
                    i.IsMain,
                    url = "/images/rooms/" + i.FileName
                })
                .ToListAsync();
            return Json(images);
        }

        // POST: /Room/UploadImages
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImages(int roomId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return Json(new { success = false, message = "Chưa chọn file." });

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var uploadDir = Path.Combine(_env.WebRootPath, "images", "rooms");
            Directory.CreateDirectory(uploadDir);

            var hasMain = await _context.RoomImages.AnyAsync(i => i.RoomId == roomId && i.IsMain);
            var saved = new List<object>();

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext)) continue;
                if (file.Length > 10 * 1024 * 1024) continue; // giới hạn 10MB/ảnh

                var safeName = $"{roomId}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadDir, safeName);

                using (var stream = System.IO.File.Create(filePath))
                    await file.CopyToAsync(stream);

                var img = new RoomImage
                {
                    RoomId = roomId,
                    FileName = safeName,
                    IsMain = !hasMain,
                    UploadedAt = DateTime.Now
                };
                hasMain = true;
                _context.RoomImages.Add(img);
                await _context.SaveChangesAsync();

                saved.Add(new { img.Id, img.FileName, img.IsMain, url = "/images/rooms/" + safeName });
            }

            return Json(new { success = true, images = saved });
        }

        // POST: /Room/DeleteImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var img = await _context.RoomImages.FindAsync(imageId);
            if (img == null) return Json(new { success = false });

            var filePath = Path.Combine(_env.WebRootPath, "images", "rooms", img.FileName);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            bool wasMain = img.IsMain;
            int roomId = img.RoomId;
            _context.RoomImages.Remove(img);
            await _context.SaveChangesAsync();

            // Nếu xóa ảnh chính thì tự động gán ảnh đầu tiên còn lại làm ảnh chính
            if (wasMain)
            {
                var next = await _context.RoomImages
                    .Where(i => i.RoomId == roomId)
                    .OrderBy(i => i.UploadedAt)
                    .FirstOrDefaultAsync();
                if (next != null) { next.IsMain = true; await _context.SaveChangesAsync(); }
            }

            return Json(new { success = true });
        }

        // POST: /Room/SetMainImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetMainImage(int imageId)
        {
            var img = await _context.RoomImages.FindAsync(imageId);
            if (img == null) return Json(new { success = false });

            // Bỏ ảnh chính cũ
            var old = await _context.RoomImages
                .Where(i => i.RoomId == img.RoomId && i.IsMain)
                .ToListAsync();
            old.ForEach(i => i.IsMain = false);

            img.IsMain = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
