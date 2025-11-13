using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings.Models;
using DualSenseClient.Services;
using DualSenseClient.ViewModels.Controls;

namespace DualSenseClient.ViewModels.Pages;

public partial class DebugPageViewModel : ViewModelBase
{
    private readonly ControllerSelectorViewModel _controllerSelector;
    public ControllerSelectorViewModel ControllerSelector => _controllerSelector;

    private readonly SelectedControllerService _selectedControllerService;
    private readonly DualSenseProfileManager _profileManager;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;
    [ObservableProperty] private ControllerInfo? _selectedControllerInfo;
    [ObservableProperty] private ControllerMonitorViewModel? _monitorViewModel;
    [ObservableProperty] private ControllerProfileViewModel? _profileViewModel;

    public DebugPageViewModel(ControllerSelectorViewModel controllerSelector, SelectedControllerService selectedControllerService, DualSenseProfileManager profileManager)
    {
        Logger.Debug("Creating DebugPageViewModel");

        _controllerSelector = controllerSelector;
        _selectedControllerService = selectedControllerService;
        _profileManager = profileManager;

        _selectedControllerService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
            {
                Logger.Debug($"Selected controller changed in service");
                HandleControllerSelectionChanged();
            }
        };

        // Initialize with current selection
        SelectedController = _selectedControllerService.SelectedController;
        if (SelectedController != null)
        {
            Logger.Info($"Initializing with selected controller: {SelectedController.Controller.Device.GetProductName()}");
            InitializeControllerViewModels();
        }
        else
        {
            Logger.Debug("No controller selected during initialization");
        }

        Logger.Debug("DebugPageViewModel created successfully");
    }

    private void HandleControllerSelectionChanged()
    {
        SelectedController = _selectedControllerService.SelectedController;

        if (SelectedController != null)
        {
            Logger.Info($"Controller selected: {SelectedController.Controller.Device.GetProductName()}");
            InitializeControllerViewModels();
        }
        else
        {
            Logger.Info("Controller deselected");
            CleanupControllerViewModels();
        }
    }

    private void InitializeControllerViewModels()
    {
        Logger.Debug("Initializing controller ViewModels");

        // Cleanup existing
        CleanupControllerViewModels();

        if (SelectedController == null)
        {
            Logger.Warning("Cannot initialize ViewModels: SelectedController is null");
            return;
        }

        try
        {
            // Get or create controller info
            SelectedControllerInfo = _profileManager.GetOrCreateControllerInfo(SelectedController.Controller);
            Logger.Debug($"Controller info: {SelectedControllerInfo.Name} (ID: {SelectedControllerInfo.Id})");

            // Create new ViewModels
            Logger.Debug("Creating ControllerMonitorViewModel");
            MonitorViewModel = new ControllerMonitorViewModel(SelectedController.Controller, SelectedControllerInfo);

            Logger.Debug("Creating ControllerProfileViewModel");
            ProfileViewModel = new ControllerProfileViewModel(SelectedController.Controller, SelectedControllerInfo, _profileManager);

            Logger.Info("Controller ViewModels initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to initialize controller ViewModels");
            Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
            CleanupControllerViewModels();
        }
    }

    private void CleanupControllerViewModels()
    {
        Logger.Debug("Cleaning up controller ViewModels");

        if (MonitorViewModel != null)
        {
            Logger.Trace("Disposing MonitorViewModel");
            MonitorViewModel.Dispose();
            MonitorViewModel = null;
        }

        if (ProfileViewModel != null)
        {
            Logger.Trace("Disposing ProfileViewModel");
            ProfileViewModel.Dispose();
            ProfileViewModel = null;
        }

        SelectedControllerInfo = null;
        Logger.Debug("ViewModels cleanup complete");
    }

    public void Dispose()
    {
        Logger.Debug("Disposing DebugPageViewModel");
        CleanupControllerViewModels();
        Logger.Debug("DebugPageViewModel disposed");
    }
}