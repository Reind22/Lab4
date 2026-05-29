namespace TicTacToe.Client.Core.ViewModels;

/// <summary>
/// Контейнер с данными текущего игрока, который ViewModel'и пробрасывают между собой
/// (никнейм + параметры подключения).
/// </summary>
public sealed class PlayerSession
{
    public string Nickname { get; set; } = "";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5050;
}
