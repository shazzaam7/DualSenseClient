using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Services;

namespace DualSenseClient.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SelectedControllerService _selectedControllerService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private bool _hasSelectedController;

    public MainViewModel(SelectedControllerService selectedControllerService, NavigationService navigationService)
    {
        _selectedControllerService = selectedControllerService;
        _navigationService = navigationService;

        _selectedControllerService.PropertyChanged += OnSelectedControllerServicePropertyChanged;

        // Initialize
        UpdateHasSelectedController();
    }

    private void OnSelectedControllerServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
        {
            bool hadController = HasSelectedController;
            UpdateHasSelectedController();

            // If we lost the controller and were on Monitor page, navigate to Home
            if (hadController && !HasSelectedController && _navigationService.CurrentTag == "Monitor")
            {
                _navigationService.NavigateToTag("Home");
            }
        }
    }

    private void UpdateHasSelectedController()
    {
        HasSelectedController = _selectedControllerService.SelectedController != null;
    }
}