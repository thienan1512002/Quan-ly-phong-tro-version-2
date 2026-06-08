using Microsoft.AspNetCore.SignalR;

namespace QuanLyPhongTro.Hubs
{
    /// <summary>
    /// SignalR Hub - trung tâm xử lý realtime
    /// 
    /// Các event được gửi đến client:
    /// - "reloadRooms"    : Yêu cầu client reload danh sách phòng
    /// - "newRequest"     : Thông báo có yêu cầu thuê mới
    /// </summary>
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Gửi lệnh reload danh sách phòng đến tất cả client
        /// Được gọi khi thêm/sửa/xóa phòng
        /// </summary>
        public async Task ReloadRooms()
        {
            await Clients.All.SendAsync("reloadRooms");
        }

        /// <summary>
        /// Gửi thông báo yêu cầu thuê mới đến tất cả admin
        /// </summary>
        public async Task NotifyNewRequest(string roomName, string userName)
        {
            await Clients.All.SendAsync("newRequest", roomName, userName);
        }
    }
}
