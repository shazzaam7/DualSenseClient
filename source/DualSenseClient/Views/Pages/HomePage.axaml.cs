using Avalonia.Controls;
using DualSenseClient.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views.Pages;

public partial class HomePage : UserControl
{
    // Properties
    private HomePageViewModel _viewModel { get; set; }

    // Constructor
    public HomePage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<HomePageViewModel>();
        DataContext = _viewModel;
    }

    // Functions
}