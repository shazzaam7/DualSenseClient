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
        Logger.Info("Initializing DualSense Manager");

        // Get the device list and subscribe to changes
        _deviceList = DeviceList.Local;
        _deviceList.Changed += OnDeviceListChanged;

        // Initial scan
        ScanForDevices();

        Logger.Debug("DualSense Manager started");
    }

    private void OnDeviceListChanged(object? sender, DeviceListChangedEventArgs e)
    {
        if (_disposed || !_scanningEnabled)
        {
            return;
        }

        Logger.Trace("Device list changed event received");

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
            Logger.Info("DualSense scanning enabled");
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
            Logger.Info("DualSense scanning disabled");
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
            Logger.Trace("Scanning for DualSense devices...");

            HidDevice[] devices = _deviceList!.GetHidDevices()
                .Where(d => d.VendorID == SonyVid)
                .Where(d => KnownPids.Contains(d.ProductID))
                .ToArray();

            Logger.Trace($"Found {devices.Length} DualSense device(s)");

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
            Logger.Error("Error scanning for devices");
            Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
        }
    }

    private void AddController(HidDevice device)
    {
        try
        {
            Logger.Debug($"Attempting to open device: {device.DevicePath}");

            if (!device.TryOpen(out HidStream? stream))
            {
                Logger.Warning($"Found device but couldn't open: {device.GetProductName()} ({device.DevicePath})");
                return;
            }

            DualSenseController controller = new DualSenseController(device, stream);

            lock (_lock)
            {
                _controllers[device.DevicePath] = controller;
            }

            Logger.Info($"Controller added: {device.GetProductName()} ({controller.ConnectionType}) - {device.DevicePath}");

            ControllerConnected?.Invoke(this, controller);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error adding controller: {device.GetProductName()}");
            Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
        }
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

        Logger.Info($"Controller removed: {deviceName} ({connectionType}) - {path}");
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

        Logger.Debug("Disposing DualSense Manager");
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
                    Logger.Warning($"Error disposing controller: {ex.Message}");
                }
            }

            _controllers.Clear();
        }

        Logger.Info("DualSense Manager disposed");
    }
}