using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Server.Persistence;

namespace TicTacToe.Server.Tests;

public class LeaderboardRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private LeaderboardDbContext _db = null!;
    private LeaderboardRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<LeaderboardDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new LeaderboardDbContext(options);
        await _db.Database.EnsureCreatedAsync();
        _repo = new LeaderboardRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task IncrementWin_CreatesRow()
    {
        await _repo.IncrementWinAsync("alice");
        var top = await _repo.GetTopAsync(10);
        Assert.Single(top);
        Assert.Equal("alice", top[0].Nickname);
        Assert.Equal(1, top[0].Wins);
    }

    [Fact]
    public async Task Increment_AccumulatesPerNickname()
    {
        await _repo.IncrementWinAsync("alice");
        await _repo.IncrementWinAsync("alice");
        await _repo.IncrementLossAsync("alice");
        await _repo.IncrementDrawAsync("alice");

        var top = await _repo.GetTopAsync(10);
        Assert.Single(top);
        Assert.Equal(2, top[0].Wins);
        Assert.Equal(1, top[0].Losses);
        Assert.Equal(1, top[0].Draws);
    }

    [Fact]
    public async Task GetTop_OrdersByWinsDesc()
    {
        await _repo.IncrementWinAsync("alice");
        await _repo.IncrementWinAsync("bob");
        await _repo.IncrementWinAsync("bob");

        var top = await _repo.GetTopAsync(10);
        Assert.Equal(2, top.Count);
        Assert.Equal("bob", top[0].Nickname);
        Assert.Equal("alice", top[1].Nickname);
    }

    [Fact]
    public async Task IncrementWin_RejectsBlankNickname()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _repo.IncrementWinAsync("  "));
    }
}
