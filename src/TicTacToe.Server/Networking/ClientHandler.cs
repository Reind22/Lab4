using Microsoft.Extensions.Logging;
using TicTacToe.Server.Game;
using TicTacToe.Server.Persistence;
using TicTacToe.Shared.Protocol;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Server.Networking;

/// <summary>Обрабатывает входящие сообщения от одного клиента.</summary>
public sealed class ClientHandler
{
    private readonly LobbyService _lobby;
    private readonly LeaderboardRepository? _leaderboard;
    private readonly ILogger<ClientHandler>? _logger;

    public ClientHandler(LobbyService lobby, LeaderboardRepository? leaderboard = null, ILogger<ClientHandler>? logger = null)
    {
        _lobby = lobby;
        _leaderboard = leaderboard;
        _logger = logger;
    }

    public async Task RunAsync(ClientConnection conn, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                NetMessage? msg;
                try { msg = await conn.ReadAsync(ct).ConfigureAwait(false); }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Чтение от {Id} прервано", conn.ConnectionId);
                    return;
                }
                if (msg is null) return;
                await DispatchAsync(conn, msg, ct).ConfigureAwait(false);
            }
        }
        finally
        {
            await CleanupAsync(conn, ct).ConfigureAwait(false);
        }
    }

    private async Task DispatchAsync(ClientConnection conn, NetMessage msg, CancellationToken ct)
    {
        switch (msg.Type)
        {
            case MessageTypes.Hello:
                {
                    var hello = ProtocolJson.FromElement<HelloMessage>(msg.Payload);
                    if (hello is null || string.IsNullOrWhiteSpace(hello.Nickname))
                        await SendErrorAsync(conn, "bad_hello", "Hello должен содержать nickname.", ct);
                    else
                        conn.Nickname = hello.Nickname.Trim();
                    return;
                }
            case MessageTypes.CreateGame:
                {
                    if (!Authenticated(conn))
                    {
                        await SendErrorAsync(conn, "no_hello", "Сначала отправь Hello.", ct);
                        return;
                    }
                    try
                    {
                        var session = _lobby.CreateGame(conn);
                        _logger?.LogInformation("Создана игра {GameId} (создатель {Nick})", session.GameId, conn.Nickname);
                    }
                    catch (Exception ex)
                    {
                        await SendErrorAsync(conn, "create_failed", ex.Message, ct);
                    }
                    return;
                }
            case MessageTypes.ListGames:
                {
                    var list = _lobby.ListGames();
                    var resp = new NetMessage(MessageTypes.GamesList,
                        ProtocolJson.ToElement(new GamesListMessage(list)));
                    await conn.TrySendAsync(resp, ct).ConfigureAwait(false);
                    return;
                }
            case MessageTypes.JoinGame:
                {
                    if (!Authenticated(conn))
                    {
                        await SendErrorAsync(conn, "no_hello", "Сначала отправь Hello.", ct);
                        return;
                    }
                    var join = ProtocolJson.FromElement<JoinGameMessage>(msg.Payload);
                    if (join is null)
                    {
                        await SendErrorAsync(conn, "bad_join", "JoinGame должен содержать gameId.", ct);
                        return;
                    }
                    try { await _lobby.JoinGameAsync(join.GameId, conn, ct).ConfigureAwait(false); }
                    catch (Exception ex) { await SendErrorAsync(conn, "join_failed", ex.Message, ct); }
                    return;
                }
            case MessageTypes.MakeMove:
                {
                    var mv = ProtocolJson.FromElement<MakeMoveMessage>(msg.Payload);
                    if (mv is null)
                    {
                        await SendErrorAsync(conn, "bad_move", "MakeMove должен содержать gameId/row/col.", ct);
                        return;
                    }
                    if (!_lobby.TryGetGame(mv.GameId, out var session))
                    {
                        await SendErrorAsync(conn, "no_game", "Игра не найдена.", ct);
                        return;
                    }
                    var ok = await session.TryMakeMoveAsync(conn, mv.Row, mv.Col, ct).ConfigureAwait(false);
                    if (!ok)
                        await SendErrorAsync(conn, "bad_move", "Ход отвергнут (не ваш ход или некорректный).", ct);
                    return;
                }
            case MessageTypes.Rematch:
                {
                    var re = ProtocolJson.FromElement<RematchMessage>(msg.Payload);
                    if (re is null || !_lobby.TryGetGame(re.GameId, out var session))
                    {
                        await SendErrorAsync(conn, "no_game", "Игра не найдена.", ct);
                        return;
                    }
                    if (session.SymbolOf(conn) is null)
                    {
                        await SendErrorAsync(conn, "not_player", "Вы не игрок этой партии.", ct);
                        return;
                    }
                    await session.RematchAsync(ct).ConfigureAwait(false);
                    return;
                }
            case MessageTypes.LeaderboardRequest:
                {
                    var entries = _leaderboard is null
                        ? Array.Empty<Shared.Protocol.Dtos.LeaderboardEntry>()
                        : (await _leaderboard.GetTopAsync(20, ct).ConfigureAwait(false)).ToArray();
                    var resp = new NetMessage(MessageTypes.LeaderboardResponse,
                        ProtocolJson.ToElement(new LeaderboardResponseMessage(entries)));
                    await conn.TrySendAsync(resp, ct).ConfigureAwait(false);
                    return;
                }
            default:
                await SendErrorAsync(conn, "unknown_type", $"Тип {msg.Type} пока не поддерживается.", ct);
                return;
        }
    }

    private static bool Authenticated(ClientConnection conn) => !string.IsNullOrEmpty(conn.Nickname);

    private static Task SendErrorAsync(ClientConnection conn, string code, string text, CancellationToken ct) =>
        conn.TrySendAsync(new NetMessage(MessageTypes.Error,
            ProtocolJson.ToElement(new ErrorMessage(code, text))), ct);

    private async Task CleanupAsync(ClientConnection conn, CancellationToken ct)
    {
        var dropped = _lobby.RemoveGamesOf(conn);
        foreach (var session in dropped)
        {
            var opponent = session.Opponent(conn);
            if (opponent is null) continue;
            await SendErrorAsync(opponent, "opponent_left", "Соперник отключился.", ct).ConfigureAwait(false);
        }
        await conn.DisposeAsync().ConfigureAwait(false);
    }
}
