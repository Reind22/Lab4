using CommunityToolkit.Mvvm.ComponentModel;
using Moq;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.Services;
using TicTacToe.Client.Core.ViewModels;
using TicTacToe.Shared.Protocol.Dtos;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.Tests.ViewModels;

public class LobbyViewModelTests
{
    private readonly Mock<IGameSessionService> _session = new();
    private readonly Mock<INavigationService> _navigation = new();

    private LobbyViewModel CreateVm() =>
        new(_session.Object, _navigation.Object, () => null!);

    [Fact]
    public void Construction_TriggersInitialRefresh()
    {
        _ = CreateVm();
        _session.Verify(s => s.RequestGamesListAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void GamesListReceived_PopulatesGames()
    {
        var vm = CreateVm();
        var list = new[]
        {
            new GameInfo(Guid.NewGuid(), "alice", DateTimeOffset.UtcNow),
            new GameInfo(Guid.NewGuid(), "bob", DateTimeOffset.UtcNow),
        };

        _session.Raise(s => s.GamesListReceived += null, this, new GamesListMessage(list));

        Assert.Equal(2, vm.Games.Count);
        Assert.Equal("alice", vm.Games[0].CreatedBy);
    }

    [Fact]
    public async Task JoinGame_CallsSession_AndNavigates()
    {
        var vm = new LobbyViewModel(_session.Object, _navigation.Object,
            () => new WaitingViewModel(_session.Object, _navigation.Object, _ => null!));
        var info = new GameInfo(Guid.NewGuid(), "x", DateTimeOffset.UtcNow);
        _session.Raise(s => s.GamesListReceived += null, this, new GamesListMessage(new[] { info }));
        vm.SelectedGame = vm.Games[0];

        await vm.JoinGameCommand.ExecuteAsync(null);

        _session.Verify(s => s.JoinGameAsync(info.GameId, It.IsAny<CancellationToken>()), Times.Once);
        _navigation.Verify(n => n.NavigateTo(It.IsAny<ObservableObject>()), Times.Once);
    }

    [Fact]
    public void JoinCommand_DisabledWhenNoSelection()
    {
        var vm = CreateVm();
        Assert.False(vm.JoinGameCommand.CanExecute(null));

        vm.SelectedGame = new GameInfo(Guid.NewGuid(), "x", DateTimeOffset.UtcNow);
        Assert.True(vm.JoinGameCommand.CanExecute(null));
    }

    [Fact]
    public void ServerError_SetsStatusMessage()
    {
        var vm = CreateVm();
        _session.Raise(s => s.ServerError += null, this, new ErrorMessage("E_X", "fail"));
        Assert.Contains("fail", vm.StatusMessage);
    }
}
