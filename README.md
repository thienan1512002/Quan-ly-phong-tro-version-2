# StayFlow - Hệ thống quản lý phòng trọ

StayFlow là ứng dụng web quản lý phòng trọ dành cho chủ trọ và người thuê. Hệ thống hỗ trợ quản lý phòng, hình ảnh, yêu cầu thuê, hợp đồng, lịch thuê, bảo trì, thông báo, điện nước và báo cáo doanh thu.

Dự án được xây dựng bằng ASP.NET Core MVC trên .NET 8, Entity Framework Core theo hướng Code First và SQL Server/LocalDB.

## Mục lục

- [1. Chức năng chính](#1-chức-năng-chính)
- [2. Công nghệ sử dụng](#2-công-nghệ-sử-dụng)
- [3. Kiến trúc hệ thống](#3-kiến-trúc-hệ-thống)
- [4. Phân quyền](#4-phân-quyền)
- [5. Luồng nghiệp vụ](#5-luồng-nghiệp-vụ)
- [6. Mô hình dữ liệu](#6-mô-hình-dữ-liệu)
- [7. Cấu trúc source code](#7-cấu-trúc-source-code)
- [8. Cài đặt và chạy project](#8-cài-đặt-và-chạy-project)
- [9. Tài khoản và dữ liệu mẫu](#9-tài-khoản-và-dữ-liệu-mẫu)
- [10. Cấu hình ứng dụng](#10-cấu-hình-ứng-dụng)
- [11. Migration và database](#11-migration-và-database)
- [12. Controller và endpoint](#12-controller-và-endpoint)
- [13. SignalR và thông báo realtime](#13-signalr-và-thông-báo-realtime)
- [14. Quản lý hình ảnh](#14-quản-lý-hình-ảnh)
- [15. Báo cáo và xuất Excel](#15-báo-cáo-và-xuất-excel)
- [16. Kiểm thử và kiểm tra build](#16-kiểm-thử-và-kiểm-tra-build)
- [17. Triển khai production](#17-triển-khai-production)
- [18. Hạn chế hiện tại và hướng phát triển](#18-hạn-chế-hiện-tại-và-hướng-phát-triển)
- [19. Xử lý lỗi thường gặp](#19-xử-lý-lỗi-thường-gặp)

## 1. Chức năng chính

### 1.1. Tài khoản

- Đăng ký tài khoản người thuê.
- Đăng nhập bằng username và password.
- Mật khẩu được băm bằng BCrypt.
- Duy trì phiên đăng nhập bằng cookie.
- Tùy chọn ghi nhớ đăng nhập trong 7 ngày.
- Đăng xuất và xử lý trang không đủ quyền truy cập.

### 1.2. Dashboard

Dashboard được hiển thị khác nhau theo vai trò.

Admin có thể xem:

- Tổng số phòng, phòng trống, phòng đang chờ duyệt và phòng đã thuê.
- Số yêu cầu thuê đang chờ duyệt.
- Số hợp đồng đang hiệu lực.
- Biểu đồ trạng thái yêu cầu thuê.
- Thống kê hợp đồng sắp hết hạn trong 30, 60 và 90 ngày.
- Các lối tắt đến quản lý phòng, hợp đồng và lịch thuê.

User có thể xem:

- Hợp đồng hiện tại và thời gian còn lại.
- Giá thuê, tiền cọc và ngày thanh toán.
- Các yêu cầu thuê gần đây.
- Số yêu cầu đang chờ duyệt và số phòng đang trống.

### 1.3. Quản lý phòng

- Xem danh sách phòng bằng DevExtreme DataGrid.
- Admin tạo, sửa và xóa phòng.
- Quản lý tên, giá, diện tích, địa chỉ, phường/quận, tỉnh/thành, mô tả và trạng thái.
- Upload nhiều hình ảnh, chọn ảnh đại diện và xóa ảnh.
- Tìm kiếm theo khoảng giá, địa chỉ, phường/quận và tỉnh/thành.
- Cập nhật danh sách phòng theo thời gian thực qua SignalR.

### 1.4. Yêu cầu thuê

- User gửi yêu cầu thuê một phòng đang trống.
- Có thể nhập ghi chú và khoảng thời gian muốn thuê.
- Một User không thể tạo hai yêu cầu đang chờ cho cùng một phòng.
- Khi có yêu cầu, phòng chuyển từ `Trống` sang `Đang chờ duyệt`.
- User có thể hủy yêu cầu khi yêu cầu vẫn đang chờ.
- Admin có thể duyệt, từ chối hoặc duyệt đồng thời tạo hợp đồng.
- Khi yêu cầu bị hủy hoặc từ chối, phòng trở lại trạng thái `Trống`.

### 1.5. Hợp đồng

- Admin tạo hợp đồng trực tiếp hoặc từ yêu cầu thuê.
- Giá thuê ban đầu được lấy từ giá hiện tại của phòng.
- Quản lý ngày bắt đầu/kết thúc, tiền thuê, tiền cọc, ngày thanh toán, phí quản lý, phí gửi xe, chỉ số điện/nước đầu kỳ và ghi chú.
- Khi tạo hợp đồng, phòng chuyển sang `Đã thuê`.
- Có thể gia hạn, thay đổi giá thuê và kết thúc hợp đồng.
- Khi kết thúc hợp đồng, phòng trở lại `Trống`.
- Có lịch thuê theo phòng/tháng và DataGrid hỗ trợ phân trang, lọc, sắp xếp.
- Có chức năng tạo dữ liệu thử nghiệm đến 10.000 hợp đồng.

### 1.6. Bảo trì và sửa chữa

- User chỉ gửi được yêu cầu khi có hợp đồng đang hiệu lực theo ngày.
- Yêu cầu được gắn tự động với phòng đang thuê.
- Lưu tiêu đề, hạng mục, mô tả và mức ưu tiên.
- Admin xem toàn bộ yêu cầu, cập nhật trạng thái và ghi chú xử lý.
- Hệ thống lưu thời điểm tạo, cập nhật và hoàn thành.

### 1.7. Thông báo

- Admin gửi thông báo cho một hoặc tất cả người thuê.
- Loại thông báo gồm thông báo chung, hợp đồng và cắt điện/nước.
- Có thể thiết lập thời gian dự kiến áp dụng.
- User đánh dấu từng thông báo hoặc toàn bộ là đã đọc.
- Badge hiển thị số thông báo chưa đọc.
- SignalR hiển thị toast khi có thông báo mới.

### 1.8. Điện nước

- Admin nhập hoặc cập nhật lượng điện và nước theo phòng, theo tháng.
- Mỗi phòng chỉ có một bản ghi cho một tháng.
- User chỉ xem được dữ liệu của phòng có hợp đồng đang hiệu lực.
- Hiển thị tối đa 24 bản ghi và biểu đồ 12 tháng gần nhất.

### 1.9. Báo cáo

- Báo cáo hợp đồng theo năm, tháng và trạng thái hiệu lực.
- Báo cáo doanh thu ước tính theo năm.
- Biểu đồ doanh thu theo tháng và top phòng doanh thu cao.
- Xuất báo cáo hợp đồng và doanh thu ra `.xlsx` bằng ClosedXML.

> Doanh thu hiện tại được ước tính từ tiền thuê tháng và số tháng hợp đồng giao với kỳ báo cáo. Hệ thống chưa lưu giao dịch thanh toán thực tế.

## 2. Công nghệ sử dụng

| Thành phần | Công nghệ | Mục đích |
|---|---|---|
| Backend | ASP.NET Core MVC 8 | Controller, Razor View, middleware và DI |
| ORM | Entity Framework Core 8 | Truy cập dữ liệu và migration Code First |
| Database | SQL Server / LocalDB | Lưu dữ liệu nghiệp vụ |
| Authentication | Cookie Authentication | Xác thực và phân quyền theo role |
| Password | BCrypt.Net-Next 4.1.0 | Băm và kiểm tra mật khẩu |
| Realtime | ASP.NET Core SignalR | Đồng bộ danh sách và thông báo tức thời |
| DataGrid | DevExtreme | Danh sách phòng, yêu cầu và hợp đồng |
| UI | Razor, CoreUI 5, Bootstrap, Font Awesome | Giao diện responsive |
| Chart | Chart.js | Dashboard, báo cáo và điện nước |
| Alert | SweetAlert2 | Xác nhận và thông báo |
| Excel | ClosedXML 0.102.2 | Xuất báo cáo `.xlsx` |

## 3. Kiến trúc hệ thống

```text
Razor Views / JavaScript
          |
          v
      Controllers
          |
          +-----------> Services
          |                |
          +----------------+
                           v
                     AppDbContext
                           |
                           v
                    SQL Server/LocalDB
```

- `Views`: hiển thị giao diện và thực hiện request AJAX/fetch.
- `Controllers`: nhận request, kiểm tra quyền, trả HTML/JSON/file.
- `Services`: logic chính của phòng, yêu cầu thuê và hợp đồng.
- `AppDbContext`: ánh xạ entity, quan hệ và quy tắc xóa dữ liệu.
- `Models`: entity và enum được lưu trong database.
- `ViewModels`: dữ liệu dành riêng cho form và giao diện.
- `NotificationHub`: đầu mối SignalR.

Các tính năng bảo trì, thông báo và điện nước hiện dùng `AppDbContext` trực tiếp trong controller; các module phòng, yêu cầu thuê và hợp đồng dùng service.

## 4. Phân quyền

| Chức năng | Admin | User |
|---|:---:|:---:|
| Đăng ký, đăng nhập | Có | Có |
| Xem dashboard | Có | Có |
| Xem và tìm phòng | Có | Có |
| Tạo/sửa/xóa phòng và ảnh | Có | Không |
| Gửi/hủy yêu cầu thuê | Không | Có |
| Xem tất cả và xử lý yêu cầu | Có | Không |
| Quản lý hợp đồng và lịch thuê | Có | Không |
| Gửi yêu cầu sửa chữa | Không | Có |
| Xử lý yêu cầu sửa chữa | Có | Không |
| Gửi thông báo | Có | Không |
| Đọc/đánh dấu thông báo | Không | Có |
| Nhập điện nước | Có | Không |
| Xem điện nước | Có | Có, giới hạn theo phòng đang thuê |
| Xem và xuất báo cáo | Có | Không |

Phân quyền được kiểm soát bằng `[Authorize]` và `[Authorize(Roles = "...")]` tại controller/action.

## 5. Luồng nghiệp vụ

### 5.1. Trạng thái phòng

| Giá trị | Enum | Ý nghĩa |
|---:|---|---|
| 0 | `Trong` | Có thể nhận yêu cầu thuê |
| 1 | `DaThue` | Đã có hợp đồng |
| 2 | `DangChoDuyet` | Có yêu cầu đang chờ xử lý |

```text
Trống
  |
  | User gửi yêu cầu
  v
Đang chờ duyệt
  |                    |
  | Duyệt và tạo HĐ    | Hủy hoặc từ chối
  v                    v
Đã thuê              Trống
  |
  | Kết thúc hợp đồng
  v
Trống
```

### 5.2. Trạng thái yêu cầu thuê

| Giá trị | Enum | Ý nghĩa |
|---:|---|---|
| 0 | `ChoDuyet` | Chờ Admin xử lý |
| 1 | `DaDuyet` | Đã được duyệt |
| 2 | `TuChoi` | Admin từ chối |
| 3 | `DaHuy` | User tự hủy |

### 5.3. Trạng thái bảo trì

| Giá trị | Enum | Ý nghĩa |
|---:|---|---|
| 0 | `Moi` | Yêu cầu mới |
| 1 | `DangXuLy` | Đang xử lý |
| 2 | `HoanThanh` | Đã hoàn thành |
| 3 | `TuChoi` | Từ chối xử lý |

Mức ưu tiên gồm `Thap`, `BinhThuong`, `Cao` và `KhanCap`.

## 6. Mô hình dữ liệu

```text
AppUser 1 ─────── n RentalRequest n ─────── 1 Room
AppUser 1 ─────── n Contract      n ─────── 1 Room
AppUser 1 ─────── n Maintenance   n ─────── 1 Room
AppUser 1 ─────── n UserNotification
Room    1 ─────── n RoomImage
Room    1 ─────── n UtilityReading
```

### `Users`

- `Username` và `Email` có unique index.
- Lưu password hash, họ tên, số điện thoại, role và ngày tạo.

### `Rooms`

- Tên, giá, diện tích, địa chỉ, phường/quận, tỉnh/thành.
- Trạng thái, mô tả và ngày tạo.

### `RoomImages`

- Liên kết `RoomId`, tên file, cờ ảnh chính và ngày upload.
- Bị xóa theo khi phòng bị xóa.

### `RentalRequests`

- Liên kết phòng/người gửi, ghi chú hai phía, ngày thuê mong muốn, trạng thái và ngày tạo.
- Quan hệ phòng/User dùng `DeleteBehavior.Restrict`.

### `Contracts`

- Liên kết phòng/người thuê, thời hạn, tiền thuê, tiền cọc và các loại phí.
- Lưu ngày thanh toán, chỉ số điện/nước đầu kỳ, ghi chú và `IsActive`.
- Quan hệ phòng/User dùng `DeleteBehavior.Restrict`.

### `MaintenanceRequests`

- Liên kết User/phòng, nội dung sự cố, ưu tiên, trạng thái và ghi chú xử lý.
- Lưu thời điểm tạo, cập nhật và hoàn thành.

### `UserNotifications`

- Liên kết User, loại, tiêu đề, nội dung, thời gian áp dụng, ngày tạo và ngày đọc.
- Bị xóa theo khi User bị xóa.

### `UtilityReadings`

- Liên kết phòng, tháng ghi nhận, điện, nước, ghi chú và thời điểm cập nhật.
- Unique index trên `(RoomId, BillingMonth)`.
- Bị xóa theo khi phòng bị xóa.

## 7. Cấu trúc source code

```text
Source Code/
├── Controllers/                 # Controller MVC và endpoint JSON
├── Data/
│   ├── AppDbContext.cs          # EF Core context và quan hệ
│   └── DbSeeder.cs              # Dữ liệu development mẫu
├── DTOs/ContractRow.cs          # Dữ liệu phẳng cho DataGrid hợp đồng
├── Hubs/NotificationHub.cs      # SignalR Hub
├── Migrations/                  # EF Core migrations
├── Models/                      # Entity, enum
│   └── ViewModels/              # Model dành cho form/giao diện
├── Services/
│   ├── ContractService.cs
│   ├── RentalRequestService.cs
│   └── RoomService.cs
├── Views/                       # Razor Views theo controller
├── wwwroot/
│   ├── css/
│   ├── images/rooms/            # Ảnh phòng upload
│   ├── js/
│   └── lib/                     # Thư viện frontend local
├── appsettings.json
├── Program.cs
├── QuanLyPhongTro.csproj
└── QuanLyPhongTro.sln
```

## 8. Cài đặt và chạy project

### 8.1. Yêu cầu

- .NET 8 SDK, không chỉ .NET 8 Runtime.
- SQL Server, SQL Server Express hoặc SQL Server LocalDB.
- Internet để restore NuGet và tải thư viện giao diện từ CDN.
- Visual Studio 2022, Rider hoặc VS Code là tùy chọn.

Kiểm tra SDK:

```bash
dotnet --list-sdks
```

Danh sách cần có một phiên bản `8.0.x`.

### 8.2. Mở source

```bash
git clone <repository-url>
cd "Source Code"
```

Mở `QuanLyPhongTro.sln` hoặc chạy lệnh trong thư mục chứa `QuanLyPhongTro.csproj`.

### 8.3. Cấu hình database

Mặc định trong `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=QuanLyPhongTroDB_v2;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

SQL Server Express:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=QuanLyPhongTroDB_v2;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

SQL Authentication:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=QuanLyPhongTroDB_v2;User Id=<username>;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

Không commit mật khẩu thật. Với production, dùng environment variable sau hoặc secret store:

```text
ConnectionStrings__DefaultConnection
```

### 8.4. Restore, build và chạy

```bash
dotnet restore
dotnet build
dotnet run --launch-profile https
```

URL mặc định:

- HTTPS: `https://localhost:7289`
- HTTP: `http://localhost:5105`
- IIS Express: `https://localhost:44318` hoặc `http://localhost:29682`

Ứng dụng mặc định mở `Dashboard/Index`; người chưa đăng nhập được chuyển đến `Account/Login`.

Hot reload:

```bash
dotnet watch run --launch-profile https
```

Tin cậy chứng chỉ HTTPS development:

```bash
dotnet dev-certs https --trust
```

## 9. Tài khoản và dữ liệu mẫu

Khi database chưa có User nào, `DbSeeder` tự tạo:

| Username | Password | Role | Người dùng |
|---|---|---|---|
| `admin` | `admin123` | Admin | Quản trị viên |
| `user1` | `user123` | User | Nguyễn Văn A |
| `user2` | `user123` | User | Trần Thị B |

Ngoài ra có 5 phòng, 2 hợp đồng đang hoạt động và 1 yêu cầu thuê mẫu. Seeder chỉ chạy khi bảng `Users` hoàn toàn trống.

> Đây là dữ liệu development. Phải đổi hoặc loại bỏ tài khoản mặc định trước khi triển khai thật.

## 10. Cấu hình ứng dụng

### Dependency injection

`Program.cs` đăng ký MVC, `AppDbContext`, Cookie Authentication, SignalR và ba service nghiệp vụ theo vòng đời scoped.

### Cookie Authentication

- Login: `/Account/Login`.
- Logout: `/Account/Logout`.
- Access denied: `/Account/AccessDenied`.
- Phiên mặc định: 8 giờ.
- Remember me: 7 ngày.
- Claims: ID, username, role và họ tên.

### Middleware và route

```text
Exception handler/HSTS
HTTPS redirection
Static files
Routing
Authentication
Authorization
Controller routes
SignalR Hub
```

Route MVC mặc định:

```text
{controller=Dashboard}/{action=Index}/{id?}
```

SignalR Hub: `/notificationHub`.

## 11. Migration và database

Khi khởi động, ứng dụng gọi `Database.MigrateAsync()` rồi `DbSeeder.SeedAsync()`. Lần chạy đầu có thể tự tạo/cập nhật database và seed dữ liệu.

Cài EF Core CLI:

```bash
dotnet tool install --global dotnet-ef --version 8.0.0
```

Lệnh thường dùng:

```bash
dotnet ef migrations list
dotnet ef migrations add TenMigration
dotnet ef database update
dotnet ef migrations remove
dotnet ef migrations script --idempotent --output migration.sql
```

Các migration hiện có:

1. `InitialCreate`
2. `AddPhoneNumberToUser`
3. `AddDesiredDatesToRentalRequest`
4. `AddDesiredDatesToRentalRequestv2`
5. `AddRoomStatusDangChoDuyet`
6. `AddRoomWardCity`
7. `AddRoomImages`
8. `AddTenantMaintenanceNotificationsAndUtilities`

## 12. Controller và endpoint

Project dùng conventional routing. Các POST thay đổi dữ liệu đều có anti-forgery token.

### Account và Dashboard

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| GET/POST | `/Account/Login` | Public | Đăng nhập |
| GET/POST | `/Account/Register` | Public | Đăng ký User |
| POST | `/Account/Logout` | Đã đăng nhập | Đăng xuất |
| GET | `/Account/AccessDenied` | Public | Không đủ quyền |
| GET | `/Dashboard/Index` | Đã đăng nhập | Dashboard theo role |

### Room

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| GET | `/Room/Index`, `/Room/GetAll` | Đã đăng nhập | Trang và JSON danh sách |
| GET | `/Room/SearchPage`, `/Room/Search` | Đã đăng nhập | Giao diện và API tìm kiếm |
| POST | `/Room/Create` | Admin | Tạo phòng |
| GET/POST | `/Room/Edit/{id}` | Admin | Sửa phòng |
| POST | `/Room/Delete/{id}` | Admin | Xóa phòng |
| GET | `/Room/GetImages` | Admin | Lấy ảnh phòng |
| POST | `/Room/UploadImages` | Admin | Upload ảnh |
| POST | `/Room/DeleteImage` | Admin | Xóa ảnh |
| POST | `/Room/SetMainImage` | Admin | Chọn ảnh đại diện |

### RentalRequest

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| GET | `/RentalRequest/Index`, `/RentalRequest/GetAll` | Đã đăng nhập | Trang và dữ liệu theo role |
| POST | `/RentalRequest/Create` | User | Gửi yêu cầu |
| POST | `/RentalRequest/Cancel/{id}` | User | Hủy yêu cầu đang chờ |
| POST | `/RentalRequest/ApproveAndCreateContract` | Admin | Duyệt và tạo hợp đồng |
| POST | `/RentalRequest/Approve/{id}` | Admin | Đánh dấu đã duyệt |
| POST | `/RentalRequest/Reject/{id}` | Admin | Từ chối |

### Contract - chỉ Admin

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/Contract/Index`, `/Contract/GetAll` | Trang và JSON DataGrid |
| GET/POST | `/Contract/Create` | Tạo hợp đồng |
| GET | `/Contract/Details/{id}` | Chi tiết |
| POST | `/Contract/Terminate/{id}` | Kết thúc hợp đồng |
| POST | `/Contract/Renew` | Gia hạn |
| GET | `/Contract/CalendarView`, `/Contract/GetCalendarData` | Lịch thuê và dữ liệu lịch |
| POST | `/Contract/SeedData` | Seed đến 10.000 hợp đồng thử nghiệm |

### Maintenance, Notification và Utility

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| GET | `/Maintenance/Index` | Đã đăng nhập | Danh sách theo role |
| POST | `/Maintenance/Create` | User | Gửi yêu cầu sửa chữa |
| POST | `/Maintenance/UpdateStatus` | Admin | Cập nhật xử lý |
| GET | `/Notification/Index` | Đã đăng nhập | Thông báo theo role |
| POST | `/Notification/Create` | Admin | Gửi thông báo |
| POST | `/Notification/MarkRead`, `/Notification/MarkAllRead` | User | Đánh dấu đã đọc |
| GET | `/Notification/UnreadCount` | User | Số chưa đọc |
| GET | `/Utility/Index` | Đã đăng nhập | Xem điện nước |
| POST | `/Utility/Save` | Admin | Lưu số liệu tháng |

### Report - chỉ Admin

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/Report/Index` | Trang chọn báo cáo |
| GET | `/Report/RentalReport` | Báo cáo hợp đồng |
| GET | `/Report/ExportRental` | Xuất Excel hợp đồng |
| GET | `/Report/RevenueReport` | Doanh thu ước tính |
| GET | `/Report/ExportRevenue` | Xuất Excel doanh thu |

## 13. SignalR và thông báo realtime

Client kết nối `/notificationHub` và tự reconnect. Các event:

| Event | Nơi phát | Tác dụng |
|---|---|---|
| `reloadRooms` | Tạo/sửa/xóa phòng | Reload danh sách phòng |
| `newRequest` | User gửi yêu cầu | Toast yêu cầu thuê mới |
| `ownerNotification` | Admin gửi thông báo | Toast và cập nhật badge chưa đọc |

## 14. Quản lý hình ảnh

Ảnh được lưu tại `wwwroot/images/rooms/`.

- Định dạng: `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`.
- Tối đa 10 MB/file.
- Tên file: `{roomId}_{guid}.{extension}`.
- Ảnh đầu tiên tự thành ảnh chính.
- Khi xóa ảnh chính, ảnh cũ nhất còn lại được chọn thay thế.
- Chỉ Admin được quản lý ảnh.

Khi chạy nhiều server/container, nên chuyển ảnh sang shared/object storage.

## 15. Báo cáo và xuất Excel

Báo cáo hợp đồng lọc theo năm, tháng và trạng thái. File Excel chứa phòng, địa chỉ, người thuê, số điện thoại, ngày hợp đồng, tiền thuê, tiền cọc và trạng thái.

Doanh thu tháng được tính:

```text
Doanh thu tháng = Tổng MonthlyRent của hợp đồng giao với tháng

Điều kiện giao với tháng:
StartDate <= ngày cuối tháng và EndDate >= ngày đầu tháng
```

Đây không phải doanh thu đã thu vì chưa có bảng hóa đơn, kỳ thanh toán hoặc giao dịch.

## 16. Kiểm thử và kiểm tra build

Unit test và integration test cho module báo incident nằm trong `Tests/IncidentReporting/`.
Integration test dùng TestServer và HTTP handler giả lập, không chạy migration/seed hoặc kết nối database hiện tại.

Kiểm tra tối thiểu:

```bash
dotnet restore
dotnet build
dotnet test QuanLyPhongTro.sln --no-build
```

Checklist thủ công:

1. Đăng ký và đăng nhập User.
2. Admin tạo phòng, upload và đổi ảnh chính.
3. User tìm phòng, gửi rồi hủy yêu cầu.
4. Admin duyệt yêu cầu và tạo hợp đồng.
5. Xác nhận trạng thái phòng thay đổi đúng.
6. User gửi yêu cầu bảo trì; Admin xử lý.
7. Admin gửi thông báo; User đánh dấu đã đọc.
8. Admin nhập điện nước; User kiểm tra biểu đồ.
9. Admin gia hạn và kết thúc hợp đồng.
10. Xuất và mở kiểm tra hai loại file Excel.

### 16.1. NMV Auto Incident Reporting

Cấu hình `AgentPlatform` trong `appsettings.json` bật báo unhandled application exception của project `ROOM`.
`ControlPlaneUrl` cần trỏ tới Control Plane đang hoạt động. Khi URL trống, app vẫn chạy, worker log warning và tạm ngừng reporting.
Điền/cập nhật URL Control Plane rồi khởi động lại app để áp dụng cấu hình reporting.
Middleware chỉ capture dữ liệu chẩn đoán, enqueue và ném lại exception; trang lỗi `/Home/Error` hiện tại tiếp tục xử lý response.
Worker gửi JSON tới `{ControlPlaneUrl}/api/incidents` bằng HttpClientFactory, timeout mặc định 10 giây mỗi lần.
Lỗi mạng, timeout, HTTP 408/429/5xx được thử lại tối đa 3 lần, với backoff 1 và 2 giây. HTTP 4xx khác và redirect không được retry.
Reporting không chuyển tiếp Authorization/Cookie từ request, không đọc request body/form, không gửi query string.
Dữ liệu chẩn đoán được che secret đã cấu hình/nhận diện, email và số điện thoại.

Queue giữ tối đa 100 incident trong RAM (`AgentPlatform:QueueCapacity`). Khi đầy, middleware log warning và bỏ incident mới thay vì chờ.
Queue không bền vững qua restart. Worker chỉ throttle sau khi gửi thành công, theo ErrorType + Endpoint + stack frame ứng dụng đầu tiên,
với cửa sổ mặc định 10 giây (`AgentPlatform:ThrottleWindowSeconds`). Control Plane tiếp tục deduplicate chính.
`OperationCanceledException` và `TaskCanceledException` không được báo; có thể thay `IIncidentReportFilter` nếu bổ sung business validation exception.
Hai field tùy chọn `AgentPlatform:DeploymentVersion` và `AgentPlatform:GitCommit` có thể được cấp từ cấu hình deployment.

Test khi chạy profile Development:

```powershell
dotnet run --launch-profile http
```

Mở `http://localhost:5105/dev/test-agent-incident` hoặc, với profile `https`, `https://localhost:7289/dev/test-agent-incident`.
HTTP 500 với message `NMV Agent Platform test incident` là kết quả cố tình gây lỗi; worker sẽ gửi incident ở nền.
Endpoint không được đăng ký trong Production/Staging. Đây là báo lỗi .NET server; lỗi JavaScript trong trình duyệt không đi qua middleware này.

Environment gửi sang Control Plane lấy từ `AgentPlatform:Environment`, mặc định `Production` theo cấu hình tích hợp,
kể cả khi test trên Development. Có thể override thành `Development` để phân biệt incident test.
AgentJob và Telegram phụ thuộc triage/AutoFixEligible trên Control Plane; triage hiện tại xếp `InvalidOperationException` vào `NeedMoreInfo`.
Không cần POST thủ công tới `/api/incidents`.

Để tắt reporting, đặt `AgentPlatform:Enabled=false` hoặc `AgentPlatform:AutoReportIncidents=false`, rồi khởi động lại app.
Nếu Control Plane không truy cập được, kiểm tra warning của `IncidentReporter` và URL ngrok; request ứng dụng vẫn được xử lý như trước.

## 17. Triển khai production

Trước khi triển khai:

1. Dùng secret/environment variable cho connection string.
2. Xóa hoặc thay toàn bộ tài khoản mẫu.
3. Không cho phép endpoint `Contract/SeedData` trong production.
4. Bật HTTPS, logging, giám sát và backup.
5. Sao lưu cả database và thư mục ảnh.
6. Cân nhắc chạy migration bằng SQL script thay vì tự migrate khi startup.
7. Cài .NET 8 ASP.NET Core Runtime trên server.
8. Kiểm tra WebSocket tại reverse proxy cho SignalR.
9. Self-host dependency CDN nếu hệ thống cần chạy offline.
10. Bổ sung chính sách mật khẩu, rate limiting và khóa đăng nhập.

Publish:

```bash
dotnet publish -c Release -o ./publish
dotnet ./publish/QuanLyPhongTro.dll
```

## 18. Hạn chế hiện tại và hướng phát triển

### Hạn chế kỹ thuật

- Automated test hiện chỉ bao phủ module incident reporting; các luồng nghiệp vụ vẫn cần kiểm thử thủ công.
- Chưa dùng ASP.NET Core Identity; xác thực và User đang tự quản lý.
- Chưa có quên/đổi mật khẩu, xác thực email, khóa tài khoản hoặc chống brute force.
- `IsActive` không tự đổi khi `EndDate` đã qua.
- Một số màn hình chỉ kiểm tra `IsActive`, một số kiểm tra thêm khoảng ngày.
- API hợp đồng tải toàn bộ dữ liệu vào bộ nhớ trước khi lọc/sắp xếp.
- Endpoint duyệt riêng chỉ đổi trạng thái yêu cầu; nên ưu tiên duyệt kèm tạo hợp đồng.
- Event yêu cầu thuê mới hiện phát cho toàn bộ SignalR client thay vì chỉ Admin.
- Dashboard User liên kết đến chi tiết hợp đồng nhưng `ContractController` chỉ cho Admin.
- Kiến trúc service chưa được áp dụng đồng nhất cho mọi module.
- Giao diện phụ thuộc nhiều CDN.

### Hướng phát triển

- Hóa đơn tiền phòng, điện, nước và dịch vụ theo tháng.
- Theo dõi thanh toán, công nợ và biên lai.
- Đơn giá điện/nước theo kỳ.
- Job tự cập nhật hợp đồng hết hạn và gửi nhắc hạn.
- Upload ảnh cho yêu cầu bảo trì.
- Quản lý nhiều người ở cùng phòng.
- Lịch sử thay đổi hợp đồng và giá thuê.
- Phân quyền chi tiết hơn.
- Tìm kiếm không dấu, phân trang/lọc hoàn toàn tại database.
- Object storage cho file.
- Unit test, integration test và CI/CD.

## 19. Xử lý lỗi thường gặp

### `NETSDK1127` hoặc thiếu targeting pack

Project target `net8.0`, cần cài đầy đủ .NET 8 SDK. Chỉ có runtime hoặc chỉ có SDK .NET 9/10 có thể chưa đủ targeting pack.

```bash
dotnet --info
dotnet --list-sdks
```

### Không kết nối được LocalDB

```powershell
sqllocaldb info
sqllocaldb info mssqllocaldb
```

Nếu không dùng LocalDB, đổi `DefaultConnection` sang SQL Server/SQL Express đang chạy.

### Database chưa đồng bộ

```bash
dotnet ef migrations list
dotnet ef database update
```

Không xóa database production khi chưa sao lưu.

### Giao diện hoặc biểu đồ không tải

Kiểm tra Internet và tab Console/Network vì CoreUI, Font Awesome, DevExtreme, SignalR client, SweetAlert2 và Chart.js được tải từ CDN.

### Không upload được ảnh

- Kiểm tra định dạng và giới hạn 10 MB.
- Tài khoản phải là Admin.
- Web process phải có quyền ghi `wwwroot/images/rooms`.

### Không nhận thông báo realtime

- Kiểm tra kết nối `/notificationHub` trong Network/WebSocket.
- Kiểm tra JavaScript console.
- Đảm bảo reverse proxy hỗ trợ WebSocket.

---

## Ghi chú dành cho người phát triển

- Namespace chính: `QuanLyPhongTro`.
- Target framework: `net8.0`.
- Database mặc định: `QuanLyPhongTroDB_v2`.
- Route mặc định: `/Dashboard/Index`.
- Thời gian hiện dùng `DateTime.Now`; khi triển khai nhiều múi giờ nên chuẩn hóa UTC.
- Giữ anti-forgery token cho mọi POST thay đổi dữ liệu.
- Khi thêm trường/entity, cập nhật model, view model, controller/service, Razor View và migration tương ứng.
