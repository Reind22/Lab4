using TicTacToe.Shared.Logic;

namespace TicTacToe.Shared.Protocol.Messages;

/// <summary>Текущее состояние партии для рассылки игрокам.</summary>
public sealed record GameStateMessage(int[] Board, CellState CurrentPlayer, GameStatus Status);
