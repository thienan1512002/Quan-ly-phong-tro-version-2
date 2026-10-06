using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Tests.Services;

public class RoomServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteAsync_MissingRoom_ReturnsFalseAndPreservesOtherRooms(bool includeRoom100)
    {
        await using var context = CreateContext();
        if (includeRoom100)
        {
            context.Rooms.Add(new Room { Id = 100, Name = "Other room" });
            await context.SaveChangesAsync();
        }

        var result = await new RoomService(context).DeleteAsync(4);

        Assert.False(result);
        Assert.Equal(includeRoom100 ? 1 : 0, await context.Rooms.CountAsync());
        Assert.All(await context.Rooms.ToListAsync(), room => Assert.Equal(100, room.Id));
    }

    [Fact]
    public async Task DeleteAsync_ExistingRoom_DeletesRequestedRoomAndRepeatedDeleteReturnsFalse()
    {
        await using var context = CreateContext();
        context.Rooms.AddRange(
            new Room { Id = 4, Name = "Requested room" },
            new Room { Id = 100, Name = "Other room" });
        await context.SaveChangesAsync();
        var service = new RoomService(context);

        Assert.True(await service.DeleteAsync(4));
        Assert.Null(await context.Rooms.FindAsync(4));
        Assert.Equal(100, Assert.Single(await context.Rooms.ToListAsync()).Id);
        Assert.False(await service.DeleteAsync(4));
        Assert.Equal(100, Assert.Single(await context.Rooms.ToListAsync()).Id);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
