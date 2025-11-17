using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DualSenseClient.Services;
using DualSenseClient.ViewModels.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views.Controls;

public partial class ProfileSelector : UserControl
{
    private readonly IProfileRenameService _renameService;

    public ProfileSelector()
    {
        InitializeComponent();

        // Get the service from the application
        _renameService = App.Services.GetRequiredService<IProfileRenameService>();

        // Find the RenameButton and attach the click event
        RenameButton.Click += OnRenameButtonClick;
    }

    private async void OnRenameButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ControllerProfileViewModel viewModel)
        {
            await _renameService.RenameProfileAsync(viewModel);
        }
    }

    public void DataContextBeginUpdate()
    {
        // Detach the event when changing DataContext to prevent memory leaks
        if (RenameButton != null)
        {
            RenameButton.Click -= OnRenameButtonClick;
        }
    }

    public void DataContextEndUpdate()
    {
        // Reattach the event when DataContext changes
        if (RenameButton != null)
        {
            RenameButton.Click += OnRenameButtonClick;
        }
    }
}