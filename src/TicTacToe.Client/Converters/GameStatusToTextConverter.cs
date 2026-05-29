using System.Globalization;
using Avalonia.Data.Converters;
using TicTacToe.Shared.Logic;

namespace TicTacToe.Client.Converters;

/// <summary>Преобразует <see cref="GameStatus"/> в человекочитаемую строку (для оверлея «Игра окончена»).</summary>
public sealed class GameStatusToTextConverter : IValueConverter
{
    public static readonly GameStatusToTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            GameStatus.XWin => "Победа крестиков",
            GameStatus.OWin => "Победа ноликов",
            GameStatus.Draw => "Ничья",
            _ => string.Empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
