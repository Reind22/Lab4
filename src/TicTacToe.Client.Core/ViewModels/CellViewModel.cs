using CommunityToolkit.Mvvm.ComponentModel;
using TicTacToe.Shared.Logic;

namespace TicTacToe.Client.Core.ViewModels;

/// <summary>Состояние одной клетки игрового поля для отображения в UI.</summary>
public sealed partial class CellViewModel : ObservableObject
{
    public int Row { get; }
    public int Col { get; }

    [ObservableProperty]
    private CellState _state;

    [ObservableProperty]
    private bool _isClickable;

    public CellViewModel(int row, int col)
    {
        Row = row;
        Col = col;
    }
}
