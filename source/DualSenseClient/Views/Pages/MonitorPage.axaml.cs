using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DualSenseClient.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views.Pages;

public partial class MonitorPage : UserControl
{
    // Properties
    private MonitorPageViewModel viewModel { get; set; }

    // Constructor
    public MonitorPage()
    {
        InitializeComponent();
        viewModel = App.Services.GetRequiredService<MonitorPageViewModel>();
        DataContext = viewModel;
    }

    // Functions
}