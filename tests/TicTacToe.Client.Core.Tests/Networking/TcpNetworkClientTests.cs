using System.Net;
using System.Net.Sockets;
using TicTacToe.Client.Core.Networking;
using TicTacToe.Shared.Protocol;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.Tests.Networking;

/// <summary>
/// Тесты сетевого клиента через локальный TcpListener — реальный сокет на 127.0.0.1.
/// </summary>
public class TcpNetworkClientTests : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly int _port;

    public TcpNetworkClientTests()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
    }

    public ValueTask DisposeAsync()
    {
        _listener.Stop();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task ConnectAsync_SetsIsConnectedTrue()
    {
        await using var client = new TcpNetworkClient();
        var serverAcceptTask = _listener.AcceptTcpClientAsync();

        await client.ConnectAsync(IPAddress.Loopback.ToString(), _port);
        using var server = await serverAcceptTask;

        Assert.True(client.IsConnected);
    }

    [Fact]
    public async Task SendAsync_DeliversMessageToServer()
    {
        await using var client = new TcpNetworkClient();
        var serverAcceptTask = _listener.AcceptTcpClientAsync();
        await client.ConnectAsync(IPAddress.Loopback.ToString(), _port);
        using var server = await serverAcceptTask;
        await using var serverStream = server.GetStream();

        var hello = new HelloMessage("alice");
        var envelope = new NetMessage(MessageTypes.Hello, ProtocolJson.ToElement(hello));

        await client.SendAsync(envelope);

        var received = await MessageFramer.ReadAsync(serverStream);
        Assert.NotNull(received);
        Assert.Equal(MessageTypes.Hello, received!.Type);
        var roundTripped = ProtocolJson.FromElement<HelloMessage>(received.Payload);
        Assert.Equal("alice", roundTripped!.Nickname);
    }

    [Fact]
    public async Task ServerSendsMessage_RaisesMessageReceived()
    {
        await using var client = new TcpNetworkClient();
        var serverAcceptTask = _listener.AcceptTcpClientAsync();
        await client.ConnectAsync(IPAddress.Loopback.ToString(), _port);
        using var server = await serverAcceptTask;
        await using var serverStream = server.GetStream();

        var tcs = new TaskCompletionSource<NetMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.MessageReceived += (_, msg) => tcs.TrySetResult(msg);

        var error = new ErrorMessage("E001", "тест");
        var envelope = new NetMessage(MessageTypes.Error, ProtocolJson.ToElement(error));
        await MessageFramer.WriteAsync(serverStream, envelope);

        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(MessageTypes.Error, received.Type);
        var payload = ProtocolJson.FromElement<ErrorMessage>(received.Payload);
        Assert.Equal("E001", payload!.Code);
        Assert.Equal("тест", payload.Message);
    }

    [Fact]
    public async Task ServerClosesConnection_RaisesDisconnected()
    {
        await using var client = new TcpNetworkClient();
        var serverAcceptTask = _listener.AcceptTcpClientAsync();
        await client.ConnectAsync(IPAddress.Loopback.ToString(), _port);
        var server = await serverAcceptTask;

        var tcs = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Disconnected += (_, ex) => tcs.TrySetResult(ex);

        server.Close();

        var ex = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Null(ex); // корректное закрытие — без исключения
        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_RaisesDisconnectedAndClearsState()
    {
        await using var client = new TcpNetworkClient();
        var serverAcceptTask = _listener.AcceptTcpClientAsync();
        await client.ConnectAsync(IPAddress.Loopback.ToString(), _port);
        using var server = await serverAcceptTask;

        var tcs = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Disconnected += (_, ex) => tcs.TrySetResult(ex);

        await client.DisconnectAsync();

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task SendAsync_BeforeConnect_Throws()
    {
        await using var client = new TcpNetworkClient();
        var envelope = new NetMessage(MessageTypes.Hello, ProtocolJson.ToElement(new HelloMessage("x")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(envelope));
    }
}
