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

        // Subscribe to the selection changed event to auto-apply profiles
        ProfileComboBox.SelectionChanged += OnProfileSelectionChanged;
    }

    private async void OnRenameButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ControllerProfileViewModel viewModel)
        {
            await _renameService.RenameProfileAsync(viewModel);
        }
    }

    private void OnProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ControllerProfileViewModel viewModel && viewModel.SelectedProfile != null)
        {
            // Auto-apply the selected profile
            viewModel.ApplySelectedProfileCommand.Execute(null);
        }
    }

    public void DataContextBeginUpdate()
    {
        // Detach the events when changing DataContext to prevent memory leaks
        if (RenameButton != null)
        {
            RenameButton.Click -= OnRenameButtonClick;
        }

        if (ProfileComboBox != null)
        {
            ProfileComboBox.SelectionChanged -= OnProfileSelectionChanged;
        }
    }

    public void DataContextEndUpdate()
    {
        // Reattach the events when DataContext changes
        if (RenameButton != null)
        {
            RenameButton.Click += OnRenameButtonClick;
        }

        if (ProfileComboBox != null)
        {
            ProfileComboBox.SelectionChanged += OnProfileSelectionChanged;
        }
    }
}