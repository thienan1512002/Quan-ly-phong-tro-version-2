using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Controllers;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
namespace QuanLyPhongTro.Tests.Controllers;
public class UtilityControllerTests
{
    private static AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static UtilityController Controller(AppDbContext db, string role = "Admin", int userId = 1)
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test")) };
        return new UtilityController(db) { ControllerContext = new ControllerContext { HttpContext = http }, TempData = new TempDataDictionary(http, new MemoryTempDataProvider()) };
    }
    private class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
    private static async Task Seed(AppDbContext db)
    {
        db.Rooms.Add(new Room { Id = 1, Name = "Ph?ng" });
        db.Contracts.Add(new Contract { Id = 1, RoomId = 1, UserId = 1, StartDate = DateTime.Today.AddMonths(-2), EndDate = DateTime.Today.AddMonths(2), IsActive = true });
        await db.SaveChangesAsync();
    }
    private static UtilityReadingInputViewModel Input() => new() { RoomId = 1, BillingMonth = DateTime.Today, PreviousElectricityMeter = 100, CurrentElectricityMeter = 125, ElectricityUnitPrice = 3000, WaterUsage = 5 };
    [Fact]
    public async Task Save_CalculatesAndUpdatesSinglePeriod_CollectPersistsOnceAndLocksEditing()
    {
        await using var db = Context(); await Seed(db);
        var controller = Controller(db);
        Assert.IsType<RedirectToActionResult>(await controller.Save(Input()));
        Assert.IsType<RedirectToActionResult>(await controller.Save(Input()));
        var reading = Assert.Single(await db.UtilityReadings.ToListAsync());
        Assert.Equal(25m, reading.ElectricityUsage); Assert.Equal(75000m, reading.ElectricityAmount);
        Assert.Equal(1, reading.ContractId); Assert.Equal(5m, reading.WaterUsage);
        await controller.Collect(reading.Id); var paidAt = reading.ElectricityPaidAt;
        await controller.Collect(reading.Id); Assert.Equal(paidAt, reading.ElectricityPaidAt);
        Assert.IsType<ConflictObjectResult>(await controller.Save(Input()));
        db.ChangeTracker.Clear();
        Assert.Equal(paidAt, (await db.UtilityReadings.SingleAsync()).ElectricityPaidAt);
    }
    [Theory]
    [InlineData(-1, 125, 3000)] [InlineData(100, 99, 3000)] [InlineData(100, 125, -1)] [InlineData(100, 125, 0.001)]
    public async Task Save_RejectsInvalidValues(decimal oldMeter, decimal newMeter, decimal price)
    {
        await using var db = Context(); await Seed(db); var input = Input();
        input.PreviousElectricityMeter = oldMeter; input.CurrentElectricityMeter = newMeter; input.ElectricityUnitPrice = price;
        Assert.IsType<ViewResult>(await Controller(db).Save(input)); Assert.Empty(db.UtilityReadings);
    }
    [Fact]
    public async Task Save_RejectsVacantRoom_AndCollectRejectsLegacyReading()
    {
        await using var db = Context(); db.Rooms.Add(new Room { Id = 1, Name = "Room" }); await db.SaveChangesAsync();
        Assert.IsType<ViewResult>(await Controller(db).Save(Input()));
        db.UtilityReadings.Add(new UtilityReading { Id = 1, RoomId = 1, BillingMonth = DateTime.Today }); await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await Controller(db).Collect(1));
    }
    [Fact]
    public async Task Tenant_DoesNotSeeOtherContractsHistory()
    {
        await using var db = Context(); await Seed(db);
        db.Contracts.Add(new Contract { Id = 2, RoomId = 1, UserId = 2, IsActive = false });
        db.UtilityReadings.AddRange(new UtilityReading { RoomId = 1, ContractId = 1, BillingMonth = DateTime.Today }, new UtilityReading { RoomId = 1, ContractId = 2, BillingMonth = DateTime.Today.AddMonths(-1) }); await db.SaveChangesAsync();
        var model = Assert.IsType<UtilityIndexViewModel>(Assert.IsType<ViewResult>(await Controller(db, "User").Index(1)).Model);
        Assert.Equal(1, Assert.Single(model.Readings).ContractId);
    }
    [Fact]
    public async Task Collect_RejectsConcurrentPayment()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var first = new AppDbContext(options);
        await Seed(first); await Controller(first).Save(Input());
        await using var second = new AppDbContext(options);
        var stale = await second.UtilityReadings.SingleAsync();
        await Controller(first).Collect(stale.Id);
        Assert.IsType<ConflictObjectResult>(await Controller(second).Collect(stale.Id));
        second.ChangeTracker.Clear();
        Assert.Equal((await first.UtilityReadings.SingleAsync()).ElectricityPaidAt, (await second.UtilityReadings.SingleAsync()).ElectricityPaidAt);
    }
    [Fact]
    public void Model_PreservesUniqueRoomPeriodAndPaymentConcurrency()
    {
        using var db = Context();
        var entity = db.Model.FindEntityType(typeof(UtilityReading))!;
        Assert.Contains(entity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "RoomId", "BillingMonth" }));
        Assert.True(entity.FindProperty(nameof(UtilityReading.ElectricityPaidAt))!.IsConcurrencyToken);
        Assert.True(entity.FindProperty(nameof(UtilityReading.UpdatedAt))!.IsConcurrencyToken);
    }
}
