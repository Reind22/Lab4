using System.ComponentModel.DataAnnotations;

namespace TicTacToe.Server.Persistence;

/// <summary>Запись о статистике игрока.</summary>
public sealed class Score
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string Nickname { get; set; } = string.Empty;

    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
}
