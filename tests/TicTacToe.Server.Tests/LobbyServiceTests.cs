using TicTacToe.Server.Game;
using TicTacToe.Server.Tests.Helpers;

namespace TicTacToe.Server.Tests;

public class LobbyServiceTests
{
    [Fact]
    public async Task CreateGame_AddsToList()
    {
        await using var p = await ConnectionPair.CreateAsync();
        p.Server.Nickname = "alice";
        var lobby = new LobbyService();

        var session = lobby.CreateGame(p.Server);

        Assert.NotEqual(Guid.Empty, session.GameId);
        var games = lobby.ListGames();
        Assert.Single(games);
        Assert.Equal("alice", games[0].CreatedBy);
    }

    [Fact]
    public async Task CreateGame_RequiresNickname()
    {
        await using var p = await ConnectionPair.CreateAsync();
        var lobby = new LobbyService();

        Assert.Throws<InvalidOperationException>(() => lobby.CreateGame(p.Server));
    }

    [Fact]
    public async Task CreateGame_TwiceBySameUser_Rejected()
    {
        await using var p = await ConnectionPair.CreateAsync();
        p.Server.Nickname = "alice";
        var lobby = new LobbyService();

        lobby.CreateGame(p.Server);
        Assert.Throws<InvalidOperationException>(() => lobby.CreateGame(p.Server));
    }

    [Fact]
    public async Task JoinGame_StartsSession()
    {
        await using var x = await ConnectionPair.CreateAsync();
        await using var o = await ConnectionPair.CreateAsync();
        x.Server.Nickname = "alice";
        o.Server.Nickname = "bob";
        var lobby = new LobbyService();

        var created = lobby.CreateGame(x.Server);
        var joined = await lobby.JoinGameAsync(created.GameId, o.Server);

        Assert.Same(created, joined);
        Assert.True(joined.IsStarted);
        Assert.Empty(lobby.ListGames());
    }

    [Fact]
    public async Task JoinGame_UnknownId_Throws()
    {
        await using var p = await ConnectionPair.CreateAsync();
        p.Server.Nickname = "alice";
        var lobby = new LobbyService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => lobby.JoinGameAsync(Guid.NewGuid(), p.Server));
    }

    [Fact]
    public async Task JoinGame_OwnGame_Rejected()
    {
        await using var p = await ConnectionPair.CreateAsync();
        p.Server.Nickname = "alice";
        var lobby = new LobbyService();
        var created = lobby.CreateGame(p.Server);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => lobby.JoinGameAsync(created.GameId, p.Server));
    }

    [Fact]
    public async Task RemoveGamesOf_RemovesAllSessionsForConnection()
    {
        await using var x = await ConnectionPair.CreateAsync();
        x.Server.Nickname = "alice";
        var lobby = new LobbyService();
        lobby.CreateGame(x.Server);

        var removed = lobby.RemoveGamesOf(x.Server);

        Assert.Single(removed);
        Assert.Empty(lobby.ListGames());
    }
}
