using DualSenseClient.ViewModels;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views;

public partial class MainWindow : AppWindow
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