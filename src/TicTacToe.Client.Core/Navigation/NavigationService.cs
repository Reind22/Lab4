using CommunityToolkit.Mvvm.ComponentModel;

namespace TicTacToe.Client.Core.Navigation;

/// <summary>Простая реализация навигации: одна активная ViewModel + событие.</summary>
public sealed class NavigationService : INavigationService
{
    private ObservableObject? _current;

    public ObservableObject? Current => _current;

    public event EventHandler? CurrentChanged;

    public void NavigateTo(ObservableObject viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (ReferenceEquals(_current, viewModel)) return;

        _current = viewModel;
        CurrentChanged?.Invoke(this, EventArgs.Empty);
    }
}
