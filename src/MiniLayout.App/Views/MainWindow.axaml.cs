using Avalonia.Controls;
using Avalonia.Interactivity;
using MiniLayout.App.ViewModels;

namespace MiniLayout.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnRunPythonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.RunPythonScript();
        }
    }

    private void OnAddBoxClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.AddComponent();
        }
    }

    private void OnResetSceneClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ResetScene();
        }
    }
}
