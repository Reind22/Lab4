using Avalonia.Controls;
using TicTacToe.Client.Core.Navigation;

namespace TicTacToe.Client;

public partial class MainWindow : Window
{
    private INavigationService? _navigation;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_navigation is not null)
            _navigation.CurrentChanged -= OnCurrentChanged;

        _navigation = DataContext as INavigationService;

        if (_navigation is not null)
        {
            _navigation.CurrentChanged += OnCurrentChanged;
            ShellContent.Content = _navigation.Current;
        }
    }

    private void OnCurrentChanged(object? sender, EventArgs e)
    {
        if (_navigation is not null)
            ShellContent.Content = _navigation.Current;
    }
}
