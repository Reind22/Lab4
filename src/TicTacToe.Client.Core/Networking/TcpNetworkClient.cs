using System.Net.Sockets;
using TicTacToe.Shared.Protocol;

namespace TicTacToe.Client.Core.Networking;

/// <summary>
/// Реализация <see cref="INetworkClient"/> поверх обычного TCP-сокета.
/// Сообщения сериализуются и фреймятся через <see cref="MessageFramer"/>.
/// </summary>
public sealed class TcpNetworkClient : INetworkClient
{
    private readonly object _sendLock = new();
    private readonly SynchronizationContext? _syncContext;
    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private CancellationTokenSource? _readLoopCts;
    private Task? _readLoop;
    private int _connected;
    private int _disposed;

    /// <summary>
    /// Создаёт клиент. Захватывает текущий <see cref="SynchronizationContext"/> (если есть),
    /// чтобы события <see cref="MessageReceived"/> и <see cref="Disconnected"/> приходили
    /// в тот же поток, где клиент был создан (для Avalonia это UI-поток).
    /// </summary>
    public TcpNetworkClient()
    {
        _syncContext = SynchronizationContext.Current;
    }

    public bool IsConnected => Volatile.Read(ref _connected) == 1;

    public event EventHandler<NetMessage>? MessageReceived;
    public event EventHandler<Exception?>? Disconnected;

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (IsConnected)
            throw new InvalidOperationException("Клиент уже подключён.");

        var tcp = new TcpClient();
        try
        {
            await tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);
        }
        catch
        {
            tcp.Dispose();
            throw;
        }

        _tcp = tcp;
        _stream = tcp.GetStream();
        Interlocked.Exchange(ref _connected, 1);

        _readLoopCts = new CancellationTokenSource();
        _readLoop = Task.Run(() => ReadLoopAsync(_readLoopCts.Token));
    }

    public async Task SendAsync(NetMessage message, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (!IsConnected || _stream is null)
            throw new InvalidOperationException("Клиент не подключён.");

        // Сериализация может идти параллельно, но запись в Stream должна быть атомарной —
        // передаём через очередь под локом, чтобы кадры не перемешивались.
        Task writeTask;
        lock (_sendLock)
        {
            writeTask = MessageFramer.WriteAsync(_stream, message, ct);
        }
        await writeTask.ConfigureAwait(false);
    }

    public async Task DisconnectAsync()
    {
        if (Interlocked.Exchange(ref _connected, 0) == 0) return;

        try { _readLoopCts?.Cancel(); } catch { /* ignore */ }
        try { _stream?.Dispose(); } catch { /* ignore */ }
        try { _tcp?.Close(); } catch { /* ignore */ }

        if (_readLoop is { } loop)
        {
            try { await loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch { /* проглатываем — событие Disconnected уже отправлено */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        await DisconnectAsync().ConfigureAwait(false);
        _readLoopCts?.Dispose();
        _tcp?.Dispose();
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        Exception? failure = null;
        try
        {
            while (!ct.IsCancellationRequested && _stream is not null)
            {
                var message = await MessageFramer.ReadAsync(_stream, ct).ConfigureAwait(false);
                if (message is null) break; // корректное закрытие
                RaiseOnContext(MessageReceived, message);
            }
        }
        catch (OperationCanceledException) { /* нормальный shutdown */ }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
        {
            // Сокет закрыт — нормальная ситуация при разрыве.
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            Interlocked.Exchange(ref _connected, 0);
            RaiseOnContext(Disconnected, failure);
        }
    }

    private void RaiseOnContext<T>(EventHandler<T>? handler, T arg)
    {
        if (handler is null) return;
        var ctx = _syncContext;
        if (ctx is null)
            handler.Invoke(this, arg);
        else
            ctx.Post(_ => handler.Invoke(this, arg), null);
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) == 1)
            throw new ObjectDisposedException(nameof(TcpNetworkClient));
    }
}
