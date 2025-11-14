using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings.Models;
using DualSenseClient.Services;

namespace DualSenseClient.ViewModels.Pages;

public partial class MonitorPageViewModel : ViewModelBase
{
    private readonly SelectedControllerService _selectedControllerService;
    private readonly DualSenseProfileManager _profileManager;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;
    [ObservableProperty] private ControllerInfo? _selectedControllerInfo;
    [ObservableProperty] private ControllerMonitorViewModel? _monitorViewModel;

    public MonitorPageViewModel(SelectedControllerService selectedControllerService, DualSenseProfileManager profileManager)
    {
        Logger.Debug("MonitorPageViewModel: Creating DebugPageViewModel");
        _selectedControllerService = selectedControllerService;
        _profileManager = profileManager;
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
            SelectedControllerInfo = _profileManager.GetOrCreateControllerInfo(SelectedController.Controller);
            Logger.Debug($"MonitorPageViewModel: Controller info: {SelectedControllerInfo.Name} (ID: {SelectedControllerInfo.Id})");
            InitializeControllerViewModels();
        }
        else
        {
            Logger.Info("MonitorPageViewModel: Controller deselected");
            CleanupControllerViewModels();
            SelectedControllerInfo = null;
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
            MonitorViewModel = new ControllerMonitorViewModel(SelectedController.Controller, SelectedControllerInfo!);
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

        Logger.Debug("MonitorPageViewModel: ViewModels cleanup complete");
    }

    public void Dispose()
    {
        Logger.Debug("MonitorPageViewModel: Disposing DebugPageViewModel");
        CleanupControllerViewModels();
        Logger.Debug("MonitorPageViewModel: DebugPageViewModel disposed");
    }
}