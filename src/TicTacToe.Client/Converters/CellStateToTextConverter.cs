using System.Globalization;
using Avalonia.Data.Converters;
using TicTacToe.Shared.Logic;

namespace TicTacToe.Client.Converters;

/// <summary>Преобразует <see cref="CellState"/> в строку для отображения в кнопке-клетке.</summary>
public sealed class CellStateToTextConverter : IValueConverter
{
    public static readonly CellStateToTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            CellState.X => "X",
            CellState.O => "O",
            _ => string.Empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
