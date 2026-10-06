using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Controllers;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Tests.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task Index_User_ReturnsOnlyOwnRequestsAndActiveContract()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var result = await CreateController(context, "User", "7").Index();

        var model = Assert.IsType<DashboardViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.True(model.IsUser);
        Assert.Equal(1, model.PendingRequests);
        Assert.Equal(7, model.UserActiveContract!.UserId);
        Assert.Equal(2, model.UserRecentRequests.Count);
        Assert.All(model.UserRecentRequests, request => Assert.Equal(7, request.UserId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("2147483648")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Index_UserWithInvalidId_ReturnsChallenge(string? userId)
    {
        await using var context = CreateContext();

        Assert.IsType<ChallengeResult>(await CreateController(context, "User", userId).Index());
    }

    [Fact]
    public async Task Index_AdminWithoutId_ReturnsGlobalCounts()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var result = await CreateController(context, "Admin", null).Index();

        var model = Assert.IsType<DashboardViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.False(model.IsUser);
        Assert.Equal(2, model.PendingRequests);
        Assert.Equal(2, model.ActiveContracts);
        Assert.Equal(2, model.ReqChoDuyet);
        Assert.Equal(1, model.ReqDaDuyet);
        Assert.Null(model.UserActiveContract);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static DashboardController CreateController(AppDbContext context, string role, string? userId)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (userId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        return new DashboardController(new RoomService(context), new RentalRequestService(context),
            new ContractService(context))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            }
        };
    }

    private static async Task SeedAsync(AppDbContext context)
    {
        context.Rooms.Add(new Room { Id = 1, Name = "Room" });
        context.RentalRequests.AddRange(
            new RentalRequest { RoomId = 1, UserId = 7, Status = RequestStatus.ChoDuyet },
            new RentalRequest { RoomId = 1, UserId = 7, Status = RequestStatus.DaDuyet },
            new RentalRequest { RoomId = 1, UserId = 8, Status = RequestStatus.ChoDuyet });
        context.Contracts.AddRange(
            new Contract { RoomId = 1, UserId = 7, IsActive = true },
            new Contract { RoomId = 1, UserId = 8, IsActive = true });
        await context.SaveChangesAsync();
    }
}
