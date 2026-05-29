namespace TicTacToe.Shared.Logic;

/// <summary>
/// Состояние и логика одной партии «Крестики-нолики» 3×3.
/// Класс не зависит от UI и сети, его одинаково использует и сервер, и клиент.
/// </summary>
public sealed class Game
{
    /// <summary>Размер стороны игрового поля.</summary>
    public const int Size = 3;

    private readonly CellState[,] _board = new CellState[Size, Size];

    /// <summary>Игрок, чей сейчас ход. По правилам игры первыми ходят крестики.</summary>
    public CellState CurrentPlayer { get; private set; } = CellState.X;

    /// <summary>Текущее состояние партии.</summary>
    public GameStatus Status { get; private set; } = GameStatus.InProgress;

    /// <summary>Количество сделанных ходов.</summary>
    public int MoveCount { get; private set; }

    /// <summary>Получить состояние клетки.</summary>
    public CellState GetCell(int row, int col)
    {
        EnsureInBounds(row, col);
        return _board[row, col];
    }

    /// <summary>
    /// Попытаться сделать ход за <see cref="CurrentPlayer"/>. Возвращает <c>false</c>,
    /// если ход некорректен (вне поля, клетка занята, игра уже окончена).
    /// </summary>
    public bool TryMakeMove(int row, int col)
    {
        if (Status != GameStatus.InProgress) return false;
        if (row < 0 || row >= Size || col < 0 || col >= Size) return false;
        if (_board[row, col] != CellState.Empty) return false;

        _board[row, col] = CurrentPlayer;
        MoveCount++;
        Status = EvaluateStatus();

        if (Status == GameStatus.InProgress)
            CurrentPlayer = CurrentPlayer == CellState.X ? CellState.O : CellState.X;

        return true;
    }

    /// <summary>Сбросить партию в начальное состояние.</summary>
    public void Reset()
    {
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                _board[r, c] = CellState.Empty;

        CurrentPlayer = CellState.X;
        Status = GameStatus.InProgress;
        MoveCount = 0;
    }

    /// <summary>Снимок поля в виде одномерного массива (по строкам).</summary>
    public CellState[] Snapshot()
    {
        var snapshot = new CellState[Size * Size];
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                snapshot[r * Size + c] = _board[r, c];
        return snapshot;
    }

    private GameStatus EvaluateStatus()
    {
        for (int i = 0; i < Size; i++)
        {
            if (LineWinner(_board[i, 0], _board[i, 1], _board[i, 2]) is { } rowWin) return rowWin;
            if (LineWinner(_board[0, i], _board[1, i], _board[2, i]) is { } colWin) return colWin;
        }

        if (LineWinner(_board[0, 0], _board[1, 1], _board[2, 2]) is { } diag1) return diag1;
        if (LineWinner(_board[0, 2], _board[1, 1], _board[2, 0]) is { } diag2) return diag2;

        return MoveCount == Size * Size ? GameStatus.Draw : GameStatus.InProgress;
    }

    private static GameStatus? LineWinner(CellState a, CellState b, CellState c)
    {
        if (a == CellState.Empty || a != b || b != c) return null;
        return a == CellState.X ? GameStatus.XWin : GameStatus.OWin;
    }

    private static void EnsureInBounds(int row, int col)
    {
        if (row < 0 || row >= Size) throw new ArgumentOutOfRangeException(nameof(row));
        if (col < 0 || col >= Size) throw new ArgumentOutOfRangeException(nameof(col));
    }
}
