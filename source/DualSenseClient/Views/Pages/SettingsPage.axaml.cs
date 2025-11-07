using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DualSenseClient.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views.Pages;

public partial class SettingsPage : UserControl
{
    // Properties
    private SettingsPageViewModel viewModel { get; set; }

    // Constructor
    public SettingsPage()
    {
        InitializeComponent();
        viewModel = App.Services.GetRequiredService<SettingsPageViewModel>();
        DataContext = viewModel;
    }

    // Functions
}