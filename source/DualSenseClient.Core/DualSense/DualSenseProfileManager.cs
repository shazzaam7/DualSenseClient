using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings;
using DualSenseClient.Core.Settings.Models;
using HidSharp;

namespace DualSenseClient.Core.DualSense;

public class DualSenseProfileManager
{
    // Properties
    private readonly ISettingsManager _settingsManager;
    private readonly DualSenseManager _dualSenseManager;

    // Constructor
    public DualSenseProfileManager(ISettingsManager settingsManager, DualSenseManager dualSenseManager)
    {
        _settingsManager = settingsManager;
        _dualSenseManager = dualSenseManager;

        // Subscribe to controller events
        _dualSenseManager.ControllerConnected += OnControllerConnected;
        _dualSenseManager.ControllerDisconnected += OnControllerDisconnected;

        // Apply profiles to already connected controllers
        InitializeExistingControllers();
    }

    // Functions
    private void OnControllerConnected(object? sender, DualSenseController controller)
    {
        Logger.Info($"Controller connected, applying profile");

        // Get or create controller info
        ControllerInfo controllerInfo = GetOrCreateControllerInfo(controller);

        // Update last seen
        controllerInfo.LastSeen = DateTime.UtcNow;
        controllerInfo.LastConnectionType = controller.ConnectionType;

        // Apply profile
        ApplyProfile(controller, controllerInfo);

        // Save settings
        _settingsManager.SaveAll();
    }

    private void OnControllerDisconnected(object? sender, string devicePath)
    {
        Logger.Info($"Controller disconnected: {devicePath}");
        // Settings are already saved when connected
        // TODO: When adding changes, do a save here
    }

    /// <summary>
    /// Applies profiles to controllers that are already connected at startup
    /// </summary>
    private void InitializeExistingControllers()
    {
        Logger.Info("Checking for already connected controllers...");

        List<DualSenseController> connectedControllers = _dualSenseManager.Controllers.Values.ToList();

        if (connectedControllers.Count == 0)
        {
            Logger.Debug("No controllers currently connected");
            return;
        }

        Logger.Info($"Found {connectedControllers.Count} connected controller(s), applying profiles");

        bool settingsChanged = false;

        foreach (DualSenseController controller in connectedControllers)
        {
            try
            {
                Logger.Debug($"Processing existing controller: {controller.Device.GetProductName()}");

                // Get or create controller info
                ControllerInfo controllerInfo = GetOrCreateControllerInfo(controller);

                // Update last seen
                controllerInfo.LastSeen = DateTime.UtcNow;
                controllerInfo.LastConnectionType = controller.ConnectionType;

                // Apply profile
                ApplyProfile(controller, controllerInfo);

                settingsChanged = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to initialize existing controller: {controller.Device.GetProductName()}");
                Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
            }
        }

        // Save settings if any controller was processed
        if (settingsChanged)
        {
            _settingsManager.SaveAll();
        }
    }

    private ControllerInfo GetOrCreateControllerInfo(DualSenseController controller)
    {
        ControllerSettings settings = _settingsManager.Application.Controllers;

        // Try to find by MAC address first (most reliable for Bluetooth)
        if (!string.IsNullOrEmpty(controller.MacAddress))
        {
            ControllerInfo? existing = settings.KnownControllers.Values.FirstOrDefault(c => c.MacAddress == controller.MacAddress);

            if (existing != null)
            {
                Logger.Debug($"Found existing controller by MAC: {controller.MacAddress}");
                return existing;
            }
        }

        // Create new controller info
        string controllerId = GenerateControllerId(controller);
        ControllerInfo controllerInfo = new ControllerInfo
        {
            Id = controllerId,
            Name = $"DualSense {settings.KnownControllers.Count + 1}",
            MacAddress = controller.MacAddress,
            SerialNumber = TryGetSerialNumber(controller.Device),
            ProfileId = settings.DefaultProfileId,
            LastSeen = DateTime.UtcNow,
            LastConnectionType = controller.ConnectionType
        };

        settings.KnownControllers[controllerId] = controllerInfo;
        Logger.Info($"Added new controller: {controllerInfo.Name} (ID: {controllerId})");

        // Create default profile if none exists
        if (settings.Profiles.Count == 0)
        {
            CreateDefaultProfile();
        }

        return controllerInfo;
    }

    private string GenerateControllerId(DualSenseController controller)
    {
        if (!string.IsNullOrEmpty(controller.MacAddress))
        {
            return $"{controller.MacAddress.Replace(":", "")}";
        }
        throw new Exception("Couldn't find MAC address of the controller");
    }

    private string? TryGetSerialNumber(HidDevice device)
    {
        try
        {
            return device.GetSerialNumber();
        }
        catch (Exception ex)
        {
            Logger.Debug($"Could not get serial number: {ex.Message}");
            return null;
        }
    }

    private void ApplyProfile(DualSenseController controller, ControllerInfo controllerInfo)
    {
        ControllerSettings settings = _settingsManager.Application.Controllers;

        // Get profile ID (use default if controller doesn't have one)
        string? profileId = controllerInfo.ProfileId ?? settings.DefaultProfileId;

        if (string.IsNullOrEmpty(profileId) || !settings.Profiles.TryGetValue(profileId, out var profile))
        {
            Logger.Warning($"No profile found for controller {controllerInfo.Name}");
            return;
        }

        Logger.Info($"Applying profile '{profile.Name}' to {controllerInfo.Name}");

        // Apply lightbar settings
        controller.SetLightbar(profile.Lightbar.Red, profile.Lightbar.Green, profile.Lightbar.Blue);

        // Apply player LEDs
        controller.SetPlayerLeds(profile.PlayerLeds.Pattern, profile.PlayerLeds.Brightness);

        // Apply mic LED
        controller.SetMicLed(profile.MicLed);
    }

    /// <summary>
    /// Manually refreshes profiles for all connected controllers
    /// </summary>
    public void RefreshAllProfiles()
    {
        Logger.Info("Refreshing profiles for all connected controllers");

        foreach (DualSenseController controller in _dualSenseManager.Controllers.Values)
        {
            ControllerInfo controllerInfo = GetOrCreateControllerInfo(controller);
            ApplyProfile(controller, controllerInfo);
        }
    }

    public void CreateDefaultProfile()
    {
        ControllerSettings settings = _settingsManager.Application.Controllers;

        ControllerProfile defaultProfile = new ControllerProfile
        {
            Id = "default",
            Name = "Default",
            Lightbar = new LightbarSettings { Red = 0, Green = 0, Blue = 0 },
            PlayerLeds = new PlayerLedSettings { Pattern = PlayerLed.None },
            MicLed = MicLed.Off
        };

        settings.Profiles["default"] = defaultProfile;
        settings.DefaultProfileId = "default";

        Logger.Info("Created default controller profile");
    }

    public void AssignProfileToController(string controllerId, string profileId)
    {
        ControllerSettings settings = _settingsManager.Application.Controllers;

        if (!settings.KnownControllers.TryGetValue(controllerId, out var controllerInfo))
        {
            Logger.Warning($"Controller not found: {controllerId}");
            return;
        }

        if (!settings.Profiles.ContainsKey(profileId))
        {
            Logger.Warning($"Profile not found: {profileId}");
            return;
        }

        controllerInfo.ProfileId = profileId;

        // Apply immediately if controller is connected
        DualSenseController? connectedController = FindConnectedController(controllerInfo);
        if (connectedController != null)
        {
            ApplyProfile(connectedController, controllerInfo);
        }

        _settingsManager.SaveAll();
        Logger.Info($"Assigned profile '{profileId}' to controller '{controllerInfo.Name}'");
    }

    private DualSenseController? FindConnectedController(ControllerInfo info)
    {
        // Try to find by MAC address
        if (!string.IsNullOrEmpty(info.MacAddress))
        {
            return _dualSenseManager.Controllers.Values.FirstOrDefault(c => c.MacAddress == info.MacAddress);
        }

        // Try to find by serial number
        if (!string.IsNullOrEmpty(info.SerialNumber))
        {
            return _dualSenseManager.Controllers.Values.FirstOrDefault(c => TryGetSerialNumber(c.Device) == info.SerialNumber);
        }

        return null;
    }

    public void Dispose()
    {
        _dualSenseManager.ControllerConnected -= OnControllerConnected;
        _dualSenseManager.ControllerDisconnected -= OnControllerDisconnected;
    }
}