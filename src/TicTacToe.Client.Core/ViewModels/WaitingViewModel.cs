using CommunityToolkit.Mvvm.ComponentModel;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.Services;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.ViewModels;

/// <summary>Экран ожидания второго игрока. По <see cref="IGameSessionService.GameStarted"/>
/// автоматически переключается на <see cref="GameViewModel"/>.</summary>
public sealed partial class WaitingViewModel : ObservableObject, IDisposable
{
    private readonly IGameSessionService _session;
    private readonly INavigationService _navigation;
    private readonly Func<GameStartedMessage, GameViewModel> _gameFactory;

    [ObservableProperty] private string _message = "Ожидаем второго игрока…";

    public WaitingViewModel(
        IGameSessionService session,
        INavigationService navigation,
        Func<GameStartedMessage, GameViewModel> gameFactory)
    {
        _session = session;
        _navigation = navigation;
        _gameFactory = gameFactory;

        _session.GameStarted += OnGameStarted;
        _session.ServerError += OnServerError;
    }

    private void OnGameStarted(object? sender, GameStartedMessage e) =>
        _navigation.NavigateTo(_gameFactory(e));

    private void OnServerError(object? sender, ErrorMessage error) =>
        Message = error.Message;

    public void Dispose()
    {
        _session.GameStarted -= OnGameStarted;
        _session.ServerError -= OnServerError;
    }
}
