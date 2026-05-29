using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TicTacToe.Server.Networking;

/// <summary>TCP-сервер: слушает порт и стартует обработчик на каждое подключение.</summary>
public sealed class TcpServer
{
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TcpServer> _logger;
    private readonly int _port;

    public int? BoundPort { get; private set; }

    public TcpServer(IServiceProvider services, ILoggerFactory loggerFactory, IOptions<ServerOptions> options)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<TcpServer>();
        _port = options.Value.Port;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        BoundPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        _logger.LogInformation("TCP-сервер слушает порт {Port}", BoundPort);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }

                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var conn = new ClientConnection(client, _loggerFactory.CreateLogger<ClientConnection>());
        _logger.LogInformation("Клиент подключился: {Id}", conn.ConnectionId);

        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ClientHandler>();

        try
        {
            await handler.RunAsync(conn, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка в обработчике клиента {Id}", conn.ConnectionId);
        }
        finally
        {
            _logger.LogInformation("Клиент отключился: {Id}", conn.ConnectionId);
        }
    }
}
