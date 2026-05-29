using Microsoft.Extensions.Logging;
using TicTacToe.Server.Networking;
using TicTacToe.Server.Persistence;
using TicTacToe.Shared.Logic;
using TicTacToe.Shared.Protocol;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Server.Game;

/// <summary>Игровая сессия: пара клиентов + общая модель партии.</summary>
public sealed class GameSession
{
    private readonly Shared.Logic.Game _game = new();
    private readonly LeaderboardRepository? _leaderboard;
    private readonly ILogger<GameSession>? _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public Guid GameId { get; }

    public ClientConnection PlayerX { get; }

    public ClientConnection? PlayerO { get; private set; }

    public bool IsStarted => PlayerO is not null;

    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    public string CreatedByNickname => PlayerX.Nickname ?? "?";

    public GameSession(Guid gameId, ClientConnection playerX, LeaderboardRepository? leaderboard = null, ILogger<GameSession>? logger = null)
    {
        GameId = gameId;
        PlayerX = playerX;
        _leaderboard = leaderboard;
        _logger = logger;
    }

    /// <summary>Подключает второго игрока и шлёт обоим <c>GameStarted</c>.</summary>
    public async Task AttachPlayerOAsync(ClientConnection playerO, CancellationToken ct = default)
    {
        if (PlayerO is not null)
            throw new InvalidOperationException("В этой игре уже есть два игрока.");

        PlayerO = playerO;

        var startedForX = new NetMessage(MessageTypes.GameStarted,
            ProtocolJson.ToElement(new GameStartedMessage(GameId, CellState.X, playerO.Nickname ?? "?")));
        var startedForO = new NetMessage(MessageTypes.GameStarted,
            ProtocolJson.ToElement(new GameStartedMessage(GameId, CellState.O, PlayerX.Nickname ?? "?")));

        await PlayerX.TrySendAsync(startedForX, ct).ConfigureAwait(false);
        await playerO.TrySendAsync(startedForO, ct).ConfigureAwait(false);

        await BroadcastStateAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Принимает ход. Возвращает <c>true</c>, если ход сыгран.</summary>
    public async Task<bool> TryMakeMoveAsync(ClientConnection from, int row, int col, CancellationToken ct = default)
    {
        if (PlayerO is null) return false;

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var symbol = SymbolOf(from);
            if (symbol is null) return false;
            if (symbol.Value != _game.CurrentPlayer) return false;

            if (!_game.TryMakeMove(row, col))
                return false;

            await BroadcastStateAsync(ct).ConfigureAwait(false);

            if (_game.Status != GameStatus.InProgress)
                await FinalizeAsync(ct).ConfigureAwait(false);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Сбрасывает партию для повторной игры.</summary>
    public async Task RematchAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _game.Reset();
            await BroadcastStateAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task FinalizeAsync(CancellationToken ct)
    {
        var status = _game.Status;
        string? winner = status switch
        {
            GameStatus.XWin => PlayerX.Nickname,
            GameStatus.OWin => PlayerO?.Nickname,
            _ => null
        };

        var over = new NetMessage(MessageTypes.GameOver,
            ProtocolJson.ToElement(new GameOverMessage(status, winner)));
        await PlayerX.TrySendAsync(over, ct).ConfigureAwait(false);
        if (PlayerO is not null)
            await PlayerO.TrySendAsync(over, ct).ConfigureAwait(false);

        if (_leaderboard is null) return;

        try
        {
            switch (status)
            {
                case GameStatus.XWin when PlayerX.Nickname is not null && PlayerO?.Nickname is not null:
                    await _leaderboard.IncrementWinAsync(PlayerX.Nickname, ct).ConfigureAwait(false);
                    await _leaderboard.IncrementLossAsync(PlayerO.Nickname, ct).ConfigureAwait(false);
                    break;
                case GameStatus.OWin when PlayerX.Nickname is not null && PlayerO?.Nickname is not null:
                    await _leaderboard.IncrementWinAsync(PlayerO.Nickname, ct).ConfigureAwait(false);
                    await _leaderboard.IncrementLossAsync(PlayerX.Nickname, ct).ConfigureAwait(false);
                    break;
                case GameStatus.Draw when PlayerX.Nickname is not null && PlayerO?.Nickname is not null:
                    await _leaderboard.IncrementDrawAsync(PlayerX.Nickname, ct).ConfigureAwait(false);
                    await _leaderboard.IncrementDrawAsync(PlayerO.Nickname, ct).ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Не удалось обновить лидерборд для игры {GameId}", GameId);
        }
    }

    public CellState? SymbolOf(ClientConnection connection)
    {
        if (connection == PlayerX) return CellState.X;
        if (connection == PlayerO) return CellState.O;
        return null;
    }

    public ClientConnection? Opponent(ClientConnection of)
    {
        if (of == PlayerX) return PlayerO;
        if (of == PlayerO) return PlayerX;
        return null;
    }

    internal Shared.Logic.Game GameModel => _game;

    internal async Task BroadcastStateAsync(CancellationToken ct)
    {
        var board = _game.Snapshot().Select(c => (int)c).ToArray();
        var msg = new NetMessage(MessageTypes.GameState,
            ProtocolJson.ToElement(new GameStateMessage(board, _game.CurrentPlayer, _game.Status)));
        await PlayerX.TrySendAsync(msg, ct).ConfigureAwait(false);
        if (PlayerO is not null)
            await PlayerO.TrySendAsync(msg, ct).ConfigureAwait(false);
    }
}
