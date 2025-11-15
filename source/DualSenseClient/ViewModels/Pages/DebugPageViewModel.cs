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
    private readonly SelectedControllerService _selectedControllerService;
    private readonly DualSenseProfileManager _profileManager;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;
    [ObservableProperty] private ControllerInfo? _selectedControllerInfo;
    [ObservableProperty] private ControllerMonitorViewModel? _monitorViewModel;
    [ObservableProperty] private ControllerProfileViewModel? _profileViewModel;

    public DebugPageViewModel(SelectedControllerService selectedControllerService, DualSenseProfileManager profileManager)
    {
        Logger.Debug<DebugPageViewModel>("Creating DebugPageViewModel");

        _selectedControllerService = selectedControllerService;
        _profileManager = profileManager;

        _selectedControllerService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
            {
                Logger.Debug<DebugPageViewModel>("Selected controller changed in service");
                HandleControllerSelectionChanged();
            }
        };

        // Initialize with current selection
        SelectedController = _selectedControllerService.SelectedController;
        if (SelectedController != null)
        {
            Logger.Info<DebugPageViewModel>($"Initializing with selected controller: {SelectedController.Controller.Device.GetProductName()}");
            InitializeControllerViewModels();
        }
        else
        {
            Logger.Debug<DebugPageViewModel>("No controller selected during initialization");
        }

        Logger.Debug<DebugPageViewModel>("DebugPageViewModel created successfully");
    }

    private void HandleControllerSelectionChanged()
    {
        SelectedController = _selectedControllerService.SelectedController;

        if (SelectedController != null)
        {
            Logger.Info<DebugPageViewModel>($"Controller selected: {SelectedController.Controller.Device.GetProductName()}");
            InitializeControllerViewModels();
        }
        else
        {
            Logger.Info<DebugPageViewModel>("Controller deselected");
            CleanupControllerViewModels();
        }
    }

    private void InitializeControllerViewModels()
    {
        Logger.Debug<DebugPageViewModel>("Initializing controller ViewModels");

        // Cleanup existing
        CleanupControllerViewModels();

        if (SelectedController == null)
        {
            Logger.Warning<DebugPageViewModel>("Cannot initialize ViewModels: SelectedController is null");
            return;
        }

        try
        {
            // Get or create controller info
            SelectedControllerInfo = _profileManager.GetOrCreateControllerInfo(SelectedController.Controller);
            Logger.Debug<DebugPageViewModel>($"Controller info: {SelectedControllerInfo.Name} (ID: {SelectedControllerInfo.Id})");

            // Create new ViewModels
            Logger.Debug<DebugPageViewModel>("Creating ControllerMonitorViewModel");
            MonitorViewModel = new ControllerMonitorViewModel(SelectedController.Controller, SelectedControllerInfo);

            Logger.Debug<DebugPageViewModel>("Creating ControllerProfileViewModel");
            ProfileViewModel = new ControllerProfileViewModel(SelectedController.Controller, SelectedControllerInfo, _profileManager);

            Logger.Info<DebugPageViewModel>("Controller ViewModels initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error<DebugPageViewModel>("Failed to initialize controller ViewModels");
            Logger.LogExceptionDetails<DebugPageViewModel>(ex, includeEnvironmentInfo: false);
            CleanupControllerViewModels();
        }
    }

    private void CleanupControllerViewModels()
    {
        Logger.Debug<DebugPageViewModel>("Cleaning up controller ViewModels");

        if (MonitorViewModel != null)
        {
            Logger.Trace<DebugPageViewModel>("Disposing MonitorViewModel");
            MonitorViewModel.Dispose();
            MonitorViewModel = null;
        }

        if (ProfileViewModel != null)
        {
            Logger.Trace<DebugPageViewModel>("Disposing ProfileViewModel");
            ProfileViewModel.Dispose();
            ProfileViewModel = null;
        }

        SelectedControllerInfo = null;
        Logger.Debug<DebugPageViewModel>("ViewModels cleanup complete");
    }

    public void Dispose()
    {
        Logger.Debug<DebugPageViewModel>("Disposing DebugPageViewModel");
        CleanupControllerViewModels();
        Logger.Debug<DebugPageViewModel>("DebugPageViewModel disposed");
    }
}