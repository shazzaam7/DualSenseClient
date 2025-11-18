using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DualSenseClient.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views.Pages;

public partial class ProfilePage : UserControl
{
    // Properties
    private ProfilePageViewModel viewModel { get; set; }

    // Constructor
    public ProfilePage()
    {
        InitializeComponent();
        viewModel = App.Services.GetRequiredService<ProfilePageViewModel>();
        DataContext = viewModel;
    }
}