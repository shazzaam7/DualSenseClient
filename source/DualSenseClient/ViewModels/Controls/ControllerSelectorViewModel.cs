using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DualSenseClient.Core.Logging;
using DualSenseClient.Services;

namespace DualSenseClient.ViewModels.Controls;

public partial class ControllerSelectorViewModel : ViewModelBase
{
    private readonly SelectedControllerService _selectedControllerService;
    private bool _isUpdating;

    public ObservableCollection<ControllerListItemViewModel> Controllers { get; } = new();

    [ObservableProperty] private bool _hasControllers;

    public ControllerSelectorViewModel(SelectedControllerService selectedControllerService)
    {
        _selectedControllerService = selectedControllerService;

        // Subscribe to changes
        _selectedControllerService.PropertyChanged += OnServicePropertyChanged;
        _selectedControllerService.AvailableControllers.CollectionChanged += OnControllersCollectionChanged;

        // Initialize
        UpdateControllersList();
    }

    private void OnServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
        {
            UpdateSelectedStates();
        }
    }

    private void OnControllersCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateControllersList();
    }

    private void UpdateControllersList()
    {
        _isUpdating = true;

        // Dispose old items
        foreach (ControllerListItemViewModel item in Controllers)
        {
            item.Dispose();
        }

        // Clear and rebuild
        Controllers.Clear();

        foreach (var controller in _selectedControllerService.AvailableControllers)
        {
            ControllerListItemViewModel item = new ControllerListItemViewModel(controller, controller == _selectedControllerService.SelectedController,
                _selectedControllerService);
            Controllers.Add(item);
        }

        HasControllers = Controllers.Count > 0;
        _isUpdating = false;
    }

    private void UpdateSelectedStates()
    {
        if (_isUpdating) return;

        foreach (var item in Controllers)
        {
            item.IsSelected = item.Controller == _selectedControllerService.SelectedController;
        }
    }

    [RelayCommand]
    private void SelectController(ControllerListItemViewModel item)
    {
        if (item != null)
        {
            _selectedControllerService.SelectController(item.Controller);
        }
    }
}

/// <summary>
/// Wrapper ViewModel for individual controller items in the list
/// </summary>
public partial class ControllerListItemViewModel : ObservableObject
{
    private readonly SelectedControllerService _selectedControllerService;

    public ControllerViewModelBase Controller { get; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isRenaming;
    [ObservableProperty] private string _editingName = string.Empty;

    // Proxy properties for easier binding
    public string Name => Controller.Name;
    public string ConnectionType => Controller.ConnectionType;
    public string ConnectionIcon => Controller.ConnectionIcon;
    public string MacAddress => Controller.MacAddress;
    public string BatteryText => Controller.BatteryText;
    public string BatteryIcon => Controller.BatteryIcon;
    public string ChargingIcon => Controller.ChargingIcon;
    public bool IsCharging => Controller.IsCharging;
    public double BatteryLevel => Controller.BatteryLevel;

    public ControllerListItemViewModel(ControllerViewModelBase controller, bool isSelected, SelectedControllerService selectedControllerService)
    {
        Controller = controller;
        IsSelected = isSelected;
        _selectedControllerService = selectedControllerService;
        EditingName = controller.Name;

        // Subscribe to controller property changes to update proxy properties
        Controller.PropertyChanged += OnControllerPropertyChanged;
    }

    private void OnControllerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Update relevant property
        switch (e.PropertyName)
        {
            case nameof(ControllerViewModelBase.Name):
                OnPropertyChanged(nameof(Name));
                EditingName = Controller.Name; // Keep editing name in sync
                break;
            case nameof(ControllerViewModelBase.BatteryText):
                OnPropertyChanged(nameof(BatteryText));
                break;
            case nameof(ControllerViewModelBase.BatteryIcon):
                OnPropertyChanged(nameof(BatteryIcon));
                break;
            case nameof(ControllerViewModelBase.ChargingIcon):
                OnPropertyChanged(nameof(ChargingIcon));
                break;
            case nameof(ControllerViewModelBase.IsCharging):
                OnPropertyChanged(nameof(IsCharging));
                break;
            case nameof(ControllerViewModelBase.BatteryLevel):
                OnPropertyChanged(nameof(BatteryLevel));
                break;
        }
    }

    [RelayCommand]
    private void StartRenaming()
    {
        Logger.Debug($"Starting rename for controller: {Name}");
        EditingName = Controller.Name;
        IsRenaming = true;
    }

    [RelayCommand]
    private void SaveName()
    {
        if (string.IsNullOrWhiteSpace(EditingName))
        {
            Logger.Warning("Cannot save empty controller name");
            CancelRenaming();
            return;
        }

        if (EditingName != Controller.Name)
        {
            Logger.Info($"Renaming controller from '{Controller.Name}' to '{EditingName}'");
            _selectedControllerService.UpdateControllerName(Controller.ControllerId, EditingName);
        }

        IsRenaming = false;
    }

    [RelayCommand]
    private void CancelRenaming()
    {
        Logger.Debug("Cancelled renaming");
        EditingName = Controller.Name;
        IsRenaming = false;
    }

    [RelayCommand]
    private async Task CopyMacAddress()
    {
        try
        {
            if (App.MainWindow?.Clipboard != null)
            {
                await App.MainWindow.Clipboard.SetTextAsync(MacAddress);
                Logger.Info($"MAC address copied to clipboard: {MacAddress}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to copy MAC address: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Controller.PropertyChanged -= OnControllerPropertyChanged;
    }
}