using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings.Models;
using DualSenseClient.Services;

namespace DualSenseClient.ViewModels.Pages;

public partial class MonitorPageViewModel : ViewModelBase
{
    private readonly SelectedControllerService _selectedControllerService;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;
    [ObservableProperty] private ControllerInfo? _selectedControllerInfo;
    [ObservableProperty] private ControllerMonitorViewModel? _monitorViewModel;

    public MonitorPageViewModel(SelectedControllerService selectedControllerService)
    {
        Logger.Debug("MonitorPageViewModel: Creating DebugPageViewModel");
        _selectedControllerService = selectedControllerService;
        _selectedControllerService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
            {
                Logger.Debug($"MonitorPageViewModel: Selected controller changed in service");
                HandleControllerSelectionChanged();
            }
        };

        HandleControllerSelectionChanged();
        Logger.Debug("MonitorPageViewModel created successfully");
    }

    private void HandleControllerSelectionChanged()
    {
        SelectedController = _selectedControllerService.SelectedController;

        if (SelectedController != null)
        {
            Logger.Info($"MonitorPageViewModel: Controller selected: {SelectedController.Controller.Device.GetProductName()}");
            InitializeControllerViewModels();
        }
        else
        {
            Logger.Info("MonitorPageViewModel: Controller deselected");
            CleanupControllerViewModels();
        }
    }

    private void InitializeControllerViewModels()
    {
        Logger.Debug("MonitorPageViewModel: Initializing controller ViewModels");

        // Cleanup existing
        CleanupControllerViewModels();

        if (SelectedController == null)
        {
            Logger.Warning("MonitorPageViewModel: Cannot initialize ViewModels: SelectedController is null");
            return;
        }

        try
        {
            // Create new ViewModels
            Logger.Debug("MonitorPageViewModel: Creating ControllerMonitorViewModel");
            MonitorViewModel = new ControllerMonitorViewModel(SelectedController.Controller, null);
        }
        catch (Exception ex)
        {
            Logger.Error("MonitorPageViewModel: Failed to initialize controller ViewModels");
            Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
            CleanupControllerViewModels();
        }
    }

    private void CleanupControllerViewModels()
    {
        Logger.Debug("MonitorPageViewModel: Cleaning up controller ViewModels");

        if (MonitorViewModel != null)
        {
            Logger.Trace("Disposing MonitorViewModel");
            MonitorViewModel.Dispose();
            MonitorViewModel = null;
        }

        SelectedControllerInfo = null;
        Logger.Debug("MonitorPageViewModel: ViewModels cleanup complete");
    }

    public void Dispose()
    {
        Logger.Debug("MonitorPageViewModel: Disposing DebugPageViewModel");
        CleanupControllerViewModels();
        Logger.Debug("MonitorPageViewModel: DebugPageViewModel disposed");
    }
}