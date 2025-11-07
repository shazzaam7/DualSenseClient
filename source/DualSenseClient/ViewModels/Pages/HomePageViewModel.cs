using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.ViewModels.Controls;

namespace DualSenseClient.ViewModels.Pages;

public partial class HomePageViewModel : ViewModelBase
{
    private readonly ControllerSelectorViewModel _controllerSelector;

    [ObservableProperty] private ControllerViewModel? _selectedController;

    public HomePageViewModel(ControllerSelectorViewModel controllerSelector)
    {
        _controllerSelector = controllerSelector;

        // Bind to the selector's selected controller
        _controllerSelector.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ControllerSelectorViewModel.SelectedController))
            {
                SelectedController = _controllerSelector.SelectedController;
            }
        };

        // Initialize with current selection
        SelectedController = _controllerSelector.SelectedController;
    }

    public ControllerSelectorViewModel ControllerSelector => _controllerSelector;
}