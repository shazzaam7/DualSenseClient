using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.Settings;
using DualSenseClient.Core.Settings.Models;
using DualSenseClient.Services;
using DualSenseClient.ViewModels.Controls;
using DualSenseClient.Core.Logging;

namespace DualSenseClient.ViewModels.Pages;

public partial class HomePageViewModel : ViewModelBase
{
    private readonly SelectedControllerService _selectedControllerService;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;

    public HomePageViewModel(SelectedControllerService selectedControllerService)
    {
        Logger.Debug("Creating HomePageViewModel");
        _selectedControllerService = selectedControllerService;
        _selectedControllerService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Logger.Debug($"Selected controller changed in service");
                    SelectedController = _selectedControllerService.SelectedController;
                });
            }
        };

        // Initialize with current selection
        SelectedController = _selectedControllerService.SelectedController;
        Logger.Debug("HomePageViewModel created successfully");
    }
}