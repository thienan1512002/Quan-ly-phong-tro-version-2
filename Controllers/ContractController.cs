using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.DTOs;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers
{
    /// <summary>
    /// Controller quản lý hợp đồng
    /// Chỉ Admin mới có thể quản lý hợp đồng
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ContractController : Controller
    {
        private readonly ContractService _contractService;
        private readonly AppDbContext _context;

        public ContractController(ContractService contractService, AppDbContext context)
        {
            _contractService = contractService;
            _context = context;
        }

        // GET: /Contract - hiển thị trang (DataGrid load riêng)
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Contract/SeedData - Seed 10k hợp đồng giả để test hiệu năng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedData()
        {
            try
            {
                return await DoSeedData();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi server: {ex.GetType().Name}: {ex.Message}" });
            }
        }

        private async Task<IActionResult> DoSeedData()
        {
            const int TARGET = 10_000;

            // ── Lấy users và rooms hiện có trong DB ──────────────────
            var userIds = await _context.Users
                .Where(u => u.Role == "User")
                .Select(u => u.Id)
                .ToArrayAsync();
            if (userIds.Length == 0)
                return Json(new { success = false, message = "Không có User nào trong DB. Hãy tạo ít nhất 1 user trước." });

            var roomIds = await _context.Rooms
                .Select(r => r.Id)
                .ToArrayAsync();
            if (roomIds.Length == 0)
                return Json(new { success = false, message = "Không có Phòng nào trong DB. Hãy tạo ít nhất 1 phòng trước." });

            // ── Tạo hợp đồng cho đến khi đủ TARGET ──────────────────
            var existingCount = await _context.Contracts.CountAsync();
            int toCreate = TARGET - existingCount;
            if (toCreate <= 0)
                return Json(new { success = false, message = $"DB đã có {existingCount} hợp đồng, không cần seed thêm." });

            var rng = new Random();
            var baseDate = new DateTime(2022, 1, 1);
            var batch = new List<Contract>(500);

            for (int i = 0; i < toCreate; i++)
            {
                var startDate = baseDate.AddDays(rng.Next(0, 1000));
                var duration = rng.Next(30, 730);
                var endDate = startDate.AddDays(duration);

                batch.Add(new Contract
                {
                    RoomId = roomIds[rng.Next(roomIds.Length)],
                    UserId = userIds[rng.Next(userIds.Length)],
                    StartDate = startDate,
                    EndDate = endDate,
                    MonthlyRent = 1_000_000 + (decimal)(rng.Next(0, 20) * 500_000),
                    IsActive = endDate >= DateTime.Today,
                    Note = i % 5 == 0 ? $"Ghi chú seed #{i}" : null,
                    CreatedAt = startDate.AddDays(-rng.Next(1, 30))
                });

                if (batch.Count == 500)
                {
                    _context.Contracts.AddRange(batch);
                    await _context.SaveChangesAsync();
                    batch.Clear();
                }
            }
            if (batch.Count > 0)
            {
                _context.Contracts.AddRange(batch);
                await _context.SaveChangesAsync();
            }

            var total = await _context.Contracts.CountAsync();
            return Json(new { success = true, message = $"Đã seed thành công! Tổng hợp đồng trong DB: {total:N0}" });
        } // end DoSeedData

        // GET: /Contract/GetAll - DevExtreme DataGrid with server-side remote operations
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? filter = null,
            [FromQuery] string? sort = null,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20,
            [FromQuery] bool requireTotalCount = true)
        {
            var contracts = await _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.User)
                .AsNoTracking()
                .ToListAsync();

            var rows = contracts.Select(c => new ContractRow
            {
                id = c.Id,
                roomName = c.Room?.Name ?? "",
                roomAddress = c.Room?.Address ?? "",
                roomArea = (double)(c.Room?.Area ?? 0),
                roomPrice = (double)(c.Room?.Price ?? 0),
                roomPriceFormatted = (c.Room?.Price ?? 0).ToString("N0") + " VNĐ",
                roomDesc = c.Room?.Description ?? "",
                roomStatus = c.Room?.Status switch
                {
                    RoomStatus.DaThue => "Đã thuê",
                    RoomStatus.DangChoDuyet => "Đang chờ duyệt",
                    _ => "Trống"
                },
                userName = c.User?.FullName ?? "",
                userEmail = c.User?.Email ?? "",
                userUsername = c.User?.Username ?? "",
                userPhone = c.User?.PhoneNumber ?? "",
                startDate = c.StartDate.ToString("dd/MM/yyyy"),
                endDate = c.EndDate.ToString("dd/MM/yyyy"),
                durationDays = (c.EndDate - c.StartDate).Days + 1,
                monthlyRent = (double)c.MonthlyRent,
                monthlyRentFormatted = c.MonthlyRent.ToString("N0") + " VNĐ",
                note = c.Note ?? "",
                isActive = c.IsActive,
                statusText = c.IsActive ? "Đang hiệu lực" : "Đã kết thúc",
                createdAt = c.CreatedAt.ToString("dd/MM/yyyy")
            }).ToList();

            // ── Apply Filter ──────────────────────────────────────────
            if (!string.IsNullOrEmpty(filter) && filter != "null")
            {
                try
                {
                    var doc = JsonDocument.Parse(filter);
                    rows = rows.Where(r => DxEvalFilter(r, doc.RootElement)).ToList();
                }
                catch { /* ignore malformed filter */ }
            }

            // ── Apply Sort ────────────────────────────────────────────
            if (!string.IsNullOrEmpty(sort) && sort != "null")
            {
                try
                {
                    var sortDoc = JsonDocument.Parse(sort);
                    var sortArr = sortDoc.RootElement.EnumerateArray().ToArray();
                    if (sortArr.Length > 0)
                    {
                        var sel = sortArr[0].GetProperty("selector").GetString() ?? "";
                        var desc = sortArr[0].TryGetProperty("desc", out var d) && d.GetBoolean();
                        var prop = typeof(ContractRow).GetProperty(sel,
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (prop != null)
                            rows = desc
                                ? rows.OrderByDescending(r => prop.GetValue(r)).ToList()
                                : rows.OrderBy(r => prop.GetValue(r)).ToList();
                    }
                }
                catch { }
            }

            // ── Summary (computed before paging) ──────────────────────
            var totalCount = rows.Count;
            var activeCount = rows.Count(r => r.isActive);
            var totalRevenue = rows.Sum(r => r.monthlyRent);

            // ── Paging ────────────────────────────────────────────────
            if (take > 0)
                rows = rows.Skip(skip).Take(take).ToList();

            return Json(new { data = rows, totalCount, summary = new object[] { totalCount, activeCount, totalRevenue } });
        }

        // ── DevExtreme filter expression evaluator ────────────────────
        private static bool DxEvalFilter(ContractRow row, JsonElement expr)
        {
            if (expr.ValueKind != JsonValueKind.Array) return true;
            var arr = expr.EnumerateArray().ToArray();
            if (arr.Length == 0) return true;

            // NOT: ["!", condition]
            if (arr.Length == 2 && arr[0].ValueKind == JsonValueKind.String && arr[0].GetString() == "!")
                return !DxEvalFilter(row, arr[1]);

            // Simple condition: ["field", "op", value]
            if (arr.Length == 3 && arr[0].ValueKind == JsonValueKind.String && arr[1].ValueKind == JsonValueKind.String)
            {
                var field = arr[0].GetString()!;
                if (field != "and" && field != "or")
                    return DxEvalCondition(row, field, arr[1].GetString()!, arr[2]);
            }

            // Compound: [cond1, "and"/"or", cond2, ...]
            bool? result = null;
            string logOp = "and";
            foreach (var el in arr)
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (s == "and" || s == "or") { logOp = s; continue; }
                }
                bool val = DxEvalFilter(row, el);
                result = result == null ? val : logOp == "and" ? result.Value && val : result.Value || val;
            }
            return result ?? true;
        }

        private static bool DxEvalCondition(ContractRow row, string field, string op, JsonElement value)
        {
            var prop = typeof(ContractRow).GetProperty(field,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null) return true;
            var raw = prop.GetValue(row);

            if (value.ValueKind == JsonValueKind.String)
            {
                var cmp = value.GetString() ?? "";
                var str = raw?.ToString() ?? "";
                return op switch
                {
                    "=" => str.Equals(cmp, StringComparison.OrdinalIgnoreCase),
                    "<>" => !str.Equals(cmp, StringComparison.OrdinalIgnoreCase),
                    "contains" => str.Contains(cmp, StringComparison.OrdinalIgnoreCase),
                    "notcontains" => !str.Contains(cmp, StringComparison.OrdinalIgnoreCase),
                    "startswith" => str.StartsWith(cmp, StringComparison.OrdinalIgnoreCase),
                    "endswith" => str.EndsWith(cmp, StringComparison.OrdinalIgnoreCase),
                    _ => true
                };
            }
            if (value.ValueKind == JsonValueKind.Number)
            {
                double cmp = value.GetDouble();
                double num = raw switch
                {
                    double dv => dv,
                    float fv => fv,
                    int iv => iv,
                    long lv => lv,
                    decimal mv => (double)mv,
                    _ => 0
                };
                return op switch
                {
                    "=" => num == cmp,
                    "<>" => num != cmp,
                    "<" => num < cmp,
                    "<=" => num <= cmp,
                    ">" => num > cmp,
                    ">=" => num >= cmp,
                    _ => true
                };
            }
            if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            {
                bool cmp = value.GetBoolean();
                bool bVal = raw is bool b && b;
                return op == "=" ? bVal == cmp : bVal != cmp;
            }
            return true;
        }

        // GET: /Contract/Create
        public IActionResult Create()
        {
            // Lấy danh sách phòng trống để hiển thị dropdown
            var emptyRooms = _context.Rooms
                .Where(r => r.Status == RoomStatus.Trong)
                .Select(r => new { r.Id, Name = $"{r.Name} - {r.Address} ({r.Price:N0} VNĐ)" })
                .ToList();

            // Lấy danh sách user để hiển thị dropdown
            var users = _context.Users
                .Where(u => u.Role == "User")
                .Select(u => new { u.Id, Name = $"{u.FullName} ({u.Username})" })
                .ToList();

            ViewBag.Rooms = new SelectList(emptyRooms, "Id", "Name");
            ViewBag.Users = new SelectList(users, "Id", "Name");

            return View();
        }

        // POST: /Contract/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContractCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdowns nếu có lỗi
                PrepareDropdowns();
                return View(model);
            }

            var (success, message) = await _contractService.CreateAsync(model);

            if (success)
            {
                TempData["Success"] = message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", message);
            PrepareDropdowns();
            return View(model);
        }

        // GET: /Contract/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _contractService.GetByIdAsync(id);
            if (contract == null) return NotFound();
            return View(contract);
        }

        // POST: /Contract/Terminate/5
        // Kết thúc hợp đồng sớm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Terminate(int id)
        {
            var success = await _contractService.TerminateAsync(id);
            if (success)
                TempData["Success"] = "Hợp đồng đã được kết thúc. Phòng trở về trạng thái trống.";
            else
                TempData["Error"] = "Không tìm thấy hợp đồng!";

            return RedirectToAction(nameof(Index));
        }

        // Helper: tạo lại dropdowns
        private void PrepareDropdowns()
        {
            var emptyRooms = _context.Rooms
                .Where(r => r.Status == RoomStatus.Trong)
                .Select(r => new { r.Id, Name = $"{r.Name} - {r.Address} ({r.Price:N0} VNĐ)" })
                .ToList();
            var users = _context.Users
                .Where(u => u.Role == "User")
                .Select(u => new { u.Id, Name = $"{u.FullName} ({u.Username})" })
                .ToList();

            ViewBag.Rooms = new SelectList(emptyRooms, "Id", "Name");
            ViewBag.Users = new SelectList(users, "Id", "Name");
        }

        // GET: /Contract/CalendarView
        public IActionResult CalendarView()
        {
            return View();
        }

        // GET: /Contract/GetCalendarData?year=2024&month=1
        [HttpGet]
        public IActionResult GetCalendarData(int year, int month)
        {
            if (year < 2000 || year > 2100 || month < 1 || month > 12)
                return BadRequest();

            var rooms = _context.Rooms
                .OrderBy(r => r.Name)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Address,
                    r.Price,
                    r.Area,
                    Status = (int)r.Status
                })
                .ToList();

            var startOfMonth = new DateTime(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var contracts = _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.User)
                .Where(c => c.StartDate <= endOfMonth && c.EndDate >= startOfMonth)
                .Select(c => new
                {
                    c.Id,
                    c.RoomId,
                    UserFullName = c.User != null ? c.User.FullName : "",
                    UserEmail = c.User != null ? c.User.Email : "",
                    StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = c.EndDate.ToString("yyyy-MM-dd"),
                    StartDateDisplay = c.StartDate.ToString("dd/MM/yyyy"),
                    EndDateDisplay = c.EndDate.ToString("dd/MM/yyyy"),
                    MonthlyRent = c.MonthlyRent,
                    MonthlyRentFormatted = c.MonthlyRent.ToString("N0") + " VNĐ",
                    c.IsActive,
                    c.Note,
                    Deposit = c.Deposit,
                    PaymentDay = c.PaymentDay,
                    ManagementFee = c.ManagementFee,
                    ParkingFee = c.ParkingFee,
                    ElectricMeter = c.ElectricMeter,
                    WaterMeter = c.WaterMeter
                })
                .ToList();

            return Json(new
            {
                rooms,
                contracts,
                daysInMonth = DateTime.DaysInMonth(year, month),
                year,
                month
            });
        }
    }
}
