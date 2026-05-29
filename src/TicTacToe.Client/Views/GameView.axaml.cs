using Avalonia.Controls;
using Avalonia.Input;
using TicTacToe.Client.Core.ViewModels;
using TicTacToe.Shared.Logic;

namespace TicTacToe.Client.Views;

public partial class GameView : UserControl
{
    public GameView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Делает ход прямо из PointerPressed на контейнере поля. Индекс ячейки
    /// вычисляем из координат клика — это надёжнее, чем полагаться на Button.Click,
    /// который в FluentTheme не всегда выстреливает поверх стилизованных шаблонов.
    /// </summary>
    private void OnFieldPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control field) return;
        if (DataContext is not GameViewModel vm) return;
        if (!vm.IsYourTurn || vm.IsGameOver) return;

        // Только основная (левая) кнопка мыши.
        if (!e.GetCurrentPoint(field).Properties.IsLeftButtonPressed) return;

        var pos = e.GetPosition(field);
        var size = field.Bounds;
        if (size.Width <= 0 || size.Height <= 0) return;

        var col = (int)(pos.X / (size.Width / Game.Size));
        var row = (int)(pos.Y / (size.Height / Game.Size));
        if (col < 0 || col >= Game.Size || row < 0 || row >= Game.Size) return;

        var idx = row * Game.Size + col;
        if (idx < 0 || idx >= vm.Cells.Count) return;

        var cell = vm.Cells[idx];
        if (cell.State != CellState.Empty) return;

        if (vm.MakeMoveCommand.CanExecute(cell))
            vm.MakeMoveCommand.Execute(cell);
    }
}
