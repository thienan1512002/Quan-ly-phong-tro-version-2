# 📚 DOCUMENT - Hệ thống Quản Lý Phòng Trọ

---

## 1. 🔐 Chức năng Đăng nhập (Login)

### Mô tả

Cho phép người dùng đăng nhập vào hệ thống bằng tài khoản username/password. Có 2 role: **Admin** và **User**.

### Luồng hoạt động

```
Người dùng → Nhập username + password → POST /Account/Login
→ Kiểm tra username trong DB
→ BCrypt.Verify(password, passwordHash)
→ Nếu đúng: tạo Claims + Cookie → Redirect về Dashboard
→ Nếu sai: hiện lỗi "Tên đăng nhập hoặc mật khẩu không đúng"
```

### Controller & Actions

| Method | URL                     | Mô tả                          |
| ------ | ----------------------- | ------------------------------ |
| GET    | `/Account/Login`        | Hiển thị form đăng nhập        |
| POST   | `/Account/Login`        | Xử lý đăng nhập                |
| POST   | `/Account/Logout`       | Đăng xuất, xóa cookie          |
| GET    | `/Account/AccessDenied` | Trang thông báo không có quyền |

### UI

- Trang login riêng biệt, không có sidebar
- Form với 2 field: Username, Password
- Checkbox "Ghi nhớ đăng nhập" (cookie 7 ngày hoặc 8 giờ)
- Hiển thị tài khoản demo để test dễ dàng

### Ghi chú kỹ thuật

- Dùng `Cookie Authentication` của ASP.NET Core
- Mật khẩu được hash bằng `BCrypt.Net-Next`
- Thông tin user lưu trong `Claims`: Id, Username, Role, FullName
- Dùng `[Authorize]` attribute để bảo vệ các trang cần đăng nhập

---

## 2. 🏠 Chức năng Quản lý Phòng (Room)

### Mô tả

Admin quản lý toàn bộ phòng trọ (CRUD). User chỉ xem danh sách. Danh sách hiển thị bằng DevExtreme DataGrid.

### Luồng hoạt động

```
Admin → Trang /Room
→ DevExtreme DataGrid gọi AJAX /Room/GetAll → Hiển thị bảng

Admin → Click "Thêm phòng" → Form Create
→ POST /Room/Create → Lưu DB → SignalR "reloadRooms" → Redirect Index

Admin → Click "Sửa" → Form Edit
→ POST /Room/Edit → Cập nhật DB → SignalR "reloadRooms" → Redirect Index

Admin → Click "Xóa" → Confirm → POST /Room/Delete
→ Kiểm tra không có hợp đồng đang hoạt động
→ Xóa DB → SignalR "reloadRooms" → Redirect Index
```

### Controller & Actions

| Method | URL                 | Auth  | Mô tả                   |
| ------ | ------------------- | ----- | ----------------------- |
| GET    | `/Room`             | Login | Trang hiển thị DataGrid |
| GET    | `/Room/GetAll`      | Login | API JSON cho DataGrid   |
| GET    | `/Room/Search`      | Login | API tìm kiếm AJAX       |
| GET    | `/Room/Create`      | Admin | Form thêm phòng         |
| POST   | `/Room/Create`      | Admin | Lưu phòng mới           |
| GET    | `/Room/Edit/{id}`   | Admin | Form sửa phòng          |
| POST   | `/Room/Edit`        | Admin | Lưu chỉnh sửa           |
| POST   | `/Room/Delete/{id}` | Admin | Xóa phòng               |
| GET    | `/Room/SearchPage`  | Login | Trang tìm kiếm          |

### UI - DevExtreme DataGrid

- Paging (10 records/trang, có thể đổi)
- Search panel (tìm theo tất cả cột)
- Header filter (lọc theo cột)
- Cột trạng thái hiển thị bằng badge màu (Xanh=Trống, Đỏ=ĐãThuê)
- Nút Sửa/Xóa chỉ hiện với Admin

### SignalR

Sau khi thêm/sửa/xóa phòng, server gọi:

```csharp
await _hubContext.Clients.All.SendAsync("reloadRooms");
```

Client lắng nghe và tự reload grid:

```javascript
connection.on("reloadRooms", () => reloadRoomGrid());
```

---

## 3. 🔍 Chức năng Tìm kiếm Phòng (Search)

### Mô tả

User tìm phòng theo giá và địa chỉ, kết quả load bằng AJAX không reload trang.

### Luồng hoạt động

```
User → Trang /Room/SearchPage
→ Nhập filter (giá min/max, địa chỉ) → Click "Tìm kiếm"
→ Fetch API gọi /Room/Search?minPrice=...&maxPrice=...&address=...
→ Server lọc DB, trả về JSON
→ JavaScript render kết quả dạng card
→ User thấy phòng trống → Click "Thuê phòng này" → openRentModal()
→ Gửi yêu cầu thuê bằng form submit
```

### API

```
GET /Room/Search?minPrice={}&maxPrice={}&address={}
→ Trả về: JSON[] - danh sách phòng match
```

### UI

- 3 input filter: giá min, giá max, địa chỉ
- Button "Xóa bộ lọc" để reset
- Kết quả hiển thị dạng card grid (responsive)
- Badge màu trạng thái
- Nút "Thuê phòng này" chỉ xuất hiện khi phòng trống
- Khi bấm thuê: prompt() hỏi ghi chú → submit form đến `/RentalRequest/Create`

### Kỹ thuật

```javascript
const response = await fetch(`/Room/Search?${params.toString()}`);
const rooms = await response.json();
displayRooms(rooms);
```

---

## 4. 📋 Chức năng Yêu cầu thuê phòng (Rental Request)

### Mô tả

User gửi yêu cầu thuê phòng. Admin xem danh sách và duyệt/từ chối.

### Luồng hoạt động

```
User:
  Tìm phòng → Click "Thuê phòng này" → Nhập ghi chú
  → POST /RentalRequest/Create → Kiểm tra phòng còn trống
  → Tạo RentalRequest (Status=ChờDuyệt)
  → SignalR "newRequest" thông báo Admin
  → Redirect về danh sách yêu cầu

Admin:
  Xem /RentalRequest → Danh sách tất cả yêu cầu
  → Click "Duyệt": POST /RentalRequest/Approve → Status=ĐãDuyệt
  → Click "Từ chối": nhập lý do → POST /RentalRequest/Reject → Status=TừChối
```

### Controller & Actions

| Method | URL                           | Auth  | Mô tả                                     |
| ------ | ----------------------------- | ----- | ----------------------------------------- |
| GET    | `/RentalRequest`              | Login | Danh sách (Admin: tất cả, User: của mình) |
| POST   | `/RentalRequest/Create`       | User  | Gửi yêu cầu thuê                          |
| POST   | `/RentalRequest/Approve/{id}` | Admin | Duyệt yêu cầu                             |
| POST   | `/RentalRequest/Reject/{id}`  | Admin | Từ chối yêu cầu                           |

### UI

- Bảng danh sách với badge trạng thái (Vàng=Chờ, Xanh=ĐãDuyệt, Đỏ=TừChối)
- Admin: có cột "Người gửi" và nút Duyệt/Từ chối
- Khi từ chối: `prompt()` hỏi lý do

### SignalR

```csharp
// Sau khi User gửi yêu cầu:
await _hubContext.Clients.All.SendAsync("newRequest", room.Name, userName);
```

```javascript
// Client (Admin) nhận:
connection.on("newRequest", (roomName, userName) => {
  showToast("Yêu cầu mới!", userName + " muốn thuê " + roomName);
});
```

---

## 5. 📄 Chức năng Hợp đồng (Contract)

### Mô tả

Admin tạo hợp đồng cho thuê. Khi tạo → phòng tự động chuyển sang "Đã thuê".

### Luồng hoạt động

```
Admin → /Contract/Create
→ Chọn phòng trống, chọn người thuê, nhập ngày bắt đầu/kết thúc
→ POST /Contract/Create
→ Validate: ngày kết thúc > ngày bắt đầu, phòng chưa được thuê
→ Tạo Contract + CẬP NHẬT Room.Status = ĐãThuê
→ Redirect về danh sách hợp đồng

Admin → Click "Kết thúc" trên hợp đồng đang hiệu lực
→ POST /Contract/Terminate → IsActive = false
→ Room.Status = Trống
```

### Controller & Actions

| Method | URL                        | Auth  | Mô tả              |
| ------ | -------------------------- | ----- | ------------------ |
| GET    | `/Contract`                | Admin | Danh sách hợp đồng |
| GET    | `/Contract/Create`         | Admin | Form tạo hợp đồng  |
| POST   | `/Contract/Create`         | Admin | Lưu hợp đồng mới   |
| GET    | `/Contract/Details/{id}`   | Admin | Chi tiết hợp đồng  |
| POST   | `/Contract/Terminate/{id}` | Admin | Kết thúc hợp đồng  |

### UI

- Dropdown "Chọn phòng" chỉ hiện phòng đang **Trống**
- Alert thông tin: "Sau khi tạo, phòng sẽ chuyển sang Đã thuê"
- Bảng danh sách có cột trạng thái (Đang hiệu lực / Đã kết thúc)
- Nút "Kết thúc" với confirm dialog

### Ghi chú quan trọng

```csharp
// Trong ContractService.CreateAsync():
room.Status = RoomStatus.DaThue; // Tự động cập nhật phòng
await _context.SaveChangesAsync(); // Lưu cả Contract lẫn Room
```

---

## 6. 📊 Dashboard

### Mô tả

Trang tổng quan hiển thị số liệu thống kê và các quick action links.

### Luồng hoạt động

```
GET /Dashboard → DashboardController.Index()
→ Gọi RoomService.GetStatsAsync() → (total, empty, occupied)
→ Gọi RentalRequestService.GetPendingCountAsync()
→ Gọi ContractService.GetActiveCountAsync()
→ Trả về View với DashboardViewModel
```

### Dữ liệu hiển thị

- 4 cards: Tổng phòng, Phòng trống, Đã thuê, Hợp đồng hiệu lực
- Progress bar tỉ lệ trống/đã thuê
- Quick action buttons với badge số yêu cầu chờ duyệt

---

## 7. ⚡ SignalR - Realtime

### Mô tả

Cập nhật realtime không cần refresh trang.

### Kiến trúc

```
Server → NotificationHub → Client (tất cả hoặc nhóm)
```

### Hub Methods (Server)

```csharp
// File: Hubs/NotificationHub.cs
public class NotificationHub : Hub
{
    // Client có thể gọi để broadcast reload
    public async Task ReloadRooms() { ... }

    // Client có thể gọi để broadcast request mới
    public async Task NotifyNewRequest(roomName, userName) { ... }
}
```

### Events từ Server → Client

| Event         | Khi nào               | Client xử lý                                 |
| ------------- | --------------------- | -------------------------------------------- |
| `reloadRooms` | Thêm/Sửa/Xóa phòng    | Gọi `reloadRoomGrid()` nếu đang ở trang Room |
| `newRequest`  | User gửi yêu cầu thuê | Hiển thị toast notification                  |

### Client JS (trong \_Layout.cshtml)

```javascript
// Kết nối
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/notificationHub")
  .withAutomaticReconnect()
  .build();

// Lắng nghe events
connection.on("reloadRooms", () => {
  if (typeof reloadRoomGrid === "function") reloadRoomGrid();
});

connection.on("newRequest", (roomName, userName) => {
  showToast("Yêu cầu mới!", `${userName} muốn thuê phòng ${roomName}`);
});

connection.start();
```

### Server gửi event

```csharp
// Trong RoomController (qua IHubContext):
await _hubContext.Clients.All.SendAsync("reloadRooms");

// Trong RentalRequestController:
await _hubContext.Clients.All.SendAsync("newRequest", room.Name, userName);
```

### Endpoint

```csharp
// Trong Program.cs:
app.MapHub<NotificationHub>("/notificationHub");
```

---

## 8. 🏗️ Dependency Injection (DI)

Tất cả service được đăng ký với `Scoped` lifetime (tạo mới mỗi request):

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(options => ...);
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<RentalRequestService>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddSignalR();
```

Controller nhận service qua constructor:

```csharp
public class RoomController : Controller
{
    private readonly RoomService _roomService;

    // ASP.NET Core tự inject RoomService
    public RoomController(RoomService roomService, IHubContext<NotificationHub> hubContext)
    {
        _roomService = roomService;
    }
}
```
