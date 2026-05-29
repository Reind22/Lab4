using TicTacToe.Shared.Logic;

namespace TicTacToe.Shared.Tests;

public class GameTests
{
    [Fact]
    public void NewGame_StartsEmpty_WithXToMove()
    {
        var game = new Game();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(CellState.X, game.CurrentPlayer);
        Assert.Equal(0, game.MoveCount);

        for (int r = 0; r < Game.Size; r++)
            for (int c = 0; c < Game.Size; c++)
                Assert.Equal(CellState.Empty, game.GetCell(r, c));
    }

    [Fact]
    public void TryMakeMove_ValidMove_SwitchesPlayerAndIncrementsCount()
    {
        var game = new Game();

        Assert.True(game.TryMakeMove(0, 0));

        Assert.Equal(CellState.X, game.GetCell(0, 0));
        Assert.Equal(CellState.O, game.CurrentPlayer);
        Assert.Equal(1, game.MoveCount);
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    [Fact]
    public void TryMakeMove_OnOccupiedCell_ReturnsFalseAndDoesNothing()
    {
        var game = new Game();
        game.TryMakeMove(1, 1);

        Assert.False(game.TryMakeMove(1, 1));
        Assert.Equal(CellState.X, game.GetCell(1, 1));
        Assert.Equal(CellState.O, game.CurrentPlayer);
        Assert.Equal(1, game.MoveCount);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    public void TryMakeMove_OutOfBounds_ReturnsFalse(int row, int col)
    {
        var game = new Game();

        Assert.False(game.TryMakeMove(row, col));
        Assert.Equal(0, game.MoveCount);
    }

    public static IEnumerable<object[]> RowWinSequences =>
        Enumerable.Range(0, Game.Size).Select(r => new object[] { r });

    [Theory]
    [MemberData(nameof(RowWinSequences))]
    public void XWinsByRow(int row)
    {
        var game = new Game();
        // X: row,0 ; O: filler ; X: row,1 ; O: filler ; X: row,2
        var fillerCol = (row + 1) % Game.Size;
        var fillerRow = (row + 1) % Game.Size;

        Assert.True(game.TryMakeMove(row, 0));
        Assert.True(game.TryMakeMove(fillerRow, fillerCol));
        Assert.True(game.TryMakeMove(row, 1));
        Assert.True(game.TryMakeMove(fillerRow, (fillerCol + 1) % Game.Size));
        Assert.True(game.TryMakeMove(row, 2));

        Assert.Equal(GameStatus.XWin, game.Status);
    }

    [Fact]
    public void OWinsByColumn()
    {
        var game = new Game();
        // X(0,0) O(0,1) X(1,0) O(1,1) X(0,2) O(2,1) → O column 1
        Assert.True(game.TryMakeMove(0, 0));
        Assert.True(game.TryMakeMove(0, 1));
        Assert.True(game.TryMakeMove(1, 0));
        Assert.True(game.TryMakeMove(1, 1));
        Assert.True(game.TryMakeMove(0, 2));
        Assert.True(game.TryMakeMove(2, 1));

        Assert.Equal(GameStatus.OWin, game.Status);
    }

    [Fact]
    public void XWinsByMainDiagonal()
    {
        var game = new Game();
        // X(0,0) O(0,1) X(1,1) O(0,2) X(2,2)
        Assert.True(game.TryMakeMove(0, 0));
        Assert.True(game.TryMakeMove(0, 1));
        Assert.True(game.TryMakeMove(1, 1));
        Assert.True(game.TryMakeMove(0, 2));
        Assert.True(game.TryMakeMove(2, 2));

        Assert.Equal(GameStatus.XWin, game.Status);
    }

    [Fact]
    public void XWinsByAntiDiagonal()
    {
        var game = new Game();
        // X(0,2) O(0,0) X(1,1) O(0,1) X(2,0)
        Assert.True(game.TryMakeMove(0, 2));
        Assert.True(game.TryMakeMove(0, 0));
        Assert.True(game.TryMakeMove(1, 1));
        Assert.True(game.TryMakeMove(0, 1));
        Assert.True(game.TryMakeMove(2, 0));

        Assert.Equal(GameStatus.XWin, game.Status);
    }

    [Fact]
    public void Draw_FullBoardWithoutWinner()
    {
        var game = new Game();
        // X O X
        // X O O
        // O X X
        var moves = new (int r, int c)[]
        {
            (0,0), (0,1), (0,2), (1,1),
            (1,0), (2,0), (2,2), (1,2),
            (2,1)
        };

        foreach (var (r, c) in moves)
            Assert.True(game.TryMakeMove(r, c));

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Equal(9, game.MoveCount);
    }

    [Fact]
    public void TryMakeMove_AfterGameOver_ReturnsFalse()
    {
        var game = new Game();
        // Quick X win: row 0
        game.TryMakeMove(0, 0);
        game.TryMakeMove(1, 0);
        game.TryMakeMove(0, 1);
        game.TryMakeMove(1, 1);
        game.TryMakeMove(0, 2);

        Assert.Equal(GameStatus.XWin, game.Status);
        Assert.False(game.TryMakeMove(2, 2));
    }

    [Fact]
    public void Reset_ClearsBoardAndState()
    {
        var game = new Game();
        game.TryMakeMove(0, 0);
        game.TryMakeMove(1, 1);

        game.Reset();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(CellState.X, game.CurrentPlayer);
        Assert.Equal(0, game.MoveCount);
        Assert.Equal(CellState.Empty, game.GetCell(0, 0));
        Assert.Equal(CellState.Empty, game.GetCell(1, 1));
    }

    [Fact]
    public void Snapshot_ReturnsRowMajorFlatArray()
    {
        var game = new Game();
        game.TryMakeMove(0, 0); // X
        game.TryMakeMove(2, 2); // O

        var snap = game.Snapshot();

        Assert.Equal(9, snap.Length);
        Assert.Equal(CellState.X, snap[0]);
        Assert.Equal(CellState.O, snap[8]);
        Assert.Equal(CellState.Empty, snap[4]);
    }

    [Fact]
    public void GetCell_OutOfBounds_Throws()
    {
        var game = new Game();
        Assert.Throws<ArgumentOutOfRangeException>(() => game.GetCell(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => game.GetCell(0, 3));
    }
}
