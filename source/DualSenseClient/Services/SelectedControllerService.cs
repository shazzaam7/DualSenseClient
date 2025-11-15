using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.Settings;
using DualSenseClient.Core.Settings.Models;
using DualSenseClient.ViewModels;
using Avalonia.Threading;

namespace DualSenseClient.Services;

public partial class SelectedControllerService : ObservableObject, IDisposable
{
    // Properties
    private readonly DualSenseManager _dualSenseManager;
    private readonly ISettingsManager _settingsManager;
    private readonly Dictionary<string, ControllerViewModelBase> _controllerViewModels = new();
    private readonly Lock _lock = new Lock();
    private string? _lastSelectedMacAddress;
    private bool _isTransitioning;
    private CancellationTokenSource? _transitionCts;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;

    [ObservableProperty] private ObservableCollection<ControllerViewModelBase> _availableControllers = new();

    public event EventHandler<ControllerViewModelBase?>? SelectedControllerChanged;

    // Constructor
    public SelectedControllerService(DualSenseManager dualSenseManager, ISettingsManager settingsManager)
    {
        _dualSenseManager = dualSenseManager;
        _settingsManager = settingsManager;

        // Subscribe to controller events
        _dualSenseManager.ControllerConnected += OnControllerConnected;
        _dualSenseManager.ControllerDisconnected += OnControllerDisconnected;

        // Initialize with existing controllers
        InitializeControllers();
    }

    // Functions
    private void InitializeControllers()
    {
        lock (_lock)
        {
            foreach (DualSenseController controller in _dualSenseManager.Controllers.Values)
            {
                AddControllerViewModelInternal(controller);
            }

            UpdateAvailableControllers();

            // Auto-selecting first controller if selected is null
            if (SelectedController == null && _controllerViewModels.Count > 0)
            {
                SelectControllerInternal(_controllerViewModels.Values.First());
            }
        }
    }

    /// <summary>
    /// Updates the name of a controller and refreshes the UI
    /// </summary>
    public void UpdateControllerName(string controllerId, string newName)
    {
        lock (_lock)
        {
            // Find the controller ViewModel by ID
            ControllerViewModelBase? controllerVm = _controllerViewModels.Values.FirstOrDefault(vm => vm.ControllerId == controllerId);

            if (controllerVm != null)
            {
                // Update the ViewModel name directly
                controllerVm.UpdateName(newName);

                // If this is the selected controller, notify of changes
                if (SelectedController == controllerVm)
                {
                    // Force property change notification
                    OnPropertyChanged(nameof(SelectedController));
                }
            }

            // Update in settings
            if (_settingsManager.Application.Controllers.KnownControllers.TryGetValue(controllerId, out var controllerInfo))
            {
                controllerInfo.Name = newName;
                _settingsManager.SaveAll();
            }
        }
    }

    /// <summary>
    /// Gets a controller ViewModel by its ID
    /// </summary>
    public ControllerViewModelBase? GetControllerById(string controllerId)
    {
        lock (_lock)
        {
            return _controllerViewModels.Values.FirstOrDefault(vm => vm.ControllerId == controllerId);
        }
    }

    private async void OnControllerConnected(object? sender, DualSenseController controller)
    {
        ControllerViewModelBase vm;
        string? normalizedMac = null;
        bool shouldSelect = false;

        lock (_lock)
        {
            // Check if this is the same controller that was previously selected
            vm = AddControllerViewModelInternal(controller);
            if (!string.IsNullOrEmpty(controller.MacAddress))
            {
                normalizedMac = NormalizeMacAddress(controller.MacAddress);

                if (_lastSelectedMacAddress != null && normalizedMac == NormalizeMacAddress(_lastSelectedMacAddress))
                {
                    shouldSelect = true;
                    _isTransitioning = true;
                }
            }

            // If no controller is selected and not transitioning, auto-select this controller
            if (SelectedController == null && !_isTransitioning)
            {
                shouldSelect = true;
            }
        }

        // Update UI
        await Dispatcher.UIThread.InvokeAsync(UpdateAvailableControllers);

        if (shouldSelect)
        {
            // Small delay to ensure UI has updated
            await Task.Delay(50);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SelectController(vm);
                _isTransitioning = false;
            });
        }
    }

    private async void OnControllerDisconnected(object? sender, string devicePath)
    {
        ControllerViewModelBase? vm;
        bool wasSelected = false;
        string? macAddress = null;

        lock (_lock)
        {
            if (!_controllerViewModels.TryGetValue(devicePath, out vm))
            {
                return;
            }

            // Store MAC address
            if (SelectedController == vm)
            {
                wasSelected = true;
                if (!string.IsNullOrEmpty(vm.MacAddress))
                {
                    macAddress = vm.MacAddress;
                    _lastSelectedMacAddress = macAddress;
                }
            }
            _controllerViewModels.Remove(devicePath);
        }

        vm.Dispose();

        // Update UI
        await Dispatcher.UIThread.InvokeAsync(UpdateAvailableControllers);

        if (wasSelected)
        {
            // Clear the current selection on UI thread immediately
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                lock (_lock)
                {
                    SelectControllerInternal(null);
                }
            });

            // Start transition
            _transitionCts?.Cancel();
            _transitionCts = new CancellationTokenSource();
            CancellationToken token = _transitionCts.Token;

            // Wait a bit to see if the same controller reconnects with different connection type
            await Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(150, token); // Wait 150ms for reconnection

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        lock (_lock)
                        {
                            // Select first available if no controller is selected and we're not transitioning
                            if (SelectedController == null && !_isTransitioning && _controllerViewModels.Count > 0)
                            {
                                SelectControllerInternal(_controllerViewModels.Values.First());
                            }
                        }
                    });
                }
                catch (TaskCanceledException)
                {
                    // Expected when a new controller connects quickly
                }
            });
        }
    }

    private ControllerViewModelBase AddControllerViewModelInternal(DualSenseController controller)
    {
        ControllerInfo? controllerInfo = GetControllerInfo(controller);
        ControllerViewModelBase vm = new ControllerViewModelBase(controller, controllerInfo);

        // Battery updates
        controller.InputChanged += (_, _) =>
        {
            Dispatcher.UIThread.Post(() => vm.UpdateBatteryState(controller.Battery));
        };

        _controllerViewModels[controller.Device.DevicePath] = vm;
        return vm;
    }

    private void UpdateAvailableControllers()
    {
        AvailableControllers.Clear();

        lock (_lock)
        {
            foreach (ControllerViewModelBase controller in _controllerViewModels.Values)
            {
                AvailableControllers.Add(controller);
            }
        }
    }

    public void SelectController(ControllerViewModelBase? controller)
    {
        lock (_lock)
        {
            SelectControllerInternal(controller);
        }
    }

    private void SelectControllerInternal(ControllerViewModelBase? controller)
    {
        if (SelectedController != controller)
        {
            _transitionCts?.Cancel(); // Cancel any transitions

            SelectedController = controller;

            // Store MAC address for reconnection
            if (controller != null && !string.IsNullOrEmpty(controller.MacAddress))
            {
                _lastSelectedMacAddress = controller.MacAddress;
            }

            SelectedControllerChanged?.Invoke(this, controller);
        }
    }

    public void SelectControllerByPath(string devicePath)
    {
        lock (_lock)
        {
            if (_controllerViewModels.TryGetValue(devicePath, out var vm))
            {
                SelectControllerInternal(vm);
            }
        }
    }

    public void SelectControllerByMac(string macAddress)
    {
        lock (_lock)
        {
            string normalized = NormalizeMacAddress(macAddress);
            ControllerViewModelBase? controller = _controllerViewModels.Values.FirstOrDefault(c => !string.IsNullOrEmpty(c.MacAddress) && NormalizeMacAddress(c.MacAddress) == normalized);

            if (controller != null)
            {
                SelectControllerInternal(controller);
            }
        }
    }

    public IEnumerable<ControllerViewModelBase> GetAllControllers()
    {
        lock (_lock)
        {
            return _controllerViewModels.Values.ToList();
        }
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

    private string NormalizeMacAddress(string mac)
    {
        return mac.Replace(":", "").Replace("-", "").ToLowerInvariant();
    }

    public void Dispose()
    {
        _transitionCts?.Cancel();
        _transitionCts?.Dispose();

        _dualSenseManager.ControllerConnected -= OnControllerConnected;
        _dualSenseManager.ControllerDisconnected -= OnControllerDisconnected;

        foreach (var vm in _controllerViewModels.Values)
        {
            vm.Dispose();
        }

        _controllerViewModels.Clear();
        AvailableControllers.Clear();
    }
}