using System.Net.Sockets;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TicTacToe.Server;
using TicTacToe.Server.Game;
using TicTacToe.Server.Networking;
using TicTacToe.Server.Persistence;
using TicTacToe.Shared.Logic;
using TicTacToe.Shared.Protocol;
using TicTacToe.Shared.Protocol.Messages;
using Xunit;

namespace TicTacToe.Server.Tests;

public class TcpIntegrationTests
{
    private static int FreePort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [Fact(Timeout = 15_000)]
    public async Task FullMatch_TwoClients_PlayUntilXWins_AndLeaderboardUpdated()
    {
        // Лидерборд in-memory.
        using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var dbOptions = new DbContextOptionsBuilder<LeaderboardDbContext>().UseSqlite(connection).Options;
        await using var db = new LeaderboardDbContext(dbOptions);
        await db.Database.EnsureCreatedAsync();
        var repo = new LeaderboardRepository(db);

        var lobby = new LobbyService(repo);

        // Поднимаем сервер на свободном порту.
        var port = FreePort();
        var serverOptions = Options.Create(new ServerOptions { Port = port });

        var services = new ServiceCollection();
        services.AddSingleton(repo);
        services.AddSingleton(lobby);
        services.AddScoped<ClientHandler>();
        var sp = services.BuildServiceProvider();

        var server = new TcpServer(sp, NullLoggerFactory.Instance, serverOptions);
        using var serverCts = new CancellationTokenSource();
        var serverTask = Task.Run(() => server.RunAsync(serverCts.Token));

        // Дождаться, пока сервер забиндится.
        for (var i = 0; i < 50 && server.BoundPort is null; i++)
            await Task.Delay(20);
        Assert.NotNull(server.BoundPort);

        try
        {
            using var alice = new TcpClient();
            using var bob = new TcpClient();
            await alice.ConnectAsync(System.Net.IPAddress.Loopback, port);
            await bob.ConnectAsync(System.Net.IPAddress.Loopback, port);

            var aliceStream = alice.GetStream();
            var bobStream = bob.GetStream();

            // Hello
            await SendAsync(aliceStream, MessageTypes.Hello, new HelloMessage("alice"));
            await SendAsync(bobStream, MessageTypes.Hello, new HelloMessage("bob"));

            // Alice создаёт игру, Bob запрашивает список и присоединяется.
            await SendAsync(aliceStream, MessageTypes.CreateGame, new CreateGameMessage());

            GamesListMessage list;
            for (var attempt = 0; ; attempt++)
            {
                await SendAsync(bobStream, MessageTypes.ListGames, new ListGamesMessage());
                var listMsg = await ReadAsync(bobStream);
                Assert.Equal(MessageTypes.GamesList, listMsg!.Type);
                list = ProtocolJson.FromElement<GamesListMessage>(listMsg.Payload)!;
                if (list.Games.Count > 0) break;
                Assert.True(attempt < 50, "Игра не появилась в лобби.");
                await Task.Delay(50);
            }
            var gameId = list.Games[0].GameId;

            await SendAsync(bobStream, MessageTypes.JoinGame, new JoinGameMessage(gameId));

            // Оба должны получить GameStarted и начальный GameState.
            var aliceStarted = await ReadAsync(aliceStream);
            var bobStarted = await ReadAsync(bobStream);
            Assert.Equal(MessageTypes.GameStarted, aliceStarted!.Type);
            Assert.Equal(MessageTypes.GameStarted, bobStarted!.Type);

            var aliceState0 = await ReadAsync(aliceStream);
            var bobState0 = await ReadAsync(bobStream);
            Assert.Equal(MessageTypes.GameState, aliceState0!.Type);
            Assert.Equal(MessageTypes.GameState, bobState0!.Type);

            // Партия: X побеждает по верхней строке.
            await PlayMove(aliceStream, gameId, 0, 0);
            await DrainState(aliceStream); await DrainState(bobStream);

            await PlayMove(bobStream, gameId, 1, 0);
            await DrainState(aliceStream); await DrainState(bobStream);

            await PlayMove(aliceStream, gameId, 0, 1);
            await DrainState(aliceStream); await DrainState(bobStream);

            await PlayMove(bobStream, gameId, 1, 1);
            await DrainState(aliceStream); await DrainState(bobStream);

            await PlayMove(aliceStream, gameId, 0, 2);
            // Финальный GameState + GameOver — оба клиента.
            var finalState = await ReadAsync(aliceStream);
            Assert.Equal(MessageTypes.GameState, finalState!.Type);
            var finalStateMsg = ProtocolJson.FromElement<GameStateMessage>(finalState.Payload)!;
            Assert.Equal(GameStatus.XWin, finalStateMsg.Status);

            var aliceOver = await ReadAsync(aliceStream);
            await ReadAsync(bobStream); // bob's GameState
            var bobOver = await ReadAsync(bobStream);
            Assert.Equal(MessageTypes.GameOver, aliceOver!.Type);
            Assert.Equal(MessageTypes.GameOver, bobOver!.Type);

            var aliceOverMsg = ProtocolJson.FromElement<GameOverMessage>(aliceOver.Payload)!;
            Assert.Equal(GameStatus.XWin, aliceOverMsg.Status);
            Assert.Equal("alice", aliceOverMsg.Winner);

            // Проверяем лидерборд.
            await SendAsync(aliceStream, MessageTypes.LeaderboardRequest, new LeaderboardRequestMessage());
            var board = await ReadAsync(aliceStream);
            Assert.Equal(MessageTypes.LeaderboardResponse, board!.Type);
            var boardMsg = ProtocolJson.FromElement<LeaderboardResponseMessage>(board.Payload)!;
            var aliceEntry = boardMsg.Entries.First(e => e.Nickname == "alice");
            var bobEntry = boardMsg.Entries.First(e => e.Nickname == "bob");
            Assert.Equal(1, aliceEntry.Wins);
            Assert.Equal(1, bobEntry.Losses);
        }
        finally
        {
            serverCts.Cancel();
            try { await serverTask; } catch { }
        }
    }

    private static Task SendAsync<T>(Stream s, string type, T payload) =>
        MessageFramer.WriteAsync(s, new NetMessage(type, ProtocolJson.ToElement(payload)));

    private static async Task<NetMessage?> ReadAsync(Stream s)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        return await MessageFramer.ReadAsync(s, cts.Token);
    }

    private static Task PlayMove(Stream s, Guid gameId, int row, int col) =>
        SendAsync(s, MessageTypes.MakeMove, new MakeMoveMessage(gameId, row, col));

    private static async Task DrainState(Stream s)
    {
        var msg = await ReadAsync(s);
        Assert.Equal(MessageTypes.GameState, msg!.Type);
    }
}
