using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.Logging;
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
    private readonly Dictionary<string, ControllerViewModelBase> _controllerViewModels = new Dictionary<string, ControllerViewModelBase>();
    private readonly Lock _lock = new Lock();
    private string? _lastSelectedMacAddress;
    private bool _isTransitioning;
    private CancellationTokenSource? _transitionCts;

    [ObservableProperty] private ControllerViewModelBase? _selectedController;

    [ObservableProperty] private ObservableCollection<ControllerViewModelBase> _availableControllers = new ObservableCollection<ControllerViewModelBase>();

    public event EventHandler<ControllerViewModelBase?>? SelectedControllerChanged;

    // Constructor
    public SelectedControllerService(DualSenseManager dualSenseManager, ISettingsManager settingsManager)
    {
        Logger.Info<SelectedControllerService>("Initializing SelectedControllerService");

        _dualSenseManager = dualSenseManager;
        _settingsManager = settingsManager;

        // Subscribe to controller events
        Logger.Debug<SelectedControllerService>("Subscribing to DualSenseManager events");
        _dualSenseManager.ControllerConnected += OnControllerConnected;
        _dualSenseManager.ControllerDisconnected += OnControllerDisconnected;

        // Initialize with existing controllers
        InitializeControllers();

        Logger.Info<SelectedControllerService>("SelectedControllerService initialized successfully");
    }

    // Functions
    private void InitializeControllers()
    {
        Logger.Debug<SelectedControllerService>("Initializing existing controllers");

        lock (_lock)
        {
            int controllerCount = _dualSenseManager.Controllers.Count;
            Logger.Debug<SelectedControllerService>($"Found {controllerCount} existing controller(s)");

            foreach (DualSenseController controller in _dualSenseManager.Controllers.Values)
            {
                Logger.Trace<SelectedControllerService>($"Adding controller: {controller.Device.DevicePath}");
                AddControllerViewModelInternal(controller);
            }

            UpdateAvailableControllers();

            // Auto-selecting first controller if selected is null
            if (SelectedController == null && _controllerViewModels.Count > 0)
            {
                ControllerViewModelBase firstController = _controllerViewModels.Values.First();
                Logger.Info<SelectedControllerService>($"Auto-selecting first controller: {firstController.Name} ({firstController.DevicePath})");
                SelectControllerInternal(firstController);
            }
            else if (_controllerViewModels.Count == 0)
            {
                Logger.Debug<SelectedControllerService>("No controllers available for selection");
            }
        }
    }

    /// <summary>
    /// Updates the name of a controller and refreshes the UI
    /// </summary>
    public void UpdateControllerName(string controllerId, string newName)
    {
        Logger.Info<SelectedControllerService>($"Updating controller name - ID: {controllerId}, New Name: '{newName}'");

        lock (_lock)
        {
            // Find the controller ViewModel by ID
            ControllerViewModelBase? controllerVm = _controllerViewModels.Values.FirstOrDefault(vm => vm.ControllerId == controllerId);

            if (controllerVm != null)
            {
                string oldName = controllerVm.Name;
                Logger.Debug<SelectedControllerService>($"Found controller ViewModel, updating name from '{oldName}' to '{newName}'");

                // Update the ViewModel name directly
                controllerVm.UpdateName(newName);

                // If this is the selected controller, notify of changes
                if (SelectedController == controllerVm)
                {
                    Logger.Trace<SelectedControllerService>("Controller is currently selected, forcing property change notification");
                    // Force property change notification
                    OnPropertyChanged(nameof(SelectedController));
                }
            }
            else
            {
                Logger.Warning<SelectedControllerService>($"Controller ViewModel not found for ID: {controllerId}");
            }

            // Update in settings
            if (_settingsManager.Application.Controllers.KnownControllers.TryGetValue(controllerId, out ControllerInfo? controllerInfo))
            {
                Logger.Debug<SelectedControllerService>($"Updating controller name in settings: {controllerId}");
                controllerInfo.Name = newName;
                _settingsManager.SaveAll();
                Logger.Trace<SelectedControllerService>("Settings saved successfully");
            }
            else
            {
                Logger.Warning<SelectedControllerService>($"Controller not found in settings: {controllerId}");
            }
        }
    }

    /// <summary>
    /// Gets a controller ViewModel by its ID
    /// </summary>
    public ControllerViewModelBase? GetControllerById(string controllerId)
    {
        Logger.Trace<SelectedControllerService>($"Getting controller by ID: {controllerId}");

        lock (_lock)
        {
            ControllerViewModelBase? controller = _controllerViewModels.Values.FirstOrDefault(vm => vm.ControllerId == controllerId);
            Logger.Trace<SelectedControllerService>(controller != null ? $"Found controller: {controller.Name}" : $"Controller not found for ID: {controllerId}");
            return controller;
        }
    }

    private async void OnControllerConnected(object? sender, DualSenseController controller)
    {
        Logger.Info<SelectedControllerService>("Controller connected event received");
        Logger.Debug<SelectedControllerService>($"  Device: {controller.Device.GetProductName()}");
        Logger.Debug<SelectedControllerService>($"  Path: {controller.Device.DevicePath}");
        Logger.Debug<SelectedControllerService>($"  MAC: {controller.MacAddress ?? "N/A"}");
        Logger.Debug<SelectedControllerService>($"  Connection: {controller.ConnectionType}");

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
                Logger.Trace<SelectedControllerService>($"Normalized MAC: {normalizedMac}");

                if (_lastSelectedMacAddress != null)
                {
                    string normalizedLastMac = NormalizeMacAddress(_lastSelectedMacAddress);
                    Logger.Trace<SelectedControllerService>($"Last selected MAC: {normalizedLastMac}");

                    if (normalizedMac == normalizedLastMac)
                    {
                        Logger.Info<SelectedControllerService>("Previously selected controller reconnected, will re-select");
                        shouldSelect = true;
                        _isTransitioning = true;
                    }
                }
            }

            // If no controller is selected and not transitioning, auto-select this controller
            if (SelectedController == null && !_isTransitioning)
            {
                Logger.Info<SelectedControllerService>("No controller currently selected, will auto-select new controller");
                shouldSelect = true;
            }
        }

        // Update UI
        Logger.Trace<SelectedControllerService>("Updating available controllers on UI thread");
        await Dispatcher.UIThread.InvokeAsync(UpdateAvailableControllers);

        if (shouldSelect)
        {
            // Small delay to ensure UI has updated
            Logger.Trace<SelectedControllerService>("Waiting for UI update before selection");
            await Task.Delay(50);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Logger.Debug<SelectedControllerService>($"Selecting controller: {vm.Name}");
                SelectController(vm);
                _isTransitioning = false;
            });
        }
        else
        {
            Logger.Debug<SelectedControllerService>("Controller added but not selected");
        }
    }

    private async void OnControllerDisconnected(object? sender, string devicePath)
    {
        Logger.Info<SelectedControllerService>($"Controller disconnected event received - Path: {devicePath}");

        ControllerViewModelBase? vm;
        bool wasSelected = false;
        string? macAddress = null;

        lock (_lock)
        {
            if (!_controllerViewModels.TryGetValue(devicePath, out vm))
            {
                Logger.Warning<SelectedControllerService>($"Disconnected controller not found in ViewModels: {devicePath}");
                return;
            }

            Logger.Debug<SelectedControllerService>($"Removing controller: {vm.Name} ({vm.ConnectionType})");

            // Store MAC address
            if (SelectedController == vm)
            {
                wasSelected = true;
                Logger.Info<SelectedControllerService>($"Disconnected controller was selected: {vm.Name}");

                if (!string.IsNullOrEmpty(vm.MacAddress))
                {
                    macAddress = vm.MacAddress;
                    _lastSelectedMacAddress = macAddress;
                    Logger.Debug<SelectedControllerService>($"Stored MAC for reconnection: {macAddress}");
                }
            }

            _controllerViewModels.Remove(devicePath);
            Logger.Trace<SelectedControllerService>("Controller removed from ViewModels collection");
        }

        vm.Dispose();
        Logger.Trace<SelectedControllerService>("Controller ViewModel disposed");

        // Update UI
        Logger.Trace<SelectedControllerService>("Updating available controllers on UI thread");
        await Dispatcher.UIThread.InvokeAsync(UpdateAvailableControllers);

        if (wasSelected)
        {
            Logger.Debug<SelectedControllerService>("Handling selection change for disconnected controller");

            // Clear the current selection on UI thread immediately
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                lock (_lock)
                {
                    Logger.Trace<SelectedControllerService>("Clearing current selection");
                    SelectControllerInternal(null);
                }
            });

            // Start transition
            _transitionCts?.Cancel();
            _transitionCts = new CancellationTokenSource();
            CancellationToken token = _transitionCts.Token;

            Logger.Debug<SelectedControllerService>("Starting transition period for potential reconnection");

            // Wait a bit to see if the same controller reconnects with different connection type
            await Task.Run(async () =>
            {
                try
                {
                    Logger.Trace<SelectedControllerService>("Waiting 150ms for potential reconnection");
                    await Task.Delay(150, token); // Wait 150ms for reconnection

                    if (token.IsCancellationRequested)
                    {
                        Logger.Trace<SelectedControllerService>("Transition cancelled, controller likely reconnected");
                        return;
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        lock (_lock)
                        {
                            // Select first available if no controller is selected and we're not transitioning
                            if (SelectedController == null && !_isTransitioning && _controllerViewModels.Count > 0)
                            {
                                ControllerViewModelBase firstController = _controllerViewModels.Values.First();
                                Logger.Info<SelectedControllerService>($"Auto-selecting next available controller: {firstController.Name}");
                                SelectControllerInternal(firstController);
                            }
                            else if (_controllerViewModels.Count == 0)
                            {
                                Logger.Debug<SelectedControllerService>("No controllers available for selection");
                            }
                        }
                    });
                }
                catch (TaskCanceledException)
                {
                    Logger.Trace<SelectedControllerService>("Transition task cancelled (expected when controller reconnects quickly)");
                }
                catch (Exception ex)
                {
                    Logger.Error<SelectedControllerService>("Unexpected error during controller transition");
                    Logger.LogExceptionDetails<SelectedControllerService>(ex, includeEnvironmentInfo: false);
                }
            });
        }
        else
        {
            Logger.Debug<SelectedControllerService>("Disconnected controller was not selected, no selection change needed");
        }
    }

    private ControllerViewModelBase AddControllerViewModelInternal(DualSenseController controller)
    {
        Logger.Trace<SelectedControllerService>($"Creating ViewModel for controller: {controller.Device.DevicePath}");

        ControllerInfo? controllerInfo = GetControllerInfo(controller);
        Logger.Debug<SelectedControllerService>(controllerInfo != null ? $"Found existing controller info: {controllerInfo.Name} (ID: {controllerInfo.Id})" : "No existing controller info found, will create new");

        ControllerViewModelBase vm = new ControllerViewModelBase(controller, controllerInfo);

        // Battery updates
        controller.InputChanged += (_, _) =>
        {
            Dispatcher.UIThread.Post(() => vm.UpdateBatteryState(controller.Battery));
        };

        _controllerViewModels[controller.Device.DevicePath] = vm;

        Logger.Trace<SelectedControllerService>($"ViewModel created and added to collection: {vm.Name}");
        return vm;
    }

    private void UpdateAvailableControllers()
    {
        Logger.Trace<SelectedControllerService>("Updating available controllers collection");

        AvailableControllers.Clear();

        lock (_lock)
        {
            int count = _controllerViewModels.Count;
            Logger.Trace<SelectedControllerService>($"Adding {count} controller(s) to available collection");

            foreach (ControllerViewModelBase controller in _controllerViewModels.Values)
            {
                AvailableControllers.Add(controller);
                Logger.Trace<SelectedControllerService>($"  - {controller.Name} ({controller.ConnectionType})");
            }
        }

        Logger.Debug<SelectedControllerService>($"Available controllers updated: {AvailableControllers.Count} controller(s)");
    }

    public void SelectController(ControllerViewModelBase? controller)
    {
        Logger.Info<SelectedControllerService>(controller != null ? $"SelectController called: {controller.Name} ({controller.DevicePath})" : "SelectController called with null (clearing selection)");

        lock (_lock)
        {
            SelectControllerInternal(controller);
        }
    }

    private void SelectControllerInternal(ControllerViewModelBase? controller)
    {
        if (SelectedController != controller)
        {
            string previousName = SelectedController?.Name ?? "None";
            string newName = controller?.Name ?? "None";

            Logger.Debug<SelectedControllerService>($"Changing selected controller from '{previousName}' to '{newName}'");

            _transitionCts?.Cancel(); // Cancel any transitions
            Logger.Trace<SelectedControllerService>("Cancelled any pending transitions");

            SelectedController = controller;

            // Store MAC address for reconnection
            if (controller != null && !string.IsNullOrEmpty(controller.MacAddress))
            {
                _lastSelectedMacAddress = controller.MacAddress;
                Logger.Trace<SelectedControllerService>($"Stored MAC for future reconnection: {_lastSelectedMacAddress}");
            }

            Logger.Debug<SelectedControllerService>("Firing SelectedControllerChanged event");
            SelectedControllerChanged?.Invoke(this, controller);
        }
        else
        {
            Logger.Trace<SelectedControllerService>("SelectControllerInternal called but controller already selected");
        }
    }

    public void SelectControllerByPath(string devicePath)
    {
        Logger.Debug<SelectedControllerService>($"Selecting controller by path: {devicePath}");

        lock (_lock)
        {
            if (_controllerViewModels.TryGetValue(devicePath, out ControllerViewModelBase? vm))
            {
                Logger.Info<SelectedControllerService>($"Found controller for path: {vm.Name}");
                SelectControllerInternal(vm);
            }
            else
            {
                Logger.Warning<SelectedControllerService>($"No controller found for path: {devicePath}");
            }
        }
    }

    public void SelectControllerByMac(string macAddress)
    {
        Logger.Debug<SelectedControllerService>($"Selecting controller by MAC: {macAddress}");

        lock (_lock)
        {
            string normalized = NormalizeMacAddress(macAddress);
            Logger.Trace<SelectedControllerService>($"Normalized MAC: {normalized}");

            ControllerViewModelBase? controller = _controllerViewModels.Values.FirstOrDefault(c => !string.IsNullOrEmpty(c.MacAddress) && NormalizeMacAddress(c.MacAddress) == normalized);

            if (controller != null)
            {
                Logger.Info<SelectedControllerService>($"Found controller for MAC: {controller.Name}");
                SelectControllerInternal(controller);
            }
            else
            {
                Logger.Warning<SelectedControllerService>($"No controller found for MAC: {macAddress}");
            }
        }
    }

    public IEnumerable<ControllerViewModelBase> GetAllControllers()
    {
        Logger.Trace<SelectedControllerService>("GetAllControllers called");

        lock (_lock)
        {
            List<ControllerViewModelBase> controllers = _controllerViewModels.Values.ToList();
            Logger.Trace<SelectedControllerService>($"Returning {controllers.Count} controller(s)");
            return controllers;
        }
    }

    private ControllerInfo? GetControllerInfo(DualSenseController controller)
    {
        Logger.Trace<SelectedControllerService>($"Getting controller info for MAC: {controller.MacAddress ?? "N/A"}");

        ControllerSettings settings = _settingsManager.Application.Controllers;

        // Try to find by MAC
        if (!string.IsNullOrEmpty(controller.MacAddress))
        {
            ControllerInfo? info = settings.KnownControllers.Values.FirstOrDefault(c => c.MacAddress == controller.MacAddress);
            Logger.Trace<SelectedControllerService>(info != null ? $"Found controller info: {info.Name} (ID: {info.Id})" : "No matching controller info found");
            return info;
        }

        Logger.Trace<SelectedControllerService>("Controller has no MAC address, cannot lookup info");
        return null;
    }

    private string NormalizeMacAddress(string mac)
    {
        string normalized = mac.Replace(":", "").Replace("-", "").ToLowerInvariant();
        Logger.Trace<SelectedControllerService>($"MAC normalized: {mac} -> {normalized}");
        return normalized;
    }

    public void Dispose()
    {
        Logger.Info<SelectedControllerService>("Disposing SelectedControllerService");

        _transitionCts?.Cancel();
        _transitionCts?.Dispose();
        Logger.Trace<SelectedControllerService>("Transition CancellationTokenSource disposed");

        _dualSenseManager.ControllerConnected -= OnControllerConnected;
        _dualSenseManager.ControllerDisconnected -= OnControllerDisconnected;
        Logger.Debug<SelectedControllerService>("Unsubscribed from DualSenseManager events");

        int controllerCount = _controllerViewModels.Count;
        Logger.Debug<SelectedControllerService>($"Disposing {controllerCount} controller ViewModel(s)");

        foreach (ControllerViewModelBase vm in _controllerViewModels.Values)
        {
            try
            {
                Logger.Trace<SelectedControllerService>($"Disposing ViewModel: {vm.Name}");
                vm.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warning<SelectedControllerService>($"Error disposing controller ViewModel: {vm.Name}");
                Logger.LogExceptionDetails<SelectedControllerService>(ex, includeEnvironmentInfo: false);
            }
        }

        _controllerViewModels.Clear();
        AvailableControllers.Clear();

        Logger.Info<SelectedControllerService>("SelectedControllerService disposed successfully");
    }
}