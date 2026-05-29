using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using TicTacToe.Shared.Logic;

namespace TicTacToe.Client.Converters;

/// <summary>
/// Возвращает цвет символа: X — тёмно-зелёный, O — песочный, Empty — прозрачный.
/// Цвета согласованы с токенами темы (AppXColor / AppOColor).
/// </summary>
public sealed class CellStateToBrushConverter : IValueConverter
{
    public static readonly CellStateToBrushConverter Instance = new();

    private static readonly IBrush XBrush = new SolidColorBrush(Color.Parse("#0C3B2E"));
    private static readonly IBrush OBrush = new SolidColorBrush(Color.Parse("#BB8A52"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CellState s
            ? s switch
            {
                CellState.X => XBrush,
                CellState.O => OBrush,
                _ => Brushes.Transparent,
            }
            : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
