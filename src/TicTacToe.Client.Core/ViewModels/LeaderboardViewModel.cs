using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.Services;
using TicTacToe.Shared.Protocol.Dtos;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.ViewModels;

/// <summary>Таблица рекордов.</summary>
public sealed partial class LeaderboardViewModel : ObservableObject, IDisposable
{
    private readonly IGameSessionService _session;
    private readonly INavigationService _navigation;
    private readonly Func<MainMenuViewModel> _menuFactory;

    public ObservableCollection<LeaderboardEntry> Entries { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _statusMessage;

    public LeaderboardViewModel(
        IGameSessionService session,
        INavigationService navigation,
        Func<MainMenuViewModel> menuFactory)
    {
        _session = session;
        _navigation = navigation;
        _menuFactory = menuFactory;

        _session.LeaderboardReceived += OnLeaderboardReceived;
        _session.ServerError += OnServerError;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try { await _session.RequestLeaderboardAsync(); }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void BackToMenu() => _navigation.NavigateTo(_menuFactory());

    private void OnLeaderboardReceived(object? sender, LeaderboardResponseMessage message)
    {
        Entries.Clear();
        foreach (var entry in message.Entries) Entries.Add(entry);
        IsLoading = false;
    }

    private void OnServerError(object? sender, ErrorMessage error) =>
        StatusMessage = error.Message;

    public void Dispose()
    {
        _session.LeaderboardReceived -= OnLeaderboardReceived;
        _session.ServerError -= OnServerError;
    }
}
