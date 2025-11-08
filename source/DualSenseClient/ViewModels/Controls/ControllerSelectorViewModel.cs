using System.Collections.ObjectModel;
using DualSenseClient.Services;

namespace DualSenseClient.ViewModels.Controls;

public partial class ControllerSelectorViewModel : ViewModelBase
{
    // Properties
    private readonly SelectedControllerService _selectedControllerService;
    private bool _isUpdating;

    public ObservableCollection<ControllerViewModelBase> Controllers => _selectedControllerService.AvailableControllers;

    public bool HasControllers => Controllers.Count > 0;

    // Constructor
    public ControllerViewModelBase? SelectedController
    {
        get => _selectedControllerService.SelectedController;
        set
        {
            if (!_isUpdating && value != _selectedControllerService.SelectedController)
            {
                _selectedControllerService.SelectController(value);
            }
        }
    }

    // Functions
    public ControllerSelectorViewModel(SelectedControllerService selectedControllerService)
    {
        _selectedControllerService = selectedControllerService;

        // Subscribe to changes
        _selectedControllerService.PropertyChanged += OnServicePropertyChanged;
        _selectedControllerService.AvailableControllers.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasControllers));
        };
    }

    private void OnServicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
        {
            _isUpdating = true;
            OnPropertyChanged(nameof(SelectedController));
            _isUpdating = false;
        }
    }
}