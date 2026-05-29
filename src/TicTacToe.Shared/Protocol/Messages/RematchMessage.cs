namespace TicTacToe.Shared.Protocol.Messages;

/// <summary>Запрос на повторную партию в той же сессии.</summary>
public sealed record RematchMessage(Guid GameId);
