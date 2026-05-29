using CommunityToolkit.Mvvm.ComponentModel;
using Moq;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.Services;
using TicTacToe.Client.Core.ViewModels;
using TicTacToe.Shared.Logic;
using TicTacToe.Shared.Protocol.Dtos;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.Tests.ViewModels;

public class WaitingAndLeaderboardTests
{
    private readonly Mock<IGameSessionService> _session = new();
    private readonly Mock<INavigationService> _navigation = new();

    [Fact]
    public void Waiting_OnGameStarted_NavigatesToGame()
    {
        var vm = new WaitingViewModel(
            _session.Object, _navigation.Object,
            start => new GameViewModel(_session.Object, _navigation.Object, () => null!, start));

        var start = new GameStartedMessage(Guid.NewGuid(), CellState.X, "opponent");
        _session.Raise(s => s.GameStarted += null, this, start);

        _navigation.Verify(n => n.NavigateTo(It.IsAny<ObservableObject>()), Times.Once);
    }

    [Fact]
    public void Leaderboard_OnReceived_PopulatesEntries()
    {
        var vm = new LeaderboardViewModel(_session.Object, _navigation.Object, () => null!);
        var entries = new[]
        {
            new LeaderboardEntry("alice", 5, 2, 1),
            new LeaderboardEntry("bob",   3, 4, 0),
        };

        _session.Raise(s => s.LeaderboardReceived += null, this, new LeaderboardResponseMessage(entries));

        Assert.Equal(2, vm.Entries.Count);
        Assert.Equal(5, vm.Entries[0].Wins);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task Leaderboard_RefreshCommand_RequestsFromSession()
    {
        var vm = new LeaderboardViewModel(_session.Object, _navigation.Object, () => null!);

        await vm.RefreshCommand.ExecuteAsync(null);

        _session.Verify(s => s.RequestLeaderboardAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
