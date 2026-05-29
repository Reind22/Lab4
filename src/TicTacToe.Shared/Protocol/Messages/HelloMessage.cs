namespace TicTacToe.Shared.Protocol.Messages;

/// <summary>Приветствие клиента — сообщает свой ник.</summary>
public sealed record HelloMessage(string Nickname);
