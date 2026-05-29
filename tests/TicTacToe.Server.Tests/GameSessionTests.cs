using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Server.Game;
using TicTacToe.Server.Persistence;
using TicTacToe.Server.Tests.Helpers;
using TicTacToe.Shared.Logic;
using TicTacToe.Shared.Protocol;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Server.Tests;

public class GameSessionTests
{
    private static async Task DrainAsync(System.Net.Sockets.TcpClient client, int messageCount)
    {
        var stream = client.GetStream();
        for (var i = 0; i < messageCount; i++)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await MessageFramer.ReadAsync(stream, cts.Token);
        }
    }

    private static async Task<NetMessage?> ReadOneAsync(System.Net.Sockets.TcpClient client)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        return await MessageFramer.ReadAsync(client.GetStream(), cts.Token);
    }

    [Fact]
    public async Task AttachPlayerO_SendsGameStartedToBoth()
    {
        await using var x = await ConnectionPair.CreateAsync();
        await using var o = await ConnectionPair.CreateAsync();
        x.Server.Nickname = "alice";
        o.Server.Nickname = "bob";

        var session = new GameSession(Guid.NewGuid(), x.Server);
        await session.AttachPlayerOAsync(o.Server);

        var startedX = await ReadOneAsync(x.ClientSide);
        var startedO = await ReadOneAsync(o.ClientSide);

        Assert.Equal(MessageTypes.GameStarted, startedX!.Type);
        Assert.Equal(MessageTypes.GameStarted, startedO!.Type);

        var asX = ProtocolJson.FromElement<GameStartedMessage>(startedX.Payload)!;
        var asO = ProtocolJson.FromElement<GameStartedMessage>(startedO.Payload)!;
        Assert.Equal(CellState.X, asX.YourSymbol);
        Assert.Equal(CellState.O, asO.YourSymbol);
        Assert.Equal("bob", asX.OpponentNickname);
        Assert.Equal("alice", asO.OpponentNickname);
    }

    [Fact]
    public async Task TryMakeMove_RejectsWhenNotYourTurn()
    {
        await using var x = await ConnectionPair.CreateAsync();
        await using var o = await ConnectionPair.CreateAsync();
        x.Server.Nickname = "alice";
        o.Server.Nickname = "bob";

        var session = new GameSession(Guid.NewGuid(), x.Server);
        await session.AttachPlayerOAsync(o.Server);

        var ok = await session.TryMakeMoveAsync(o.Server, 0, 0);
        Assert.False(ok);
    }

    [Fact]
    public async Task TryMakeMove_AcceptsAlternatingTurns()
    {
        await using var x = await ConnectionPair.CreateAsync();
        await using var o = await ConnectionPair.CreateAsync();
        x.Server.Nickname = "alice";
        o.Server.Nickname = "bob";

        var session = new GameSession(Guid.NewGuid(), x.Server);
        await session.AttachPlayerOAsync(o.Server);

        Assert.True(await session.TryMakeMoveAsync(x.Server, 0, 0));
        Assert.True(await session.TryMakeMoveAsync(o.Server, 1, 1));
        Assert.False(await session.TryMakeMoveAsync(o.Server, 2, 2));
    }

    [Fact]
    public async Task XWin_UpdatesLeaderboard()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<LeaderboardDbContext>().UseSqlite(connection).Options;
        await using var db = new LeaderboardDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var repo = new LeaderboardRepository(db);

        await using var x = await ConnectionPair.CreateAsync();
        await using var o = await ConnectionPair.CreateAsync();
        x.Server.Nickname = "alice";
        o.Server.Nickname = "bob";

        var session = new GameSession(Guid.NewGuid(), x.Server, repo);
        await session.AttachPlayerOAsync(o.Server);

        await session.TryMakeMoveAsync(x.Server, 0, 0);
        await session.TryMakeMoveAsync(o.Server, 1, 0);
        await session.TryMakeMoveAsync(x.Server, 0, 1);
        await session.TryMakeMoveAsync(o.Server, 1, 1);
        await session.TryMakeMoveAsync(x.Server, 0, 2);

        var top = await repo.GetTopAsync(10);
        var alice = top.First(e => e.Nickname == "alice");
        var bob = top.First(e => e.Nickname == "bob");
        Assert.Equal(1, alice.Wins);
        Assert.Equal(0, alice.Losses);
        Assert.Equal(1, bob.Losses);
        Assert.Equal(0, bob.Wins);
    }

    [Fact]
    public async Task Rematch_ResetsBoardAndBroadcasts()
    {
        await using var x = await ConnectionPair.CreateAsync();
        await using var o = await ConnectionPair.CreateAsync();
        x.Server.Nickname = "alice";
        o.Server.Nickname = "bob";

        var session = new GameSession(Guid.NewGuid(), x.Server);
        await session.AttachPlayerOAsync(o.Server);

        await session.TryMakeMoveAsync(x.Server, 0, 0);
        await session.RematchAsync();

        Assert.True(await session.TryMakeMoveAsync(x.Server, 0, 0));
    }
}
