using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Services;
using DualSenseClient.Core.Logging;

namespace DualSenseClient.ViewModels.Pages;

public partial class HomePageViewModel : ViewModelBase
{
    private readonly SelectedControllerService _selectedControllerService;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;

    public HomePageViewModel(SelectedControllerService selectedControllerService)
    {
        Logger.Debug<HomePageViewModel>("Creating HomePageViewModel");

        _selectedControllerService = selectedControllerService;
        _selectedControllerService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Logger.Debug<HomePageViewModel>("Selected controller changed in service");
                    SelectedController = _selectedControllerService.SelectedController;

                    if (SelectedController != null)
                    {
                        Logger.Info<HomePageViewModel>($"Controller selected: {SelectedController.Name}");
                    }
                    else
                    {
                        Logger.Info<HomePageViewModel>("Controller deselected");
                    }
                });
            }
        };

        // Initialize with current selection
        SelectedController = _selectedControllerService.SelectedController;

        Logger.Debug<HomePageViewModel>(SelectedController != null ? $"Initialized with controller: {SelectedController.Name}" : "Initialized with no controller selected");
        Logger.Debug<HomePageViewModel>("HomePageViewModel created successfully");
    }
}