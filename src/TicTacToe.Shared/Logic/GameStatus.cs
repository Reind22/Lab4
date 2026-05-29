namespace TicTacToe.Shared.Logic;

/// <summary>
/// Текущее состояние партии.
/// </summary>
public enum GameStatus
{
    /// <summary>Игра идёт.</summary>
    InProgress = 0,

    /// <summary>Победили крестики.</summary>
    XWin = 1,

    /// <summary>Победили нолики.</summary>
    OWin = 2,

    /// <summary>Ничья — поле заполнено, победителя нет.</summary>
    Draw = 3,
}
