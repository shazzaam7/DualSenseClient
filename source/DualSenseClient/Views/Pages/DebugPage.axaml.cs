using Avalonia.Controls;
using DualSenseClient.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views.Pages;

public partial class DebugPage : UserControl
{
    // Properties
    private DebugPageViewModel _viewModel { get; set; }


    // Constructor
    public DebugPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<DebugPageViewModel>();
        DataContext = _viewModel;
    }

    // Functions
}