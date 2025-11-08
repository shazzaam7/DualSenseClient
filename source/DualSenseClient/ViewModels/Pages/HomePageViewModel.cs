using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Services;
using DualSenseClient.ViewModels.Controls;

namespace DualSenseClient.ViewModels.Pages;

public partial class HomePageViewModel : ViewModelBase
{
    private readonly ControllerSelectorViewModel _controllerSelector;
    public ControllerSelectorViewModel ControllerSelector => _controllerSelector;

    private readonly SelectedControllerService _selectedControllerService;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;
    [ObservableProperty] private ControllerMonitorViewModel? _monitorViewModel;

    public HomePageViewModel(ControllerSelectorViewModel controllerSelector, SelectedControllerService selectedControllerService)
    {
        _controllerSelector = controllerSelector;
        _selectedControllerService = selectedControllerService;

        _selectedControllerService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
            {
                SelectedController = _selectedControllerService.SelectedController;

                if (SelectedController != null)
                {
                    MonitorViewModel?.Dispose();
                    MonitorViewModel = new ControllerMonitorViewModel(SelectedController.Controller, null);
                }
                else
                {
                    MonitorViewModel?.Dispose();
                    MonitorViewModel = null;
                }
            }
        };

        SelectedController = _selectedControllerService.SelectedController;
        if (SelectedController != null)
        {
            MonitorViewModel = new ControllerMonitorViewModel(SelectedController.Controller, null);
        }
    }
}