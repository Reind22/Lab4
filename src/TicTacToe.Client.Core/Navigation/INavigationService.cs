using CommunityToolkit.Mvvm.ComponentModel;

namespace TicTacToe.Client.Core.Navigation;

/// <summary>
/// Сервис навигации между ViewModel'ями. View подписывается на
/// <see cref="CurrentChanged"/>, чтобы переключать соответствующий View.
/// </summary>
public interface INavigationService
{
    /// <summary>Активная ViewModel.</summary>
    ObservableObject? Current { get; }

    /// <summary>Срабатывает при смене активной ViewModel.</summary>
    event EventHandler? CurrentChanged;

    /// <summary>Установить ViewModel в качестве активной.</summary>
    void NavigateTo(ObservableObject viewModel);
}
