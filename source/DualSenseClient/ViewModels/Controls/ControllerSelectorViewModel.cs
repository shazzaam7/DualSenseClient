using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.Settings;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.ViewModels.Controls;

public partial class ControllerSelectorViewModel : ViewModelBase, IDisposable
{
    private readonly DualSenseManager _dualSenseManager;
    private readonly ISettingsManager _settingsManager;

    [ObservableProperty] private ObservableCollection<ControllerViewModel> _controllers = [];

    [ObservableProperty] private ControllerViewModel? _selectedController;

    [ObservableProperty] private bool _hasControllers;

    public ControllerSelectorViewModel(DualSenseManager dualSenseManager, ISettingsManager settingsManager)
    {
        _dualSenseManager = dualSenseManager;
        _settingsManager = settingsManager;

        // Subscribe to controller events
        _dualSenseManager.ControllerConnected += OnControllerConnected;
        _dualSenseManager.ControllerDisconnected += OnControllerDisconnected;

        // Load existing controllers
        RefreshControllers();
    }

    private void OnControllerConnected(object? sender, DualSenseController controller)
    {
        RefreshControllers();
    }

    private void OnControllerDisconnected(object? sender, string devicePath)
    {
        RefreshControllers();
    }

    private void RefreshControllers()
    {
        string? currentSelection = SelectedController?.DevicePath;

        Controllers.Clear();

        foreach (DualSenseController controller in _dualSenseManager.Controllers.Values)
        {
            ControllerInfo? controllerInfo = GetControllerInfo(controller);
            ControllerViewModel vm = new ControllerViewModel(controller, controllerInfo);
            Controllers.Add(vm);

            // Subscribe to input changes for battery updates
            controller.InputChanged += (_, _) => vm.UpdateBatteryState(controller.Battery);
        }

        HasControllers = Controllers.Count > 0;

        // Restore selection or select first
        if (currentSelection != null)
        {
            SelectedController = Controllers.FirstOrDefault(c => c.DevicePath == currentSelection);
        }

        SelectedController ??= Controllers.FirstOrDefault();
    }

    private ControllerInfo? GetControllerInfo(DualSenseController controller)
    {
        ControllerSettings settings = _settingsManager.Application.Controllers;

        // Try to find by MAC
        if (!string.IsNullOrEmpty(controller.MacAddress))
        {
            return settings.KnownControllers.Values.FirstOrDefault(c => c.MacAddress == controller.MacAddress);
        }

        return null;
    }

    public void Dispose()
    {
        _dualSenseManager.ControllerConnected -= OnControllerConnected;
        _dualSenseManager.ControllerDisconnected -= OnControllerDisconnected;

        // Dispose all controller view models
        foreach (ControllerViewModel controller in Controllers)
        {
            controller.Dispose();
        }
    }
}