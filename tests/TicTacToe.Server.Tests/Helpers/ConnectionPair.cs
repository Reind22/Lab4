using System.Net;
using System.Net.Sockets;
using TicTacToe.Server.Networking;

namespace TicTacToe.Server.Tests.Helpers;

/// <summary>
/// Создаёт два <see cref="ClientConnection"/>, соединённых через локальный TCP-сокет —
/// удобно для юнит-тестов GameSession, не поднимая полный сервер.
/// </summary>
internal sealed class ConnectionPair : IAsyncDisposable
{
    public ClientConnection Server { get; }
    public TcpClient ClientSide { get; }

    private readonly TcpListener _listener;

    private ConnectionPair(TcpListener listener, ClientConnection server, TcpClient client)
    {
        _listener = listener;
        Server = server;
        ClientSide = client;
    }

    public static async Task<ConnectionPair> CreateAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var clientSide = new TcpClient();
        var connectTask = clientSide.ConnectAsync(IPAddress.Loopback, port);
        var serverSide = await listener.AcceptTcpClientAsync();
        await connectTask;

        var conn = new ClientConnection(serverSide);
        return new ConnectionPair(listener, conn, clientSide);
    }

    public async ValueTask DisposeAsync()
    {
        ClientSide.Close();
        await Server.DisposeAsync();
        _listener.Stop();
    }
}
