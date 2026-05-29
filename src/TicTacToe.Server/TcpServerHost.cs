using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicTacToe.Server.Networking;

namespace TicTacToe.Server;

/// <summary><see cref="BackgroundService"/>, который держит TCP-сервер на протяжении жизни хоста.</summary>
public sealed class TcpServerHost : BackgroundService
{
    private readonly TcpServer _server;
    private readonly ILogger<TcpServerHost> _logger;

    public TcpServerHost(TcpServer server, ILogger<TcpServerHost> logger)
    {
        _server = server;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _server.RunAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Сервер завершился с ошибкой");
            throw;
        }
    }
}
