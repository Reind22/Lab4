using CommunityToolkit.Mvvm.ComponentModel;
using Moq;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.Services;
using TicTacToe.Client.Core.ViewModels;
using TicTacToe.Shared.Logic;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.Tests.ViewModels;

public class GameViewModelTests
{
    private readonly Mock<IGameSessionService> _session = new();
    private readonly Mock<INavigationService> _navigation = new();
    private readonly Guid _gameId = Guid.NewGuid();

    private GameViewModel CreateVm(CellState yourSymbol)
    {
        var start = new GameStartedMessage(_gameId, yourSymbol, "opponent");
        return new GameViewModel(_session.Object, _navigation.Object, () => null!, start);
    }

    [Fact]
    public void NewGame_AsX_HasNineEmptyCellsAndIsYourTurn()
    {
        var vm = CreateVm(CellState.X);

        Assert.Equal(9, vm.Cells.Count);
        Assert.All(vm.Cells, c => Assert.Equal(CellState.Empty, c.State));
        Assert.True(vm.IsYourTurn);
        Assert.All(vm.Cells, c => Assert.True(c.IsClickable));
    }

    [Fact]
    public void NewGame_AsO_NotYourTurn_AndCellsNotClickable()
    {
        var vm = CreateVm(CellState.O);
        Assert.False(vm.IsYourTurn);
        Assert.All(vm.Cells, c => Assert.False(c.IsClickable));
    }

    [Fact]
    public async Task MakeMove_OnYourTurn_CallsSessionWithCoordinates()
    {
        var vm = CreateVm(CellState.X);
        var cell = vm.Cells.First(c => c.Row == 1 && c.Col == 2);

        await vm.MakeMoveCommand.ExecuteAsync(cell);

        _session.Verify(s => s.MakeMoveAsync(_gameId, 1, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MakeMove_NotYourTurn_DoesNothing()
    {
        var vm = CreateVm(CellState.O);
        var cell = vm.Cells[0];

        await vm.MakeMoveCommand.ExecuteAsync(cell);

        _session.Verify(s => s.MakeMoveAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GameStateChanged_UpdatesBoardAndTurn()
    {
        var vm = CreateVm(CellState.X);
        var board = new[] { 1, 0, 0, 0, 2, 0, 0, 0, 0 }; // X в (0,0), O в (1,1)

        _session.Raise(s => s.GameStateChanged += null, this,
            new GameStateMessage(board, CellState.X, GameStatus.InProgress));

        Assert.Equal(CellState.X, vm.Cells[0].State);
        Assert.Equal(CellState.O, vm.Cells[4].State);
        Assert.True(vm.IsYourTurn);
    }

    [Fact]
    public void GameOver_AsWinner_SetsVictoryStatus()
    {
        var vm = CreateVm(CellState.X);

        _session.Raise(s => s.GameOver += null, this, new GameOverMessage(GameStatus.XWin, "you"));

        Assert.True(vm.IsGameOver);
        Assert.Equal(GameStatus.XWin, vm.Status);
        Assert.Contains("Вы победили", vm.StatusMessage);
        Assert.All(vm.Cells, c => Assert.False(c.IsClickable));
    }

    [Fact]
    public void GameOver_Draw_SetsDrawMessage()
    {
        var vm = CreateVm(CellState.X);

        _session.Raise(s => s.GameOver += null, this, new GameOverMessage(GameStatus.Draw, null));

        Assert.True(vm.IsGameOver);
        Assert.Contains("Ничья", vm.StatusMessage);
    }

    [Fact]
    public async Task RematchCommand_CallsSessionAndResetsBoard()
    {
        var vm = CreateVm(CellState.X);
        // занимаем клетку и завершаем игру
        _session.Raise(s => s.GameStateChanged += null, this,
            new GameStateMessage(new[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 }, CellState.O, GameStatus.InProgress));
        _session.Raise(s => s.GameOver += null, this, new GameOverMessage(GameStatus.Draw, null));

        await vm.RematchCommand.ExecuteAsync(null);

        _session.Verify(s => s.RequestRematchAsync(_gameId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(vm.IsGameOver);
        Assert.All(vm.Cells, c => Assert.Equal(CellState.Empty, c.State));
    }

    [Fact]
    public void BackToMenu_NavigatesToMainMenu()
    {
        var player = new PlayerSession();
        var start = new GameStartedMessage(_gameId, CellState.X, "opp");
        var vm = new GameViewModel(_session.Object, _navigation.Object,
            () => new MainMenuViewModel(_session.Object, _navigation.Object, player,
                () => null!, () => null!, () => null!),
            start);

        vm.BackToMenuCommand.Execute(null);

        _navigation.Verify(n => n.NavigateTo(It.IsAny<ObservableObject>()), Times.Once);
    }
}
