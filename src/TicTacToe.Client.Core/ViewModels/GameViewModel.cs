using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.Services;
using TicTacToe.Shared.Logic;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Core.ViewModels;

/// <summary>Экран игры: поле 3×3, статус, реванш.</summary>
public sealed partial class GameViewModel : ObservableObject, IDisposable
{
    private readonly IGameSessionService _session;
    private readonly INavigationService _navigation;
    private readonly Func<MainMenuViewModel> _menuFactory;

    public Guid GameId { get; }
    public CellState YourSymbol { get; }
    public string OpponentNickname { get; }

    public ObservableCollection<CellViewModel> Cells { get; }

    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isYourTurn;
    [ObservableProperty] private bool _isGameOver;
    [ObservableProperty] private GameStatus _status = GameStatus.InProgress;

    public GameViewModel(
        IGameSessionService session,
        INavigationService navigation,
        Func<MainMenuViewModel> menuFactory,
        GameStartedMessage start)
    {
        _session = session;
        _navigation = navigation;
        _menuFactory = menuFactory;

        GameId = start.GameId;
        YourSymbol = start.YourSymbol;
        OpponentNickname = start.OpponentNickname;

        Cells = new ObservableCollection<CellViewModel>(
            Enumerable.Range(0, Game.Size * Game.Size)
                      .Select(i => new CellViewModel(i / Game.Size, i % Game.Size)));

        IsYourTurn = YourSymbol == CellState.X;
        UpdateClickability();
        StatusMessage = IsYourTurn ? "Ваш ход" : $"Ход соперника ({OpponentNickname})";

        _session.GameStateChanged += OnGameStateChanged;
        _session.GameOver += OnGameOver;
        _session.ServerError += OnServerError;
    }

    [RelayCommand]
    private async Task MakeMoveAsync(CellViewModel? cell)
    {
        if (cell is null || IsGameOver || !IsYourTurn || cell.State != CellState.Empty) return;
        try
        {
            await _session.MakeMoveAsync(GameId, cell.Row, cell.Col);
        }
        catch (Exception ex) { StatusMessage = $"Ошибка хода: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task RematchAsync()
    {
        try
        {
            await _session.RequestRematchAsync(GameId);
            ResetBoard();
            IsGameOver = false;
            Status = GameStatus.InProgress;
            StatusMessage = "Ожидаем подтверждения реванша…";
        }
        catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
    }

    [RelayCommand]
    private void BackToMenu() => _navigation.NavigateTo(_menuFactory());

    private void OnGameStateChanged(object? sender, GameStateMessage state)
    {
        for (int i = 0; i < Cells.Count && i < state.Board.Length; i++)
            Cells[i].State = (CellState)state.Board[i];

        Status = state.Status;
        IsYourTurn = state.Status == GameStatus.InProgress && state.CurrentPlayer == YourSymbol;
        IsGameOver = state.Status != GameStatus.InProgress;
        UpdateClickability();

        StatusMessage = state.Status switch
        {
            GameStatus.InProgress => IsYourTurn ? "Ваш ход" : $"Ход соперника ({OpponentNickname})",
            _ => StatusMessage
        };
    }

    private void OnGameOver(object? sender, GameOverMessage over)
    {
        IsGameOver = true;
        Status = over.Status;
        IsYourTurn = false;
        UpdateClickability();

        StatusMessage = over.Status switch
        {
            GameStatus.Draw => "Ничья",
            GameStatus.XWin when YourSymbol == CellState.X => "Вы победили!",
            GameStatus.OWin when YourSymbol == CellState.O => "Вы победили!",
            _ => $"Победил {over.Winner}"
        };
    }

    private void OnServerError(object? sender, ErrorMessage error)
    {
        // После конца партии сообщение об уходе соперника уже не важно:
        // результат на экране должен остаться тем, который определил сервер.
        if (IsGameOver && string.Equals(error.Code, "opponent_left", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        StatusMessage = error.Message;
    }

    private void UpdateClickability()
    {
        foreach (var cell in Cells)
            cell.IsClickable = !IsGameOver && IsYourTurn && cell.State == CellState.Empty;
    }

    private void ResetBoard()
    {
        foreach (var cell in Cells) cell.State = CellState.Empty;
        IsYourTurn = YourSymbol == CellState.X;
        UpdateClickability();
    }

    public void Dispose()
    {
        _session.GameStateChanged -= OnGameStateChanged;
        _session.GameOver -= OnGameOver;
        _session.ServerError -= OnServerError;
    }
}
