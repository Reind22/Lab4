using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TicTacToe.Shared.Protocol;

namespace TicTacToe.Server.Networking;

/// <summary>Подключение одного клиента: чтение/запись <see cref="NetMessage"/>.</summary>
public sealed class ClientConnection : IAsyncDisposable
{
    private readonly TcpClient _tcp;
    private readonly Stream _stream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ILogger<ClientConnection>? _logger;

    public Guid ConnectionId { get; } = Guid.NewGuid();

    public string? Nickname { get; set; }

    public ClientConnection(TcpClient tcp, ILogger<ClientConnection>? logger = null)
    {
        _tcp = tcp;
        _stream = tcp.GetStream();
        _logger = logger;
    }

    public Task<NetMessage?> ReadAsync(CancellationToken ct) =>
        MessageFramer.ReadAsync(_stream, ct);

    public async Task SendAsync(NetMessage message, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await MessageFramer.WriteAsync(_stream, message, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (_logger is not null)
        {
            _logger.LogWarning(ex, "Не удалось отправить {Type} клиенту {Id}", message.Type, ConnectionId);
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task TrySendAsync(NetMessage message, CancellationToken ct = default)
    {
        try { await SendAsync(message, ct).ConfigureAwait(false); }
        catch { /* клиент отключился — пусть верхний уровень обработает */ }
    }

    public ValueTask DisposeAsync()
    {
        try { _tcp.Close(); } catch { }
        _writeLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
