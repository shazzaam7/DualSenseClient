using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings.Models;
using DualSenseClient.Services;
using DualSenseClient.ViewModels.Controls;

namespace DualSenseClient.ViewModels.Pages;

public partial class ProfilePageViewModel : ViewModelBase
{
    private readonly SelectedControllerService _selectedControllerService;
    private readonly NavigationService _navigationService;
    private readonly DualSenseProfileManager _profileManager;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;
    [ObservableProperty] private ControllerInfo? _selectedControllerInfo;
    [ObservableProperty] private ControllerProfileViewModel? _profileViewModel;
    [ObservableProperty] private SpecialActionsViewModel? _specialActionsViewModel;

    public ProfilePageViewModel(SelectedControllerService selectedControllerService, DualSenseProfileManager profileManager, NavigationService navigationService)
    {
        Logger.Debug<ProfilePageViewModel>("Creating ProfilePageViewModel");

        _selectedControllerService = selectedControllerService;
        _navigationService = navigationService;
        _profileManager = profileManager;

        _selectedControllerService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
            {
                Logger.Debug<ProfilePageViewModel>("Selected controller changed in service");
                HandleControllerSelectionChanged();
            }
        };

        // Initialize with current selection
        SelectedController = _selectedControllerService.SelectedController;
        if (SelectedController != null)
        {
            Logger.Info<ProfilePageViewModel>($"Initializing with selected controller: {SelectedController.Controller.Device.GetProductName()}");
            InitializeControllerViewModels();
        }
        else
        {
            Logger.Debug<ProfilePageViewModel>("No controller selected during initialization");
            _navigationService.NavigateToTag("Home");
        }

        Logger.Debug<ProfilePageViewModel>("ProfilePageViewModel created successfully");
    }

    private void HandleControllerSelectionChanged()
    {
        // Cleanup existing ViewModels
        CleanupControllerViewModels();

        SelectedController = _selectedControllerService.SelectedController;

        if (SelectedController != null)
        {
            Logger.Info<ProfilePageViewModel>($"Controller selected: {SelectedController.Controller.Device.GetProductName()}");
            InitializeControllerViewModels();
        }
        else
        {
            Logger.Info<ProfilePageViewModel>("Controller deselected");
            _navigationService.NavigateToTag("Home");
        }
    }

    private void InitializeControllerViewModels()
    {
        Logger.Debug<ProfilePageViewModel>("Initializing controller ViewModels");

        if (SelectedController == null)
        {
            Logger.Warning<ProfilePageViewModel>("Cannot initialize ViewModels: SelectedController is null");
            return;
        }

        try
        {
            // Get or create controller info
            SelectedControllerInfo = _profileManager.GetOrCreateControllerInfo(SelectedController.Controller);
            Logger.Debug<ProfilePageViewModel>($"Controller info: {SelectedControllerInfo.Name} (ID: {SelectedControllerInfo.Id})");

            // Create new ViewModels
            Logger.Debug<ProfilePageViewModel>("Creating ControllerProfileViewModel");
            ProfileViewModel = new ControllerProfileViewModel(SelectedController.Controller, SelectedControllerInfo, _profileManager);

            Logger.Debug<ProfilePageViewModel>("Creating SpecialActionsViewModel");
            SpecialActionsViewModel = new SpecialActionsViewModel(SelectedController.Controller, SelectedControllerInfo, _profileManager);

            Logger.Info<ProfilePageViewModel>("Controller ViewModels initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error<ProfilePageViewModel>("Failed to initialize controller ViewModels");
            Logger.LogExceptionDetails<ProfilePageViewModel>(ex, includeEnvironmentInfo: false);
            CleanupControllerViewModels();
        }
    }

    private void CleanupControllerViewModels()
    {
        Logger.Debug<ProfilePageViewModel>("Cleaning up controller ViewModels");

        if (ProfileViewModel != null)
        {
            Logger.Trace<ProfilePageViewModel>("Disposing ProfileViewModel");
            ProfileViewModel.Dispose();
            ProfileViewModel = null;
        }

        if (SpecialActionsViewModel != null)
        {
            Logger.Trace<ProfilePageViewModel>("Disposing SpecialActionsViewModel");
            SpecialActionsViewModel.Dispose();
            SpecialActionsViewModel = null;
        }

        SelectedControllerInfo = null;
        Logger.Debug<ProfilePageViewModel>("ViewModels cleanup complete");
    }

    public void Dispose()
    {
        Logger.Debug<ProfilePageViewModel>("Disposing ProfilePageViewModel");
        CleanupControllerViewModels();
        Logger.Debug<ProfilePageViewModel>("ProfilePageViewModel disposed");
    }
}