using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.Services;
using TicTacToe.Shared.Protocol.Dtos;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.ViewModels;

/// <summary>Лобби: список открытых игр + присоединение к одной из них.</summary>
public sealed partial class LobbyViewModel : ObservableObject, IDisposable
{
    private readonly IGameSessionService _session;
    private readonly INavigationService _navigation;
    private readonly Func<WaitingViewModel> _waitingFactory;

    public ObservableCollection<GameInfo> Games { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinGameCommand))]
    private GameInfo? _selectedGame;

    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string? _statusMessage;

    public LobbyViewModel(
        IGameSessionService session,
        INavigationService navigation,
        Func<WaitingViewModel> waitingFactory)
    {
        _session = session;
        _navigation = navigation;
        _waitingFactory = waitingFactory;

        _session.GamesListReceived += OnGamesListReceived;
        _session.ServerError += OnServerError;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try { await _session.RequestGamesListAsync(); }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
        finally { IsRefreshing = false; }
    }

    private bool CanJoin => SelectedGame is not null;

    [RelayCommand(CanExecute = nameof(CanJoin))]
    private async Task JoinGameAsync()
    {
        if (SelectedGame is null) return;
        try
        {
            await _session.JoinGameAsync(SelectedGame.GameId);
            _navigation.NavigateTo(_waitingFactory());
        }
        catch (Exception ex) { StatusMessage = $"Не удалось присоединиться: {ex.Message}"; }
    }

    private void OnGamesListReceived(object? sender, GamesListMessage message)
    {
        Games.Clear();
        foreach (var info in message.Games) Games.Add(info);
    }

    private void OnServerError(object? sender, ErrorMessage error) =>
        StatusMessage = $"Сервер: {error.Code} — {error.Message}";

    public void Dispose()
    {
        _session.GamesListReceived -= OnGamesListReceived;
        _session.ServerError -= OnServerError;
    }
}
