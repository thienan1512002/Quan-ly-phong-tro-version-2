using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Hubs;
using QuanLyPhongTro.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// ĐĂNG KÝ SERVICES (Dependency Injection)
// =============================================

// 1. MVC với Views
builder.Services.AddControllersWithViews();

// 2. DbContext - kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// 4. SignalR - realtime
builder.Services.AddSignalR();

// 5. Đăng ký các Service (Scoped = tạo mới mỗi request HTTP)
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<RentalRequestService>();
builder.Services.AddScoped<ContractService>();

// =============================================
// CẤU HÌNH MIDDLEWARE PIPELINE
// =============================================

var app = builder.Build();

// Seed dữ liệu mẫu khi khởi động lần đầu
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Thứ tự quan trọng!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Đăng ký SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
