using System.Globalization;
using Avalonia.Data.Converters;
using TicTacToe.Shared.Logic;

namespace TicTacToe.Client.Converters;

/// <summary>
/// Проверяет, что <see cref="CellState"/> равен значению из параметра ("X"/"O"/"Empty"). Возвращает <c>bool</c>.
/// </summary>
public sealed class CellStateEqualsConverter : IValueConverter
{
    public static readonly CellStateEqualsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CellState state) return false;
        if (parameter is not string s) return false;

        return Enum.TryParse<CellState>(s, ignoreCase: true, out var expected) && state == expected;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
