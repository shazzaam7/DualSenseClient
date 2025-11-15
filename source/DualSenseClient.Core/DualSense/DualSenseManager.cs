using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.Logging;
using HidSharp;

namespace DualSenseClient.Core.DualSense;

/// <summary>
/// Manages DualSense controller discovery and lifecycle
/// </summary>
public class DualSenseManager : IDisposable
{
    // Fields
    private static readonly int SonyVid = 0x054C;
    private static readonly int[] KnownPids = [0x0CE6]; // DualSense PID

    private readonly Dictionary<string, DualSenseController> _controllers = new Dictionary<string, DualSenseController>();
    private readonly Lock _lock = new Lock();
    private bool _disposed;
    private bool _scanningEnabled = true;
    private DeviceList? _deviceList;

    // Events
    public event EventHandler<DualSenseController>? ControllerConnected;
    public event EventHandler<string>? ControllerDisconnected;

    // Public Properties
    public IReadOnlyDictionary<string, DualSenseController> Controllers
    {
        get
        {
            lock (_lock)
            {
                return new Dictionary<string, DualSenseController>(_controllers);
            }
        }
    }

    // Constructor
    public DualSenseManager()
    {
        Logger.Info<DualSenseManager>("Initializing DualSense Manager");

        // Get the device list and subscribe to changes
        _deviceList = DeviceList.Local;
        _deviceList.Changed += OnDeviceListChanged;

        // Initial scan
        ScanForDevices();

        Logger.Debug<DualSenseManager>("DualSense Manager started");
    }

    private void OnDeviceListChanged(object? sender, DeviceListChangedEventArgs e)
    {
        if (_disposed || !_scanningEnabled)
        {
            return;
        }

        Logger.Trace<DualSenseManager>("Device list changed event received");

        // Debounce multiple rapid changes
        Task.Run(async () =>
        {
            await Task.Delay(100); // Small delay to batch rapid changes
            ScanForDevices();
        });
    }

    /// <summary>
    /// Enables scanning if it was previously stopped.
    /// </summary>
    public void StartScanning()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _scanningEnabled = true;
            Logger.Info<DualSenseManager>("DualSense scanning enabled");
        }

        // Perform immediate scan
        ScanForDevices();
    }

    /// <summary>
    /// Stops scanning for controllers (can be resumed later).
    /// </summary>
    public void StopScanning()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _scanningEnabled = false;
            Logger.Info<DualSenseManager>("DualSense scanning disabled");
        }
    }

    /// <summary>
    /// Manually triggers a device scan
    /// </summary>
    public void RefreshDevices()
    {
        if (!_disposed && _scanningEnabled)
        {
            ScanForDevices();
        }
    }

    private void ScanForDevices()
    {
        if (_disposed || !_scanningEnabled)
        {
            return;
        }

        try
        {
            Logger.Trace<DualSenseManager>("Scanning for DualSense devices...");

            HidDevice[] devices = _deviceList!.GetHidDevices()
                .Where(d => d.VendorID == SonyVid)
                .Where(d => KnownPids.Contains(d.ProductID))
                .ToArray();

            Logger.Trace<DualSenseManager>($"Found {devices.Length} DualSense device(s)");

            lock (_lock)
            {
                // Remove disconnected controllers
                HashSet<string> currentPaths = devices.Select(d => d.DevicePath).ToHashSet();
                List<string> toRemove = _controllers.Keys.Where(path => !currentPaths.Contains(path)).ToList();

                foreach (string path in toRemove)
                {
                    RemoveController(path);
                }

                // Add new controllers
                List<HidDevice> newDevices = devices
                    .Where(d => !_controllers.ContainsKey(d.DevicePath))
                    .ToList();

                foreach (HidDevice device in newDevices)
                {
                    AddController(device);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<DualSenseManager>("Error scanning for devices");
            Logger.LogExceptionDetails<DualSenseManager>(ex, includeEnvironmentInfo: false);
        }
    }

    private void AddController(HidDevice device)
    {
        try
        {
            Logger.Debug<DualSenseManager>($"Attempting to open device: {device.DevicePath}");

            if (!device.TryOpen(out HidStream? stream))
            {
                Logger.Warning<DualSenseManager>($"Found device but couldn't open: {device.GetProductName()} ({device.DevicePath})");
                return;
            }

            DualSenseController newController = new DualSenseController(device, stream);

            lock (_lock)
            {
                // Check for duplicate controllers
                DualSenseController? duplicateController = FindDuplicateController(newController);

                if (duplicateController != null)
                {
                    Logger.Info<DualSenseManager>($"Detected same controller connected via both {duplicateController.ConnectionType} and {newController.ConnectionType}");

                    switch (newController.ConnectionType)
                    {
                        // New connection is USB, existing is Bluetooth, disconnect Bluetooth
                        case ConnectionType.USB when duplicateController.ConnectionType == ConnectionType.Bluetooth:
                        {
                            Logger.Info<DualSenseManager>("Replacing Bluetooth connection with USB connection");

                            string oldPath = duplicateController.Device.DevicePath;

                            // Remove the Bluetooth controller from dictionary
                            _controllers.Remove(oldPath);

                            // Try to disconnect Bluetooth
                            try
                            {
                                duplicateController.DisconnectBluetooth();
                                Logger.Debug<DualSenseManager>("Bluetooth disconnection initiated");
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning<DualSenseManager>($"Failed to disconnect Bluetooth: {ex.Message}");
                            }

                            // Dispose the old controller
                            duplicateController.Dispose();

                            // Add the new USB controller
                            _controllers[device.DevicePath] = newController;

                            Logger.Info<DualSenseManager>($"Controller switched to USB: {device.GetProductName()} - {device.DevicePath}");

                            // Fire disconnected event for old path
                            ControllerDisconnected?.Invoke(this, oldPath);

                            // Fire connected event for new controller
                            ControllerConnected?.Invoke(this, newController);

                            return;
                        }
                        // If new connection is Bluetooth and existing is USB, ignore the new Bluetooth connection
                        // New connection is Bluetooth and existing is USB (Shouldn't happen, but we dispose the Bluetooth)
                        case ConnectionType.Bluetooth when duplicateController.ConnectionType == ConnectionType.USB:
                            Logger.Info<DualSenseManager>("USB connection already exists for this controller, ignoring Bluetooth connection");
                            newController.Dispose();
                            return;
                        // Both same type (shouldn't happen, but log it out, dispose the new one)
                        default:
                            Logger.Warning<DualSenseManager>($"Same controller detected twice with same connection type: {newController.ConnectionType}");
                            newController.Dispose();
                            return;
                    }
                }

                // No duplicates
                _controllers[device.DevicePath] = newController;
            }

            Logger.Info<DualSenseManager>($"Controller added: {device.GetProductName()} ({newController.ConnectionType}) - {device.DevicePath}");

            ControllerConnected?.Invoke(this, newController);
        }
        catch (Exception ex)
        {
            Logger.Error<DualSenseManager>($"Error adding controller: {device.GetProductName()}");
            Logger.LogExceptionDetails<DualSenseManager>(ex, includeEnvironmentInfo: false);
        }
    }

    /// <summary>
    /// Finds if the same physical controller is already connected with a different connection type
    /// </summary>
    private DualSenseController? FindDuplicateController(DualSenseController newController)
    {
        string? newMac = newController.MacAddress;
        foreach (DualSenseController existingController in _controllers.Values)
        {
            // Skip if the device path is the same (shouldn't happen)
            if (existingController.Device.DevicePath == newController.Device.DevicePath)
            {
                continue;
            }

            string? existingMac = existingController.MacAddress;

            // Match by MAC address
            if (!string.IsNullOrEmpty(newMac) && !string.IsNullOrEmpty(existingMac))
            {
                if (NormalizeMacAddress(newMac) == NormalizeMacAddress(existingMac))
                {
                    Logger.Debug<DualSenseManager>($"Found duplicate controller by MAC: {newMac}");
                    return existingController;
                }
            }
        }

        return null;
    }

    // TODO: Move this into utils to reduce code duplication
    private string NormalizeMacAddress(string mac)
    {
        return mac.Replace(":", "").Replace("-", "").ToLowerInvariant();
    }

    private void RemoveController(string path)
    {
        DualSenseController? controller;
        lock (_lock)
        {
            if (!_controllers.Remove(path, out controller))
            {
                return;
            }
        }

        string deviceName = controller.Device.GetProductName();
        ConnectionType connectionType = controller.ConnectionType;

        controller.Dispose();

        Logger.Info<DualSenseManager>($"Controller removed: {deviceName} ({connectionType}) - {path}");
        ControllerDisconnected?.Invoke(this, path);
    }

    public DualSenseController? GetFirstController()
    {
        lock (_lock)
        {
            return _controllers.Values.FirstOrDefault();
        }
    }

    public IEnumerable<DualSenseController> GetControllersByType(ConnectionType type)
    {
        lock (_lock)
        {
            return _controllers.Values.Where(c => c.ConnectionType == type).ToList();
        }
    }

    public DualSenseController? GetControllerByPath(string devicePath)
    {
        lock (_lock)
        {
            return _controllers.GetValueOrDefault(devicePath);
        }
    }

    public int ControllerCount
    {
        get
        {
            lock (_lock)
            {
                return _controllers.Count;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Logger.Debug<DualSenseManager>("Disposing DualSense Manager");
        _disposed = true;

        if (_deviceList != null)
        {
            _deviceList.Changed -= OnDeviceListChanged;
        }

        lock (_lock)
        {
            foreach (DualSenseController controller in _controllers.Values)
            {
                try
                {
                    controller.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Warning<DualSenseManager>($"Error disposing controller: {ex.Message}");
                }
            }

            _controllers.Clear();
        }

        Logger.Info<DualSenseManager>("DualSense Manager disposed");
    }
}