using Avalonia.Controls;
using DualSenseClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views;

public partial class MainWindow : Window
{
    // Properties
    private MainWindowViewModel _viewModel { get; set; }

    // Constructors
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainWindowViewModel>();
        DataContext = _viewModel;
    }

    // Functions
}