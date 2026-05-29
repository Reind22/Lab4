using TicTacToe.Shared.Logic;

namespace TicTacToe.Shared.Protocol.Messages;

/// <summary>Партия завершена.</summary>
public sealed record GameOverMessage(GameStatus Status, string? Winner);
