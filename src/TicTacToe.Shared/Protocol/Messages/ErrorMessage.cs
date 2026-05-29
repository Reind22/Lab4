namespace TicTacToe.Shared.Protocol.Messages;

/// <summary>Ошибка от сервера к клиенту.</summary>
public sealed record ErrorMessage(string Code, string Message);
