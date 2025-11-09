using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.Settings;
using DualSenseClient.Core.Settings.Models;
using DualSenseClient.Services;
using DualSenseClient.ViewModels.Controls;

namespace DualSenseClient.ViewModels.Pages;

public partial class HomePageViewModel : ViewModelBase
{
    private readonly ControllerSelectorViewModel _controllerSelector;
    public ControllerSelectorViewModel ControllerSelector => _controllerSelector;

    private readonly SelectedControllerService _selectedControllerService;

    private readonly DualSenseProfileManager _profileManager;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;
    [ObservableProperty] private ControllerInfo? _selectedControllerInfo;
    [ObservableProperty] private ControllerMonitorViewModel? _monitorViewModel;
    [ObservableProperty] private ControllerProfileViewModel? _profileViewModel;

    public HomePageViewModel(ControllerSelectorViewModel controllerSelector, SelectedControllerService selectedControllerService, DualSenseProfileManager profileManager)
    {
        _controllerSelector = controllerSelector;
        _selectedControllerService = selectedControllerService;
        _profileManager = profileManager;

        _selectedControllerService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
            {
                SelectedController = _selectedControllerService.SelectedController;

                if (SelectedController != null)
                {
                    SelectedControllerInfo = _profileManager.GetOrCreateControllerInfo(SelectedController!.Controller);
                    MonitorViewModel?.Dispose();
                    ProfileViewModel?.Dispose();
                    MonitorViewModel = new ControllerMonitorViewModel(SelectedController.Controller, SelectedControllerInfo);
                    ProfileViewModel = new ControllerProfileViewModel(SelectedController.Controller, SelectedControllerInfo, _profileManager);
                }
                else
                {
                    MonitorViewModel?.Dispose();
                    ProfileViewModel?.Dispose();
                    MonitorViewModel = null;
                    ProfileViewModel = null;
                }
            }
        };

        SelectedController = _selectedControllerService.SelectedController;
        if (SelectedController != null)
        {
            SelectedControllerInfo = _profileManager.GetOrCreateControllerInfo(SelectedController!.Controller);
            MonitorViewModel = new ControllerMonitorViewModel(SelectedController.Controller, SelectedControllerInfo);
            ProfileViewModel = new ControllerProfileViewModel(SelectedController.Controller, SelectedControllerInfo, _profileManager);
        }
    }
}