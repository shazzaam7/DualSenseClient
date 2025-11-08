using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Services;
using DualSenseClient.ViewModels.Controls;

namespace DualSenseClient.ViewModels.Pages;

public partial class HomePageViewModel : ViewModelBase
{
    // Properties
    private readonly ControllerSelectorViewModel _controllerSelector;

    public ControllerSelectorViewModel ControllerSelector => _controllerSelector;

    private readonly SelectedControllerService _selectedControllerService;

    [ObservableProperty] private ControllerViewModel? _selectedController;

    // Constructor
    public HomePageViewModel(ControllerSelectorViewModel controllerSelector, SelectedControllerService selectedControllerService)
    {
        _controllerSelector = controllerSelector;
        _selectedControllerService = selectedControllerService;

        // Bind to the service's selected controller
        _selectedControllerService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
            {
                SelectedController = _selectedControllerService.SelectedController;
            }
        };

        // Initialize with current selection
        SelectedController = _selectedControllerService.SelectedController;
    }
}