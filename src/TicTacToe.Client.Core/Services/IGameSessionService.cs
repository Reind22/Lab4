using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.Services;

/// <summary>
/// Контракт сервиса сессии — позволяет ViewModel'ям общаться с сервером,
/// не завися от конкретной TCP-реализации (упрощает тестирование через моки).
/// </summary>
public interface IGameSessionService
{
    bool IsConnected { get; }

    event EventHandler<GameStartedMessage>? GameStarted;
    event EventHandler<GameStateMessage>? GameStateChanged;
    event EventHandler<GameOverMessage>? GameOver;
    event EventHandler<GamesListMessage>? GamesListReceived;
    event EventHandler<LeaderboardResponseMessage>? LeaderboardReceived;
    event EventHandler<ErrorMessage>? ServerError;
    event EventHandler<Exception?>? Disconnected;

    Task ConnectAsync(string host, int port, CancellationToken ct = default);
    Task SayHelloAsync(string nickname, CancellationToken ct = default);
    Task CreateGameAsync(CancellationToken ct = default);
    Task RequestGamesListAsync(CancellationToken ct = default);
    Task JoinGameAsync(Guid gameId, CancellationToken ct = default);
    Task MakeMoveAsync(Guid gameId, int row, int col, CancellationToken ct = default);
    Task RequestRematchAsync(Guid gameId, CancellationToken ct = default);
    Task RequestLeaderboardAsync(CancellationToken ct = default);
    Task DisconnectAsync();
}
