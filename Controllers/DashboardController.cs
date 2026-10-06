using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;
using System.Security.Claims;

namespace QuanLyPhongTro.Controllers
{
    /// <summary>
    /// Controller cho trang Dashboard - tổng quan hệ thống
    /// Yêu cầu đăng nhập mới vào được
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly RoomService _roomService;
        private readonly RentalRequestService _requestService;
        private readonly ContractService _contractService;

        public DashboardController(
            RoomService roomService,
            RentalRequestService requestService,
            ContractService contractService)
        {
            _roomService = roomService;
            _requestService = requestService;
            _contractService = contractService;
        }

        // GET: /Dashboard hoặc /Dashboard/Index
        public async Task<IActionResult> Index()
        {
            var (total, empty, waiting, occupied) = await _roomService.GetDetailedStatsAsync();

            int? currentUserId = null;
            bool isUser = User.IsInRole("User");
            if (isUser)
                currentUserId = null;

            var pendingRequests = await _requestService.GetPendingCountAsync(currentUserId);
            var activeContracts = await _contractService.GetActiveCountAsync();

            var viewModel = new DashboardViewModel
            {
                TotalRooms      = total,
                EmptyRooms      = empty,
                WaitingRooms    = waiting,
                OccupiedRooms   = occupied,
                PendingRequests = pendingRequests,
                ActiveContracts = activeContracts,
                IsUser          = isUser
            };

            if (!isUser)
            {
                // Admin: load chart data
                var (choDuyet, daDuyet, tuChoi, daHuy) = await _requestService.GetStatusCountsAsync();
                viewModel.ReqChoDuyet = choDuyet;
                viewModel.ReqDaDuyet  = daDuyet;
                viewModel.ReqTuChoi   = tuChoi;
                viewModel.ReqDaHuy    = daHuy;

                var (exp30, exp60, exp90) = await _contractService.GetExpiringCountsAsync();
                viewModel.ContractsExpiring30 = exp30;
                viewModel.ContractsExpiring60 = exp60;
                viewModel.ContractsExpiring90 = exp90;
            }
            else 
            {
                viewModel.UserActiveContract = await _contractService.GetActiveByUserAsync(currentUserId.Value);
                viewModel.UserRecentRequests = await _requestService.GetByUserAsync(currentUserId.Value);
            }

            return View(viewModel);
        }
    }
}
