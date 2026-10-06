using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using QuanLyPhongTro.Hubs;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;
using System.Security.Claims;

namespace QuanLyPhongTro.Controllers
{
    /// <summary>
    /// Controller quản lý yêu cầu thuê phòng
    /// - User: gửi yêu cầu, xem danh sách yêu cầu của mình
    /// - Admin: xem tất cả, duyệt / từ chối
    /// </summary>
    [Authorize]
    public class RentalRequestController : Controller
    {
        private readonly RentalRequestService _requestService;
        private readonly RoomService _roomService;
        private readonly ContractService _contractService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public RentalRequestController(
            RentalRequestService requestService,
            RoomService roomService,
            ContractService contractService,
            IHubContext<NotificationHub> hubContext)
        {
            _requestService = requestService;
            _roomService = roomService;
            _contractService = contractService;
            _hubContext = hubContext;
        }

        // GET: /RentalRequest - hiển thị trang (DataGrid load riêng)
        public IActionResult Index()
        {
            return View();
        }

        // GET: /RentalRequest/GetAll - API JSON cho DevExtreme DataGrid
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = 0;
            var isAdmin = User.IsInRole("Admin");

            var requests = isAdmin
                ? await _requestService.GetAllAsync()
                : await _requestService.GetByUserAsync(userId);

            // Trả về dữ liệu phẳng (flat) cho DevExtreme
            var data = requests.Select(r => new
            {
                r.Id,
                r.RoomId,
                r.UserId,
                UserFullName = r.User?.FullName ?? "",
                RoomName = r.Room?.Name ?? "",
                RoomAddress = r.Room?.Address ?? "",
                RoomPrice = r.Room?.Price ?? 0,
                r.Note,
                r.AdminNote,
                r.Deposit,
                DesiredStartDate = r.DesiredStartDate.HasValue ? r.DesiredStartDate.Value.ToString("yyyy-MM-dd") : null,
                DesiredEndDate = r.DesiredEndDate.HasValue ? r.DesiredEndDate.Value.ToString("yyyy-MM-dd") : null,
                StatusValue = (int)r.Status,
                StatusText = r.Status switch
                {
                    QuanLyPhongTro.Models.RequestStatus.ChoDuyet => "Chờ duyệt",
                    QuanLyPhongTro.Models.RequestStatus.DaDuyet => "Đã duyệt",
                    QuanLyPhongTro.Models.RequestStatus.TuChoi => "Từ chối",
                    QuanLyPhongTro.Models.RequestStatus.DaHuy => "Đã hủy",
                    _ => ""
                },
                CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });

            return Json(data);
        }

        // POST: /RentalRequest/Create
        // User gửi yêu cầu thuê phòng
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Create(int roomId, string? note,
            DateTime? desiredStartDate = null, DateTime? desiredEndDate = null,
            decimal? deposit = null)
        {
            if (!deposit.HasValue || deposit.Value < 0)
            {
                TempData["Error"] = "Vui lòng nhập tiền cọc hợp lệ.";
                return RedirectToAction("SearchPage", "Room");
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _requestService.CreateAsync(
                roomId, userId, note, desiredStartDate, desiredEndDate, deposit);

            if (success)
            {
                // Lấy thông tin phòng để gửi thông báo
                var room = await _roomService.GetByIdAsync(roomId);
                var userName = User.FindFirstValue("FullName") ?? User.Identity!.Name;

                // Gửi thông báo SignalR đến tất cả admin
                await _hubContext.Clients.All.SendAsync("newRequest", room?.Name, userName);

                TempData["Success"] = "Gửi yêu cầu thành công! Vui lòng chờ admin duyệt.";
            }
            else
            {
                TempData["Error"] = "Không thể gửi yêu cầu. Phòng đã có người thuê hoặc bạn đã gửi yêu cầu trước đó.";
            }

            return RedirectToAction("Index");
        }

        // POST: /RentalRequest/ApproveAndCreateContract
        // Duyệt yêu cầu + tạo hợp đồng + đổi trạng thái phòng (JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveAndCreateContract(
            int requestId, DateTime startDate, DateTime endDate, string? note,
            decimal? deposit, int? paymentDay, decimal? managementFee,
            decimal? parkingFee, int? electricMeter, int? waterMeter)
        {
            var request = await _requestService.GetByIdAsync(requestId);
            if (request == null)
                return Json(new { success = false, message = "Không tìm thấy yêu cầu thuê." });

            var model = new ContractCreateViewModel
            {
                RoomId = request.RoomId,
                UserId = request.UserId,
                StartDate = startDate,
                EndDate = endDate,
                Note = note,
                Deposit = deposit ?? request.Deposit,
                PaymentDay = paymentDay,
                ManagementFee = managementFee,
                ParkingFee = parkingFee,
                ElectricMeter = electricMeter,
                WaterMeter = waterMeter
            };

            var (success, message) = await _contractService.CreateAsync(model);
            if (!success)
                return Json(new { success = false, message });

            await _requestService.ApproveAsync(requestId, null);
            return Json(new { success = true, message = "Đã duyệt và tạo hợp đồng thành công!" });
        }

        // POST: /RentalRequest/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, string? adminNote)
        {
            await _requestService.ApproveAsync(id, adminNote);
            TempData["Success"] = "Đã duyệt yêu cầu!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /RentalRequest/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id, string? adminNote)
        {
            await _requestService.RejectAsync(id, adminNote);
            TempData["Success"] = "Đã từ chối yêu cầu.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /RentalRequest/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _requestService.CancelAsync(id, userId);
            return Json(new { success = ok, message = ok ? "Đã hủy yêu cầu thành công." : "Không thể hủy yêu cầu này." });
        }
    }
}
