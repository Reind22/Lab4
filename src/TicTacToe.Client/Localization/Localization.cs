using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace TicTacToe.Client.Localization;

/// <summary>Опция языка для UI-переключателя.</summary>
public sealed record LanguageOption(string Code, string DisplayName);

/// <summary>
/// Singleton-источник локализованных строк. View биндят свойства через
/// <c>{Binding KeyName, Source={x:Static loc:Localization.Instance}}</c>.
/// При смене культуры синхронно обновляются все биндинги.
/// </summary>
public sealed class Localization : INotifyPropertyChanged
{
    public static Localization Instance { get; } = new();

    private readonly ResourceManager _rm = new(
        "TicTacToe.Client.Resources.Strings",
        typeof(Localization).Assembly);

    private CultureInfo _culture = new("ru");

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<LanguageOption> AvailableLanguages { get; } = new[]
    {
        new LanguageOption("ru", "Русский"),
        new LanguageOption("en", "English"),
    };

    public LanguageOption SelectedLanguage
    {
        get => AvailableLanguages.FirstOrDefault(l => l.Code == _culture.Name)
               ?? AvailableLanguages[0];
        set
        {
            if (value is null) return;
            SetCulture(value.Code);
        }
    }

    public CultureInfo Culture
    {
        get => _culture;
        private set
        {
            if (_culture.Name == value.Name) return;
            _culture = value;
            // Сообщаем «изменилось всё», чтобы все View пересчитали биндинги.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    /// <summary>Переключить активную культуру (например, "ru" или "en").</summary>
    public void SetCulture(string code) => Culture = new CultureInfo(code);

    /// <summary>Прямой доступ к ключу по имени, для динамических случаев.</summary>
    public string Get([CallerMemberName] string? key = null) =>
        _rm.GetString(key ?? string.Empty, _culture) ?? $"!{key}!";

    // Все доступные ключи как именованные свойства.
    public string AppTitle => Get();
    public string BackToMenuButton => Get();
    public string RefreshButton => Get();

    public string MainMenuTitle => Get();
    public string NicknameLabel => Get();
    public string NicknamePlaceholder => Get();
    public string ConnectionExpanderHeader => Get();
    public string HostLabel => Get();
    public string PortLabel => Get();
    public string CreateGameButton => Get();
    public string FindGameButton => Get();
    public string LeaderboardButton => Get();
    public string LanguageLabel => Get();

    public string LobbyTitle => Get();
    public string LobbyJoinButton => Get();

    public string WaitingTitle => Get();

    public string GameYouPlayAs => Get();
    public string GameOpponentLabel => Get();
    public string GameRematchButton => Get();

    public string LeaderboardTitle => Get();
    public string LeaderboardColumnPlayer => Get();
    public string LeaderboardColumnWins => Get();
    public string LeaderboardColumnLosses => Get();
    public string LeaderboardColumnDraws => Get();
}
