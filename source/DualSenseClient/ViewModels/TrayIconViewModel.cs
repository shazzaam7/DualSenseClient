using System;
using CommunityToolkit.Mvvm.Input;
using DualSenseClient.Core.Logging;
using DualSenseClient.Services;

namespace DualSenseClient.ViewModels;

public partial class TrayIconViewModel
{
    private readonly Action? _showMainWindowAction;
    private readonly SelectedControllerService _selectedControllerService;

    public TrayIconViewModel(Action? showMainWindowAction = null, SelectedControllerService? selectedControllerService = null)
    {
        _showMainWindowAction = showMainWindowAction;
        _selectedControllerService = selectedControllerService ?? throw new ArgumentNullException(nameof(selectedControllerService));
    }

    [RelayCommand]
    private void ShowMainWindow()
    {
        _showMainWindowAction?.Invoke();
    }

    [RelayCommand]
    private void ExitApplication()
    {
        try
        {
            if (App.Desktop != null)
            {
                App.Desktop.Shutdown();
            }
        }
        catch (Exception ex)
        {
            Logger.Error<TrayIconViewModel>($"Failed to exit application: {ex.Message}");
        }
    }

    [RelayCommand]
    private void DisconnectController(ControllerViewModelBase controller)
    {
        if (controller.ConnectionType == "Bluetooth")
        {
            Logger.Info<TrayIconViewModel>($"Attempting to disconnect Bluetooth controller: {controller.Name}");
            bool success = controller.Controller.DisconnectBluetooth();

            if (success)
            {
                Logger.Info<TrayIconViewModel>($"Successfully disconnected Bluetooth controller: {controller.Name}");
            }
            else
            {
                Logger.Warning<TrayIconViewModel>($"Failed to disconnect Bluetooth controller: {controller.Name}");
            }
        }
        else
        {
            Logger.Warning<TrayIconViewModel>($"Cannot disconnect controller via Bluetooth: {controller.Name} is connected via {controller.ConnectionType}");
        }
    }
}