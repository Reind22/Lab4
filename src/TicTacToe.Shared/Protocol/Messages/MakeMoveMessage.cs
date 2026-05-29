namespace TicTacToe.Shared.Protocol.Messages;

/// <summary>Ход игрока.</summary>
public sealed record MakeMoveMessage(Guid GameId, int Row, int Col);
