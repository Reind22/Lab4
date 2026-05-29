using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TicTacToe.Server.Networking;
using TicTacToe.Server.Persistence;
using TicTacToe.Shared.Protocol.Dtos;

namespace TicTacToe.Server.Game;

/// <summary>Лобби: создаёт/выдаёт игровые сессии.</summary>
public sealed class LobbyService
{
    private readonly ConcurrentDictionary<Guid, GameSession> _games = new();
    private readonly LeaderboardRepository? _leaderboard;
    private readonly ILoggerFactory? _loggerFactory;

    public LobbyService(LeaderboardRepository? leaderboard = null, ILoggerFactory? loggerFactory = null)
    {
        _leaderboard = leaderboard;
        _loggerFactory = loggerFactory;
    }

    public GameSession CreateGame(ClientConnection creator)
    {
        if (creator.Nickname is null)
            throw new InvalidOperationException("Создатель должен иметь никнейм (отправь Hello).");

        if (_games.Values.Any(g => !g.IsStarted && g.PlayerX == creator))
            throw new InvalidOperationException("У вас уже есть открытая игра.");

        var id = Guid.NewGuid();
        var session = new GameSession(id, creator, _leaderboard, _loggerFactory?.CreateLogger<GameSession>());
        if (!_games.TryAdd(id, session))
            throw new InvalidOperationException("Не удалось создать игру: коллизия GameId.");
        return session;
    }

    public async Task<GameSession> JoinGameAsync(Guid gameId, ClientConnection joiner, CancellationToken ct = default)
    {
        if (joiner.Nickname is null)
            throw new InvalidOperationException("Игрок должен иметь никнейм (отправь Hello).");

        if (!_games.TryGetValue(gameId, out var session))
            throw new InvalidOperationException("Игра не найдена.");

        if (session.IsStarted)
            throw new InvalidOperationException("Игра уже идёт.");

        if (session.PlayerX == joiner)
            throw new InvalidOperationException("Нельзя играть против самого себя.");

        await session.AttachPlayerOAsync(joiner, ct).ConfigureAwait(false);
        return session;
    }

    public IReadOnlyList<GameInfo> ListGames() =>
        _games.Values
            .Where(g => !g.IsStarted)
            .Select(g => new GameInfo(g.GameId, g.CreatedByNickname, g.CreatedAt))
            .OrderBy(g => g.CreatedAt)
            .ToList();

    public bool TryGetGame(Guid gameId, out GameSession session) =>
        _games.TryGetValue(gameId, out session!);

    /// <summary>Удаляет все игры, в которых участвует данное соединение.</summary>
    public IReadOnlyList<GameSession> RemoveGamesOf(ClientConnection connection)
    {
        var removed = new List<GameSession>();
        foreach (var pair in _games)
        {
            if (pair.Value.PlayerX == connection || pair.Value.PlayerO == connection)
            {
                if (_games.TryRemove(pair.Key, out var s))
                    removed.Add(s);
            }
        }
        return removed;
    }
}
