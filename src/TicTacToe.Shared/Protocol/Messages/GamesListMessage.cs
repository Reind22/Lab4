using TicTacToe.Shared.Protocol.Dtos;

namespace TicTacToe.Shared.Protocol.Messages;

/// <summary>Ответ сервера со списком игр в лобби.</summary>
public sealed record GamesListMessage(IReadOnlyList<GameInfo> Games);
