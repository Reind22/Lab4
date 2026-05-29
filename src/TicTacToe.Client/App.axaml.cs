using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Client.Composition;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.ViewModels;

namespace TicTacToe.Client;

public partial class App : Application
{
    /// <summary>Глобальный DI-контейнер приложения.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddClientServices();
        Services = services.BuildServiceProvider();

        // Стартовая ViewModel — главное меню.
        var navigation = Services.GetRequiredService<INavigationService>();
        navigation.NavigateTo(Services.GetRequiredService<MainMenuViewModel>());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = navigation
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
