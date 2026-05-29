using TicTacToe.Shared.Protocol.Dtos;

namespace TicTacToe.Shared.Protocol.Messages;

/// <summary>Ответ сервера со списком записей таблицы рекордов.</summary>
public sealed record LeaderboardResponseMessage(IReadOnlyList<LeaderboardEntry> Entries);
