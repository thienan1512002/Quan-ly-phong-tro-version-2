using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Data
{
    /// <summary>
    /// Class khởi tạo dữ liệu mẫu cho database
    /// Chạy lần đầu khi database chưa có dữ liệu
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Chỉ seed nếu chưa có user nào trong DB
            if (context.Users.Any()) return;

            // Tạo tài khoản Admin
            var admin = new AppUser
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Email = "admin@phongtro.com",
                FullName = "Quản trị viên",
                Role = "Admin",
                CreatedAt = DateTime.Now
            };

            // Tạo tài khoản User mẫu
            var user1 = new AppUser
            {
                Username = "user1",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                Email = "user1@gmail.com",
                FullName = "Nguyễn Văn A",
                Role = "User",
                CreatedAt = DateTime.Now
            };

            var user2 = new AppUser
            {
                Username = "user2",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                Email = "user2@gmail.com",
                FullName = "Trần Thị B",
                Role = "User",
                CreatedAt = DateTime.Now
            };

            context.Users.AddRange(admin, user1, user2);
            await context.SaveChangesAsync();

            // Tạo phòng mẫu
            var rooms = new List<Room>
            {
                new Room
                {
                    Name = "Phòng 101",
                    Price = 2500000,
                    Area = 25,
                    Address = "123 Nguyễn Văn Cừ, Quận 5, TP.HCM",
                    Status = RoomStatus.Trong,
                    Description = "Phòng thoáng mát, có máy lạnh, gần trường đại học",
                    CreatedAt = DateTime.Now
                },
                new Room
                {
                    Name = "Phòng 102",
                    Price = 3000000,
                    Area = 30,
                    Address = "123 Nguyễn Văn Cừ, Quận 5, TP.HCM",
                    Status = RoomStatus.DaThue,
                    Description = "Phòng rộng, có gác lửng, ban công",
                    CreatedAt = DateTime.Now
                },
                new Room
                {
                    Name = "Phòng 201",
                    Price = 2000000,
                    Area = 20,
                    Address = "456 Lê Văn Việt, Quận 9, TP.HCM",
                    Status = RoomStatus.Trong,
                    Description = "Phòng nhỏ gọn, phù hợp sinh viên",
                    CreatedAt = DateTime.Now
                },
                new Room
                {
                    Name = "Phòng 202",
                    Price = 3500000,
                    Area = 35,
                    Address = "456 Lê Văn Việt, Quận 9, TP.HCM",
                    Status = RoomStatus.Trong,
                    Description = "Phòng VIP, đầy đủ nội thất",
                    CreatedAt = DateTime.Now
                },
                new Room
                {
                    Name = "Phòng 301",
                    Price = 1800000,
                    Area = 18,
                    Address = "789 Đinh Tiên Hoàng, Bình Thạnh, TP.HCM",
                    Status = RoomStatus.DaThue,
                    Description = "Phòng sinh viên, bình dân",
                    CreatedAt = DateTime.Now
                }
            };

            context.Rooms.AddRange(rooms);
            await context.SaveChangesAsync();

            // Tạo hợp đồng mẫu cho phòng đã thuê
            var contract1 = new Contract
            {
                RoomId = rooms[1].Id, // Phòng 102
                UserId = user1.Id,
                StartDate = DateTime.Now.AddMonths(-2),
                EndDate = DateTime.Now.AddMonths(4),
                MonthlyRent = rooms[1].Price,
                Note = "Hợp đồng 6 tháng",
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-2)
            };

            var contract2 = new Contract
            {
                RoomId = rooms[4].Id, // Phòng 301
                UserId = user2.Id,
                StartDate = DateTime.Now.AddMonths(-1),
                EndDate = DateTime.Now.AddMonths(5),
                MonthlyRent = rooms[4].Price,
                Note = "Hợp đồng 6 tháng",
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-1)
            };

            context.Contracts.AddRange(contract1, contract2);
            await context.SaveChangesAsync();

            // Tạo yêu cầu thuê mẫu
            var request = new RentalRequest
            {
                RoomId = rooms[0].Id, // Phòng 101
                UserId = user2.Id,
                Note = "Tôi muốn thuê phòng này từ tháng sau",
                Status = RequestStatus.ChoDuyet,
                CreatedAt = DateTime.Now
            };

            context.RentalRequests.Add(request);
            await context.SaveChangesAsync();
        }
    }
}
