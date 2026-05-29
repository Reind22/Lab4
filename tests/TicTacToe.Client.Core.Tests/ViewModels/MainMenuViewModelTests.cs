using CommunityToolkit.Mvvm.ComponentModel;
using Moq;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.Services;
using TicTacToe.Client.Core.ViewModels;

namespace TicTacToe.Client.Core.Tests.ViewModels;

public class MainMenuViewModelTests
{
    private readonly Mock<IGameSessionService> _session = new();
    private readonly Mock<INavigationService> _navigation = new();
    private readonly PlayerSession _player = new() { Nickname = "", Host = "127.0.0.1", Port = 5050 };

    private MainMenuViewModel CreateVm()
    {
        var lobbyFactory = () => (LobbyViewModel)null!;
        var waitingFactory = () => (WaitingViewModel)null!;
        var leaderboardFactory = () => (LeaderboardViewModel)null!;
        return new MainMenuViewModel(
            _session.Object, _navigation.Object, _player,
            lobbyFactory, waitingFactory, leaderboardFactory);
    }

    [Fact]
    public void CreateGameCommand_DisabledWhenNicknameEmpty()
    {
        var vm = CreateVm();
        vm.Nickname = "";
        Assert.False(vm.CreateGameCommand.CanExecute(null));
    }

    [Fact]
    public void CreateGameCommand_EnabledWhenNicknameSet()
    {
        var vm = CreateVm();
        vm.Nickname = "alice";
        Assert.True(vm.CreateGameCommand.CanExecute(null));
    }

    private LobbyViewModel BuildLobby() => new(_session.Object, _navigation.Object, BuildWaiting);
    private WaitingViewModel BuildWaiting() => new(_session.Object, _navigation.Object, _ => null!);
    private LeaderboardViewModel BuildLeaderboard() => new(_session.Object, _navigation.Object, () => null!);

    [Fact]
    public async Task CreateGame_ConnectsAndCreates_AndNavigatesToWaiting()
    {
        var vm = new MainMenuViewModel(
            _session.Object, _navigation.Object, _player,
            BuildLobby, BuildWaiting, BuildLeaderboard);
        vm.Nickname = "alice";
        _session.SetupGet(s => s.IsConnected).Returns(false);

        await vm.CreateGameCommand.ExecuteAsync(null);

        _session.Verify(s => s.ConnectAsync("127.0.0.1", 5050, It.IsAny<CancellationToken>()), Times.Once);
        _session.Verify(s => s.SayHelloAsync("alice", It.IsAny<CancellationToken>()), Times.Once);
        _session.Verify(s => s.CreateGameAsync(It.IsAny<CancellationToken>()), Times.Once);
        _navigation.Verify(n => n.NavigateTo(It.IsAny<ObservableObject>()), Times.Once);
    }

    [Fact]
    public async Task FindGame_NavigatesToLobby_WithoutCreatingGame()
    {
        var vm = new MainMenuViewModel(
            _session.Object, _navigation.Object, _player,
            BuildLobby, BuildWaiting, BuildLeaderboard);
        vm.Nickname = "bob";
        _session.SetupGet(s => s.IsConnected).Returns(true);

        await vm.FindGameCommand.ExecuteAsync(null);

        _session.Verify(s => s.CreateGameAsync(It.IsAny<CancellationToken>()), Times.Never);
        _navigation.Verify(n => n.NavigateTo(It.IsAny<ObservableObject>()), Times.Once);
    }

    [Fact]
    public async Task ShowLeaderboard_NavigatesAndRequestsLeaderboard()
    {
        var vm = new MainMenuViewModel(
            _session.Object, _navigation.Object, _player,
            BuildLobby, BuildWaiting, BuildLeaderboard);
        vm.Nickname = "carol";
        _session.SetupGet(s => s.IsConnected).Returns(true);

        await vm.ShowLeaderboardCommand.ExecuteAsync(null);

        _navigation.Verify(n => n.NavigateTo(It.IsAny<ObservableObject>()), Times.Once);
        _session.Verify(s => s.RequestLeaderboardAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConnectFailure_SetsStatusMessage_NoNavigation()
    {
        var vm = CreateVm();
        vm.Nickname = "alice";
        _session.SetupGet(s => s.IsConnected).Returns(false);
        _session.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("offline"));

        await vm.CreateGameCommand.ExecuteAsync(null);

        Assert.Contains("offline", vm.StatusMessage);
        _navigation.Verify(n => n.NavigateTo(It.IsAny<ObservableObject>()), Times.Never);
    }
}
