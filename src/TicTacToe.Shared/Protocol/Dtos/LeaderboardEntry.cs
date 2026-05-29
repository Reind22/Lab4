namespace TicTacToe.Shared.Protocol.Dtos;

/// <summary>Запись таблицы рекордов.</summary>
public sealed record LeaderboardEntry(string Nickname, int Wins, int Losses, int Draws);
