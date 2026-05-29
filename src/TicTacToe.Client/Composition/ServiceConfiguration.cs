using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Client.Core.Navigation;
using TicTacToe.Client.Core.Networking;
using TicTacToe.Client.Core.Services;
using TicTacToe.Client.Core.ViewModels;
using TicTacToe.Shared.Protocol.Messages;

namespace TicTacToe.Client.Composition;

/// <summary>Регистрирует серверы и ViewModel'и в DI-контейнере.</summary>
public static class ServiceConfiguration
{
    public static IServiceCollection AddClientServices(this IServiceCollection services)
    {
        // Singleton-инфраструктура.
        services.AddSingleton<INetworkClient, TcpNetworkClient>();
        services.AddSingleton<IGameSessionService, GameSessionService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<PlayerSession>();

        // ViewModel'и: каждая фабрика создаёт свежий экземпляр.
        services.AddTransient<MainMenuViewModel>();
        services.AddTransient<LobbyViewModel>();
        services.AddTransient<WaitingViewModel>();
        services.AddTransient<LeaderboardViewModel>();

        // GameViewModel принимает GameStartedMessage — параметризованная фабрика.
        services.AddTransient<Func<GameStartedMessage, GameViewModel>>(sp =>
            start => ActivatorUtilities.CreateInstance<GameViewModel>(sp, start));

        // Func<T> — стандартный паттерн для параметризованных VM.
        services.AddTransient<Func<MainMenuViewModel>>(sp => sp.GetRequiredService<MainMenuViewModel>);
        services.AddTransient<Func<LobbyViewModel>>(sp => sp.GetRequiredService<LobbyViewModel>);
        services.AddTransient<Func<WaitingViewModel>>(sp => sp.GetRequiredService<WaitingViewModel>);
        services.AddTransient<Func<LeaderboardViewModel>>(sp => sp.GetRequiredService<LeaderboardViewModel>);

        return services;
    }
}
