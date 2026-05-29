using TicTacToe.Client.Core.Networking;
using TicTacToe.Shared.Protocol;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.Services;

/// <summary>
/// Высокоуровневая обёртка над <see cref="INetworkClient"/>. Принимает «сырые» сообщения,
/// раскладывает их по типизированным событиям и предоставляет методы для команд клиента.
/// </summary>
public sealed class GameSessionService : IGameSessionService, IDisposable
{
    private readonly INetworkClient _client;
    private bool _disposed;

    public GameSessionService(INetworkClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _client.MessageReceived += OnMessageReceived;
        _client.Disconnected += OnDisconnected;
    }

    /// <summary>Подключён ли клиент к серверу.</summary>
    public bool IsConnected => _client.IsConnected;

    public event EventHandler<GameStartedMessage>? GameStarted;
    public event EventHandler<GameStateMessage>? GameStateChanged;
    public event EventHandler<GameOverMessage>? GameOver;
    public event EventHandler<GamesListMessage>? GamesListReceived;
    public event EventHandler<LeaderboardResponseMessage>? LeaderboardReceived;
    public event EventHandler<ErrorMessage>? ServerError;
    public event EventHandler<Exception?>? Disconnected;

    public Task ConnectAsync(string host, int port, CancellationToken ct = default) =>
        _client.ConnectAsync(host, port, ct);

    public Task SayHelloAsync(string nickname, CancellationToken ct = default) =>
        SendAsync(MessageTypes.Hello, new HelloMessage(nickname), ct);

    public Task CreateGameAsync(CancellationToken ct = default) =>
        SendAsync(MessageTypes.CreateGame, new CreateGameMessage(), ct);

    public Task RequestGamesListAsync(CancellationToken ct = default) =>
        SendAsync(MessageTypes.ListGames, new ListGamesMessage(), ct);

    public Task JoinGameAsync(Guid gameId, CancellationToken ct = default) =>
        SendAsync(MessageTypes.JoinGame, new JoinGameMessage(gameId), ct);

    public Task MakeMoveAsync(Guid gameId, int row, int col, CancellationToken ct = default) =>
        SendAsync(MessageTypes.MakeMove, new MakeMoveMessage(gameId, row, col), ct);

    public Task RequestRematchAsync(Guid gameId, CancellationToken ct = default) =>
        SendAsync(MessageTypes.Rematch, new RematchMessage(gameId), ct);

    public Task RequestLeaderboardAsync(CancellationToken ct = default) =>
        SendAsync(MessageTypes.LeaderboardRequest, new LeaderboardRequestMessage(), ct);

    public Task DisconnectAsync() => _client.DisconnectAsync();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _client.MessageReceived -= OnMessageReceived;
        _client.Disconnected -= OnDisconnected;
    }

    private Task SendAsync<T>(string type, T payload, CancellationToken ct) =>
        _client.SendAsync(new NetMessage(type, ProtocolJson.ToElement(payload)), ct);

    private void OnMessageReceived(object? sender, NetMessage message)
    {
        switch (message.Type)
        {
            case MessageTypes.GameStarted:
                Raise(GameStarted, ProtocolJson.FromElement<GameStartedMessage>(message.Payload));
                break;
            case MessageTypes.GameState:
                Raise(GameStateChanged, ProtocolJson.FromElement<GameStateMessage>(message.Payload));
                break;
            case MessageTypes.GameOver:
                Raise(GameOver, ProtocolJson.FromElement<GameOverMessage>(message.Payload));
                break;
            case MessageTypes.GamesList:
                Raise(GamesListReceived, ProtocolJson.FromElement<GamesListMessage>(message.Payload));
                break;
            case MessageTypes.LeaderboardResponse:
                Raise(LeaderboardReceived, ProtocolJson.FromElement<LeaderboardResponseMessage>(message.Payload));
                break;
            case MessageTypes.Error:
                Raise(ServerError, ProtocolJson.FromElement<ErrorMessage>(message.Payload));
                break;
            // Hello/CreateGame/ListGames/JoinGame/MakeMove/Rematch/LeaderboardRequest —
            // это исходящие; от сервера их не приходит.
        }
    }

    private void Raise<T>(EventHandler<T>? handler, T? payload) where T : class
    {
        if (handler is null || payload is null) return;
        handler.Invoke(this, payload);
    }

    private void OnDisconnected(object? sender, Exception? error) =>
        Disconnected?.Invoke(this, error);
}
