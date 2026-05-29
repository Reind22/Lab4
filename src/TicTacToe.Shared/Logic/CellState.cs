namespace TicTacToe.Shared.Logic;

/// <summary>
/// Состояние одной клетки игрового поля.
/// </summary>
public enum CellState
{
    /// <summary>Клетка пуста.</summary>
    Empty = 0,

    /// <summary>Клетка занята крестиком.</summary>
    X = 1,

    /// <summary>Клетка занята ноликом.</summary>
    O = 2,
}
