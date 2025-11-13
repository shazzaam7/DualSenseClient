using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DualSenseClient.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views.Pages;

public partial class DevicesPage : UserControl
{
    // Properties
    private DevicesPageViewModel viewModel { get; set; }

    // Constructor
    public DevicesPage()
    {
        InitializeComponent();
        viewModel = App.Services.GetRequiredService<DevicesPageViewModel>();
        DataContext = viewModel;
    }

    // Functions
}