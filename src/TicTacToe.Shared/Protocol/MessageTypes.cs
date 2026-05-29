namespace TicTacToe.Shared.Protocol;

/// <summary>Имена типов сообщений сетевого протокола.</summary>
public static class MessageTypes
{
    public const string Hello = "Hello";
    public const string CreateGame = "CreateGame";
    public const string ListGames = "ListGames";
    public const string GamesList = "GamesList";
    public const string JoinGame = "JoinGame";
    public const string GameStarted = "GameStarted";
    public const string MakeMove = "MakeMove";
    public const string GameState = "GameState";
    public const string GameOver = "GameOver";
    public const string Rematch = "Rematch";
    public const string LeaderboardRequest = "LeaderboardRequest";
    public const string LeaderboardResponse = "LeaderboardResponse";
    public const string Error = "Error";
}
