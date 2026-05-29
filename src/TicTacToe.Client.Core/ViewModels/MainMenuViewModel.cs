using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.Services;

namespace TicTacToe.Client.Core.ViewModels;

/// <summary>Главное меню: ввод никнейма, выбор действия.</summary>
public sealed partial class MainMenuViewModel : ObservableObject
{
    private readonly IGameSessionService _session;
    private readonly INavigationService _navigation;
    private readonly PlayerSession _player;
    private readonly Func<LobbyViewModel> _lobbyFactory;
    private readonly Func<WaitingViewModel> _waitingFactory;
    private readonly Func<LeaderboardViewModel> _leaderboardFactory;

    public MainMenuViewModel(
        IGameSessionService session,
        INavigationService navigation,
        PlayerSession player,
        Func<LobbyViewModel> lobbyFactory,
        Func<WaitingViewModel> waitingFactory,
        Func<LeaderboardViewModel> leaderboardFactory)
    {
        _session = session;
        _navigation = navigation;
        _player = player;
        _lobbyFactory = lobbyFactory;
        _waitingFactory = waitingFactory;
        _leaderboardFactory = leaderboardFactory;

        _nickname = player.Nickname;
        _host = player.Host;
        _port = player.Port;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateGameCommand))]
    [NotifyCanExecuteChangedFor(nameof(FindGameCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowLeaderboardCommand))]
    private string _nickname;

    [ObservableProperty] private string _host;
    [ObservableProperty] private int _port;
    [ObservableProperty] private string? _statusMessage;

    private bool CanProceed => !string.IsNullOrWhiteSpace(Nickname);

    [RelayCommand(CanExecute = nameof(CanProceed))]
    private async Task CreateGameAsync()
    {
        if (!await EnsureConnectedAsync()) return;
        await _session.CreateGameAsync();
        _navigation.NavigateTo(_waitingFactory());
    }

    [RelayCommand(CanExecute = nameof(CanProceed))]
    private async Task FindGameAsync()
    {
        if (!await EnsureConnectedAsync()) return;
        _navigation.NavigateTo(_lobbyFactory());
    }

    [RelayCommand(CanExecute = nameof(CanProceed))]
    private async Task ShowLeaderboardAsync()
    {
        if (!await EnsureConnectedAsync()) return;
        var vm = _leaderboardFactory();
        _navigation.NavigateTo(vm);
        await _session.RequestLeaderboardAsync();
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        SyncToPlayer();
        try
        {
            if (!_session.IsConnected)
            {
                await _session.ConnectAsync(Host, Port);
                await _session.SayHelloAsync(Nickname);
            }
            StatusMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось подключиться: {ex.Message}";
            return false;
        }
    }

    private void SyncToPlayer()
    {
        _player.Nickname = Nickname;
        _player.Host = Host;
        _player.Port = Port;
    }
}
