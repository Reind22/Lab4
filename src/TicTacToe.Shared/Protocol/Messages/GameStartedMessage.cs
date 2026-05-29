using TicTacToe.Shared.Logic;

namespace TicTacToe.Shared.Protocol.Messages;

/// <summary>Уведомление, что партия началась.</summary>
public sealed record GameStartedMessage(Guid GameId, CellState YourSymbol, string OpponentNickname);
