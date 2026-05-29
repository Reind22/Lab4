using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TicTacToe.Client;

/// <summary>
/// Сопоставляет ViewModel из <c>TicTacToe.Client.Core.ViewModels</c> с View из
/// <c>TicTacToe.Client.Views</c> по соглашению об именовании.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
            return new TextBlock { Text = "null" };

        var vmType = data.GetType();
        var viewTypeName = vmType.FullName!
            .Replace(".Client.Core.ViewModels.", ".Client.Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        // Ищем во всех загруженных сборках (View живёт в TicTacToe.Client).
        var viewType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(viewTypeName))
            .FirstOrDefault(t => t is not null);

        if (viewType is null)
            return new TextBlock { Text = $"Не найден View для {vmType.Name}" };

        return (Control)Activator.CreateInstance(viewType)!;
    }

    public bool Match(object? data) => data is ObservableObject;
}
