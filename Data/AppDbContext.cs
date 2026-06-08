using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Data
{
    /// <summary>
    /// DbContext chính của ứng dụng - quản lý kết nối DB và các Entity
    /// </summary>
    public class AppDbContext : DbContext
    {
        // Constructor nhận tham số từ DI (Dependency Injection)
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Các bảng trong database
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RentalRequest> RentalRequests { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<RoomImage> RoomImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình bảng User
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique(); // Username phải duy nhất
                entity.HasIndex(u => u.Email).IsUnique();    // Email phải duy nhất
            });

            // Cấu hình quan hệ Room - RoomImage
            modelBuilder.Entity<RoomImage>(entity =>
            {
                entity.HasOne(i => i.Room)
                      .WithMany(r => r.Images)
                      .HasForeignKey(i => i.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình quan hệ Room - RentalRequest
            modelBuilder.Entity<RentalRequest>(entity =>
            {
                entity.HasOne(r => r.Room)
                      .WithMany(rm => rm.RentalRequests)
                      .HasForeignKey(r => r.RoomId)
                      .OnDelete(DeleteBehavior.Restrict); // Không xóa phòng khi có yêu cầu

                entity.HasOne(r => r.User)
                      .WithMany(u => u.RentalRequests)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình quan hệ Room - Contract
            modelBuilder.Entity<Contract>(entity =>
            {
                entity.HasOne(c => c.Room)
                      .WithMany(r => r.Contracts)
                      .HasForeignKey(c => c.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.User)
                      .WithMany(u => u.Contracts)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
