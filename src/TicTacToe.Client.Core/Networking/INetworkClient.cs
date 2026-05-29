using TicTacToe.Shared.Protocol;

namespace TicTacToe.Client.Core.Networking;

/// <summary>
/// Абстракция сетевого клиента для общения с сервером по протоколу TicTacToe.
/// Скрывает работу с сокетом, чтобы ViewModel'и можно было тестировать через мок.
/// </summary>
public interface INetworkClient : IAsyncDisposable
{
    /// <summary>Подключён ли клиент к серверу.</summary>
    bool IsConnected { get; }

    /// <summary>Срабатывает, когда от сервера получено очередное сообщение.</summary>
    event EventHandler<NetMessage>? MessageReceived;

    /// <summary>Срабатывает при разрыве соединения (в т.ч. при ошибке).</summary>
    event EventHandler<Exception?>? Disconnected;

    /// <summary>Подключиться к серверу. Должно быть вызвано один раз перед использованием.</summary>
    Task ConnectAsync(string host, int port, CancellationToken ct = default);

    /// <summary>Отправить произвольное сообщение протокола серверу.</summary>
    Task SendAsync(NetMessage message, CancellationToken ct = default);

    /// <summary>Корректно отключиться от сервера.</summary>
    Task DisconnectAsync();
}
