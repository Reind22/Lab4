namespace TicTacToe.Shared.Protocol.Dtos;

/// <summary>Сводка по игре в лобби.</summary>
public sealed record GameInfo(Guid GameId, string CreatedBy, DateTimeOffset CreatedAt);
