using Moq;
using TicTacToe.Client.Core.Networking;
using TicTacToe.Client.Core.Services;
using TicTacToe.Shared.Logic;
using TicTacToe.Shared.Protocol;
using TicTacToe.Shared.Protocol.Dtos;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.Tests.Services;

public class GameSessionServiceTests
{
    private readonly Mock<INetworkClient> _network = new(MockBehavior.Strict);
    private NetMessage? _captured;

    public GameSessionServiceTests()
    {
        _network.Setup(c => c.SendAsync(It.IsAny<NetMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<NetMessage, CancellationToken>((m, _) => _captured = m);
    }

    [Fact]
    public async Task SayHelloAsync_SendsHelloEnvelope()
    {
        using var session = new GameSessionService(_network.Object);

        await session.SayHelloAsync("alice");

        Assert.NotNull(_captured);
        Assert.Equal(MessageTypes.Hello, _captured!.Type);
        var payload = ProtocolJson.FromElement<HelloMessage>(_captured.Payload);
        Assert.Equal("alice", payload!.Nickname);
    }

    [Fact]
    public async Task CreateGameAsync_SendsCreateGameEnvelope()
    {
        using var session = new GameSessionService(_network.Object);

        await session.CreateGameAsync();

        Assert.Equal(MessageTypes.CreateGame, _captured!.Type);
    }

    [Fact]
    public async Task JoinGameAsync_SendsJoinGameWithId()
    {
        using var session = new GameSessionService(_network.Object);
        var id = Guid.NewGuid();

        await session.JoinGameAsync(id);

        Assert.Equal(MessageTypes.JoinGame, _captured!.Type);
        var payload = ProtocolJson.FromElement<JoinGameMessage>(_captured.Payload);
        Assert.Equal(id, payload!.GameId);
    }

    [Fact]
    public async Task MakeMoveAsync_SendsMoveWithCoordinates()
    {
        using var session = new GameSessionService(_network.Object);
        var id = Guid.NewGuid();

        await session.MakeMoveAsync(id, 1, 2);

        Assert.Equal(MessageTypes.MakeMove, _captured!.Type);
        var payload = ProtocolJson.FromElement<MakeMoveMessage>(_captured.Payload);
        Assert.Equal(id, payload!.GameId);
        Assert.Equal(1, payload.Row);
        Assert.Equal(2, payload.Col);
    }

    [Fact]
    public async Task RequestRematchAsync_SendsRematch()
    {
        using var session = new GameSessionService(_network.Object);
        var id = Guid.NewGuid();

        await session.RequestRematchAsync(id);

        Assert.Equal(MessageTypes.Rematch, _captured!.Type);
        var payload = ProtocolJson.FromElement<RematchMessage>(_captured.Payload);
        Assert.Equal(id, payload!.GameId);
    }

    [Fact]
    public async Task RequestLeaderboardAsync_SendsLeaderboardRequest()
    {
        using var session = new GameSessionService(_network.Object);

        await session.RequestLeaderboardAsync();

        Assert.Equal(MessageTypes.LeaderboardRequest, _captured!.Type);
    }

    [Fact]
    public void IncomingGameStarted_RaisesGameStartedEvent()
    {
        using var session = new GameSessionService(_network.Object);
        GameStartedMessage? received = null;
        session.GameStarted += (_, msg) => received = msg;

        var payload = new GameStartedMessage(Guid.NewGuid(), CellState.X, "bob");
        Raise(MessageTypes.GameStarted, payload);

        Assert.NotNull(received);
        Assert.Equal(payload.GameId, received!.GameId);
        Assert.Equal(CellState.X, received.YourSymbol);
        Assert.Equal("bob", received.OpponentNickname);
    }

    [Fact]
    public void IncomingGameState_RaisesGameStateChangedEvent()
    {
        using var session = new GameSessionService(_network.Object);
        GameStateMessage? received = null;
        session.GameStateChanged += (_, msg) => received = msg;

        var board = new[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
        Raise(MessageTypes.GameState, new GameStateMessage(board, CellState.O, GameStatus.InProgress));

        Assert.NotNull(received);
        Assert.Equal(GameStatus.InProgress, received!.Status);
        Assert.Equal(CellState.O, received.CurrentPlayer);
        Assert.Equal(1, received.Board[0]);
    }

    [Fact]
    public void IncomingGameOver_RaisesGameOverEvent()
    {
        using var session = new GameSessionService(_network.Object);
        GameOverMessage? received = null;
        session.GameOver += (_, msg) => received = msg;

        Raise(MessageTypes.GameOver, new GameOverMessage(GameStatus.XWin, "alice"));

        Assert.Equal(GameStatus.XWin, received!.Status);
        Assert.Equal("alice", received.Winner);
    }

    [Fact]
    public void IncomingGamesList_RaisesGamesListReceivedEvent()
    {
        using var session = new GameSessionService(_network.Object);
        GamesListMessage? received = null;
        session.GamesListReceived += (_, msg) => received = msg;

        var games = new[] { new GameInfo(Guid.NewGuid(), "carol", DateTimeOffset.UtcNow) };
        Raise(MessageTypes.GamesList, new GamesListMessage(games));

        Assert.Single(received!.Games);
        Assert.Equal("carol", received.Games[0].CreatedBy);
    }

    [Fact]
    public void IncomingLeaderboardResponse_RaisesLeaderboardReceivedEvent()
    {
        using var session = new GameSessionService(_network.Object);
        LeaderboardResponseMessage? received = null;
        session.LeaderboardReceived += (_, msg) => received = msg;

        var entries = new[] { new LeaderboardEntry("alice", 5, 2, 1) };
        Raise(MessageTypes.LeaderboardResponse, new LeaderboardResponseMessage(entries));

        Assert.Single(received!.Entries);
        Assert.Equal(5, received.Entries[0].Wins);
    }

    [Fact]
    public void IncomingError_RaisesServerErrorEvent()
    {
        using var session = new GameSessionService(_network.Object);
        ErrorMessage? received = null;
        session.ServerError += (_, msg) => received = msg;

        Raise(MessageTypes.Error, new ErrorMessage("E_BAD", "bad request"));

        Assert.Equal("E_BAD", received!.Code);
        Assert.Equal("bad request", received.Message);
    }

    [Fact]
    public void IncomingUnknownType_DoesNotThrow()
    {
        using var session = new GameSessionService(_network.Object);
        // Просто проверяем, что вход в свитч с неизвестным типом не падает.
        Raise("UnknownTypeXYZ", new HelloMessage("x"));
    }

    [Fact]
    public void UnderlyingDisconnect_BubblesUp()
    {
        using var session = new GameSessionService(_network.Object);
        Exception? captured = null;
        var bubbled = false;
        session.Disconnected += (_, ex) => { bubbled = true; captured = ex; };

        var ex = new IOException("сокет упал");
        _network.Raise(c => c.Disconnected += null, this, ex);

        Assert.True(bubbled);
        Assert.Same(ex, captured);
    }

    [Fact]
    public void Dispose_UnsubscribesFromUnderlyingClient()
    {
        var session = new GameSessionService(_network.Object);
        var fired = false;
        session.GameStarted += (_, _) => fired = true;

        session.Dispose();
        Raise(MessageTypes.GameStarted, new GameStartedMessage(Guid.NewGuid(), CellState.X, "x"));

        Assert.False(fired);
    }

    private void Raise<T>(string type, T payload) =>
        _network.Raise(c => c.MessageReceived += null,
            this, new NetMessage(type, ProtocolJson.ToElement(payload)));
}
