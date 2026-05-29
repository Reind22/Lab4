namespace TicTacToe.Shared.Protocol.Messages;

/// <summary>Запрос на подключение к существующей игре.</summary>
public sealed record JoinGameMessage(Guid GameId);
