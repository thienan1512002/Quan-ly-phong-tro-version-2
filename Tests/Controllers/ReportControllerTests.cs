using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Controllers;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Tests.Controllers;

public class ReportControllerTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(6, 6)]
    [InlineData(12, 12)]
    public async Task RentalReport_UsesSelectedMonthAndFiltersContracts(int? month, int expectedMonth)
    {
        await using var context = CreateContext();
        context.Rooms.Add(new Room { Id = 1, Name = "Room" });
        context.Users.Add(new AppUser { Id = 1, Username = "tenant", Email = "tenant@example.test" });
        context.Contracts.AddRange(Enumerable.Range(1, 12).Select(m => new Contract
        {
            RoomId = 1,
            UserId = 1,
            StartDate = new DateTime(2026, m, 1),
            EndDate = new DateTime(2026, m, DateTime.DaysInMonth(2026, m)),
            MonthlyRent = 100,
            Deposit = 200
        }));
        await context.SaveChangesAsync();
        var controller = new ReportController(context);

        var result = Assert.IsType<ViewResult>(await controller.RentalReport(2026, month, null));

        Assert.Equal(expectedMonth, Assert.IsType<int>(result.ViewData["Month"]));
        var contracts = Assert.IsType<List<Contract>>(result.Model);
        Assert.Equal(expectedMonth == 0 ? 12 : 1, contracts.Count);
        if (expectedMonth > 0)
            Assert.Equal(expectedMonth, Assert.Single(contracts).StartDate.Month);
        Assert.Equal(contracts.Count, result.ViewData["TotalContracts"]);
        Assert.Equal(contracts.Count * 100m, result.ViewData["TotalRent"]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public async Task RentalReport_InvalidMonth_ReturnsBadRequest(int month)
    {
        await using var context = CreateContext();
        var controller = new ReportController(context);

        Assert.IsType<BadRequestObjectResult>(await controller.RentalReport(2026, month, null));
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
