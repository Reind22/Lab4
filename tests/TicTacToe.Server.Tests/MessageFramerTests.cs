using TicTacToe.Shared.Protocol;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Server.Tests;

public class MessageFramerTests
{
    [Fact]
    public async Task RoundTrip_PreservesMessage()
    {
        using var ms = new MemoryStream();
        var src = new NetMessage(MessageTypes.Hello, ProtocolJson.ToElement(new HelloMessage("alice")));

        await MessageFramer.WriteAsync(ms, src);
        ms.Position = 0;

        var read = await MessageFramer.ReadAsync(ms);
        Assert.NotNull(read);
        Assert.Equal(MessageTypes.Hello, read!.Type);
        var hello = ProtocolJson.FromElement<HelloMessage>(read.Payload);
        Assert.Equal("alice", hello!.Nickname);
    }

    [Fact]
    public async Task ReadAsync_ReturnsNull_OnEmptyStream()
    {
        using var ms = new MemoryStream();
        var read = await MessageFramer.ReadAsync(ms);
        Assert.Null(read);
    }

    [Fact]
    public async Task RoundTrip_TwoMessagesInARow()
    {
        using var ms = new MemoryStream();
        var a = new NetMessage(MessageTypes.Hello, ProtocolJson.ToElement(new HelloMessage("a")));
        var b = new NetMessage(MessageTypes.CreateGame, ProtocolJson.ToElement(new CreateGameMessage()));

        await MessageFramer.WriteAsync(ms, a);
        await MessageFramer.WriteAsync(ms, b);
        ms.Position = 0;

        var ra = await MessageFramer.ReadAsync(ms);
        var rb = await MessageFramer.ReadAsync(ms);
        Assert.Equal(MessageTypes.Hello, ra!.Type);
        Assert.Equal(MessageTypes.CreateGame, rb!.Type);
    }
}
