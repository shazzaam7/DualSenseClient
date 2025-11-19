using System;
using CommunityToolkit.Mvvm.Input;
using DualSenseClient.Core.Logging;

namespace DualSenseClient.ViewModels;

public partial class TrayIconViewModel
{
    private readonly Action? _showMainWindowAction;

    public TrayIconViewModel(Action? showMainWindowAction = null)
    {
        _showMainWindowAction = showMainWindowAction;
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
}