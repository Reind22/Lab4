namespace TicTacToe.Server;

/// <summary>Опции сервера, читаемые из <c>appsettings.json</c> (секция Server).</summary>
public sealed class ServerOptions
{
    public const string SectionName = "Server";

    public int Port { get; set; } = 5050;
    public string DbPath { get; set; } = "leaderboard.db";
}
