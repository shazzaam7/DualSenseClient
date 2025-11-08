using Avalonia.Controls;
using DualSenseClient.ViewModels.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views.Controls;

public partial class ControllerSelector : UserControl
{
    private ControllerSelectorViewModel _viewModel { get; set; }

    public ControllerSelector()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ControllerSelectorViewModel>();
        DataContext = _viewModel;
    }

    public ControllerSelectorViewModel ViewModel => _viewModel;
}