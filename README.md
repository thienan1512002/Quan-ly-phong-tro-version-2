# 🏠 Quản Lý Phòng Trọ

Hệ thống web quản lý cho thuê phòng trọ xây dựng bằng ASP.NET Core MVC (.NET 8).

---

## 📋 Công nghệ sử dụng

| Công nghệ                 | Mô tả                             |
| ------------------------- | --------------------------------- |
| ASP.NET Core MVC (.NET 8) | Framework chính                   |
| Entity Framework Core 8   | ORM, Code First                   |
| SQL Server / LocalDB      | Database                          |
| DevExtreme                | DataGrid hiển thị danh sách phòng |
| CoreUI 5                  | Template giao diện admin          |
| SignalR                   | Realtime notifications            |
| BCrypt.Net                | Mã hóa mật khẩu                   |
| Cookie Authentication     | Xác thực người dùng               |

---

## ⚡ Hướng dẫn chạy nhanh

### Yêu cầu

- .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
- SQL Server hoặc LocalDB (cài kèm Visual Studio)

### Bước 1: Clone / mở project

```bash
cd QuanLyPhongTro
```

### Bước 2: Cấu hình chuỗi kết nối database

Mở file `appsettings.json`, sửa `DefaultConnection`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=QuanLyPhongTroDB;Trusted_Connection=True;"
}
```

> Nếu dùng SQL Server Express:
>
> ```
> Server=.\\SQLEXPRESS;Database=QuanLyPhongTroDB;Trusted_Connection=True;
> ```

### Bước 3: Tạo database

```bash
dotnet ef database update
```

Lệnh này sẽ:

- Tạo database `QuanLyPhongTroDB`
- Tạo đầy đủ các bảng
- Seed dữ liệu mẫu tự động khi chạy lần đầu

### Bước 4: Chạy ứng dụng

```bash
dotnet run
```

Truy cập: `https://localhost:5001` hoặc `http://localhost:5000`

---

## 🔑 Tài khoản demo

| Username | Password   | Role  |
| -------- | ---------- | ----- |
| `admin`  | `admin123` | Admin |
| `user1`  | `user123`  | User  |
| `user2`  | `user123`  | User  |

---

## 📁 Cấu trúc project

```
QuanLyPhongTro/
├── Controllers/
│   ├── AccountController.cs      # Đăng nhập / đăng xuất
│   ├── DashboardController.cs    # Trang tổng quan
│   ├── RoomController.cs         # Quản lý phòng
│   ├── RentalRequestController.cs# Yêu cầu thuê
│   └── ContractController.cs     # Hợp đồng
├── Data/
│   ├── AppDbContext.cs           # EF Core DbContext
│   └── DbSeeder.cs               # Dữ liệu mẫu
├── Hubs/
│   └── NotificationHub.cs        # SignalR Hub
├── Models/
│   ├── AppUser.cs                # Entity người dùng
│   ├── Room.cs                   # Entity phòng trọ
│   ├── RentalRequest.cs          # Entity yêu cầu thuê
│   ├── Contract.cs               # Entity hợp đồng
│   └── ViewModels/
│       └── ViewModels.cs         # ViewModel (Login, Dashboard, v.v.)
├── Services/
│   ├── RoomService.cs            # Logic phòng
│   ├── RentalRequestService.cs   # Logic yêu cầu
│   └── ContractService.cs        # Logic hợp đồng
├── Views/
│   ├── Account/                  # Login, AccessDenied
│   ├── Dashboard/                # Trang chủ
│   ├── Room/                     # CRUD phòng, tìm kiếm
│   ├── RentalRequest/            # Danh sách yêu cầu
│   ├── Contract/                 # CRUD hợp đồng
│   └── Shared/                   # _Layout.cshtml
├── Program.cs                    # Entry point, cấu hình DI
└── appsettings.json              # Cấu hình app
```

---

## 🗄️ Database Schema

```
Users
├── Id (PK)
├── Username (UNIQUE)
├── PasswordHash
├── Email (UNIQUE)
├── FullName
├── Role (Admin/User)
└── CreatedAt

Rooms
├── Id (PK)
├── Name
├── Price
├── Area
├── Address
├── Status (0=Trống, 1=ĐãThuê)
├── Description
└── CreatedAt

RentalRequests
├── Id (PK)
├── RoomId (FK → Rooms)
├── UserId (FK → Users)
├── Note
├── Status (0=ChờDuyệt, 1=ĐãDuyệt, 2=TừChối)
├── AdminNote
└── CreatedAt

Contracts
├── Id (PK)
├── RoomId (FK → Rooms)
├── UserId (FK → Users)
├── StartDate
├── EndDate
├── MonthlyRent
├── Note
├── IsActive
└── CreatedAt
```

---

## 🛠️ Các lệnh hữu ích

```bash
# Tạo migration mới
dotnet ef migrations add TenMigration

# Cập nhật database
dotnet ef database update

# Xóa migration cuối (nếu chưa apply)
dotnet ef migrations remove

# Build
dotnet build

# Chạy
dotnet run

# Chạy với watch (tự reload khi thay đổi code)
dotnet watch run
```
