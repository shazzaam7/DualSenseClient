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
        Logger.Info("Initializing DualSenseProfileManager");

        _settingsManager = settingsManager;
        _dualSenseManager = dualSenseManager;

        // Subscribe to controller events
        Logger.Debug("Subscribing to controller events");
        _dualSenseManager.ControllerConnected += OnControllerConnected;
        _dualSenseManager.ControllerDisconnected += OnControllerDisconnected;

        // Apply profiles to already connected controllers
        InitializeExistingControllers();

        Logger.Info("DualSenseProfileManager initialized successfully");
    }

    // Functions
    private void OnControllerConnected(object? sender, DualSenseController controller)
    {
        Logger.Info($"Controller connected event: {controller.Device.GetProductName()}");
        Logger.Debug($"  Connection type: {controller.ConnectionType}");
        Logger.Debug($"  MAC Address: {controller.MacAddress ?? "Unknown"}");

        try
        {
            // Get or create controller info
            ControllerInfo controllerInfo = GetOrCreateControllerInfo(controller);

            // Update last seen
            controllerInfo.LastSeen = DateTime.UtcNow;
            controllerInfo.LastConnectionType = controller.ConnectionType;
            Logger.Debug($"  Updated last seen: {controllerInfo.LastSeen:O}");

            // Apply profile
            ApplyProfile(controller, controllerInfo);

            // Save settings
            Logger.Debug("Saving settings after controller connection");
            _settingsManager.SaveAll();

            Logger.Info($"Controller '{controllerInfo.Name}' connected and configured successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to handle controller connection");
            Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
        }
    }

    private void OnControllerDisconnected(object? sender, string devicePath)
    {
        Logger.Info($"Controller disconnected: {devicePath}");
        Logger.Debug("Settings are already saved, no action needed");
    }

    /// <summary>
    /// Applies profiles to controllers that are already connected at startup
    /// </summary>
    private void InitializeExistingControllers()
    {
        Logger.Info("Checking for already connected controllers at startup");

        List<DualSenseController> connectedControllers = _dualSenseManager.Controllers.Values.ToList();

        if (connectedControllers.Count == 0)
        {
            Logger.Debug("No controllers currently connected");
            return;
        }

        Logger.Info($"Found {connectedControllers.Count} connected controller(s), applying profiles");

        bool settingsChanged = false;
        int successCount = 0;
        int failureCount = 0;

        foreach (DualSenseController controller in connectedControllers)
        {
            try
            {
                string productName = controller.Device.GetProductName();
                Logger.Debug($"Processing existing controller: {productName}");
                Logger.Trace($"  Device path: {controller.Device.DevicePath}");
                Logger.Trace($"  Connection type: {controller.ConnectionType}");

                // Get or create controller info
                ControllerInfo controllerInfo = GetOrCreateControllerInfo(controller);

                // Update last seen
                controllerInfo.LastSeen = DateTime.UtcNow;
                controllerInfo.LastConnectionType = controller.ConnectionType;

                // Apply profile
                ApplyProfile(controller, controllerInfo);

                settingsChanged = true;
                successCount++;
                Logger.Debug($"Successfully initialized controller '{controllerInfo.Name}'");
            }
            catch (Exception ex)
            {
                failureCount++;
                Logger.Error($"Failed to initialize existing controller");
                Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
            }
        }

        // Save settings if any controller was processed
        if (settingsChanged)
        {
            Logger.Debug("Saving settings after initializing existing controllers");
            _settingsManager.SaveAll();
        }

        Logger.Info($"Existing controllers initialization complete: {successCount} succeeded, {failureCount} failed");
    }

    public ControllerInfo GetOrCreateControllerInfo(DualSenseController controller)
    {
        Logger.Debug($"Getting or creating controller info");
        ControllerSettings settings = _settingsManager.Application.Controllers;

        // Try to find by MAC address first (most reliable for Bluetooth)
        if (!string.IsNullOrEmpty(controller.MacAddress))
        {
            Logger.Trace($"Searching by MAC address: {controller.MacAddress}");
            ControllerInfo? existing = settings.KnownControllers.Values.FirstOrDefault(c => c.MacAddress == controller.MacAddress);

            if (existing != null)
            {
                Logger.Debug($"Found existing controller by MAC: {existing.Name} (ID: {existing.Id})");
                return existing;
            }
        }
        else
        {
            Logger.Warning("Controller has no MAC address, cannot search by MAC");
        }

        // Create new controller info
        Logger.Info("Creating new controller entry");
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

        Logger.Debug($"  Controller ID: {controllerId}");
        Logger.Debug($"  Name: {controllerInfo.Name}");
        Logger.Debug($"  MAC: {controllerInfo.MacAddress ?? "None"}");
        Logger.Debug($"  Serial: {controllerInfo.SerialNumber ?? "None"}");
        Logger.Debug($"  Profile ID: {controllerInfo.ProfileId ?? "None"}");

        settings.KnownControllers[controllerId] = controllerInfo;
        Logger.Info($"Added new controller: {controllerInfo.Name} (ID: {controllerId})");

        // Create default profile if none exists
        if (settings.Profiles.Count == 0)
        {
            Logger.Warning("No profiles exist, creating default profile");
            CreateDefaultProfile();
        }

        return controllerInfo;
    }

    private string GenerateControllerId(DualSenseController controller)
    {
        if (!string.IsNullOrEmpty(controller.MacAddress))
        {
            string id = controller.MacAddress.Replace(":", "");
            Logger.Trace($"Generated controller ID from MAC: {id}");
            return id;
        }

        Logger.Error("Cannot generate controller ID: MAC address is missing");
        throw new Exception("Couldn't find MAC address of the controller");
    }

    private string? TryGetSerialNumber(HidDevice device)
    {
        try
        {
            string? serial = device.GetSerialNumber();
            Logger.Trace($"Retrieved serial number: {serial ?? "None"}");
            return serial;
        }
        catch (Exception ex)
        {
            Logger.Debug($"Could not get serial number: {ex.Message}");
            return null;
        }
    }

    private void ApplyProfile(DualSenseController controller, ControllerInfo controllerInfo)
    {
        Logger.Debug($"Applying profile to controller '{controllerInfo.Name}'");
        ControllerSettings settings = _settingsManager.Application.Controllers;

        // Get profile ID (use default if controller doesn't have one)
        string? profileId = controllerInfo.ProfileId ?? settings.DefaultProfileId;
        Logger.Trace($"Profile ID to apply: {profileId ?? "None"}");

        if (string.IsNullOrEmpty(profileId) || !settings.Profiles.TryGetValue(profileId, out var profile))
        {
            Logger.Warning($"No valid profile found for controller {controllerInfo.Name}");
            return;
        }

        Logger.Info($"Applying profile '{profile.Name}' to '{controllerInfo.Name}'");
        Logger.Debug($"  Lightbar: RGB({profile.Lightbar.Red}, {profile.Lightbar.Green}, {profile.Lightbar.Blue})");
        Logger.Debug($"  Player LEDs: {profile.PlayerLeds.Pattern} @ {profile.PlayerLeds.Brightness}");
        Logger.Debug($"  Mic LED: {profile.MicLed}");

        try
        {
            // Apply lightbar settings
            controller.SetLightbar(profile.Lightbar.Red, profile.Lightbar.Green, profile.Lightbar.Blue);
            Logger.Trace("Lightbar applied");

            // Apply player LEDs
            controller.SetPlayerLeds(profile.PlayerLeds.Pattern, profile.PlayerLeds.Brightness);
            Logger.Trace("Player LEDs applied");

            // Apply mic LED
            controller.SetMicLed(profile.MicLed);
            Logger.Trace("Mic LED applied");

            Logger.Info("Profile applied successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to apply profile settings");
            Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
        }
    }

    /// <summary>
    /// Manually refreshes profiles for all connected controllers
    /// </summary>
    public void RefreshAllProfiles()
    {
        Logger.Info("Refreshing profiles for all connected controllers");
        int count = _dualSenseManager.Controllers.Count;
        Logger.Debug($"Connected controllers: {count}");

        foreach (DualSenseController controller in _dualSenseManager.Controllers.Values)
        {
            try
            {
                ControllerInfo controllerInfo = GetOrCreateControllerInfo(controller);
                ApplyProfile(controller, controllerInfo);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to refresh profile for controller");
                Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
            }
        }

        Logger.Info("Profile refresh complete");
    }

    public void CreateDefaultProfile()
    {
        Logger.Info("Creating default controller profile");
        ControllerSettings settings = _settingsManager.Application.Controllers;

        ControllerProfile defaultProfile = new ControllerProfile
        {
            Id = "default",
            Name = "Default",
            Lightbar = new LightbarSettings { Red = 0, Green = 0, Blue = 0 },
            PlayerLeds = new PlayerLedSettings { Pattern = PlayerLed.None },
            MicLed = MicLed.Off
        };

        Logger.Debug("Default profile settings: Lightbar=Off, PlayerLEDs=None, MicLED=Off");

        settings.Profiles["default"] = defaultProfile;
        settings.DefaultProfileId = "default";

        Logger.Info("Default controller profile created and set");
    }

    public void AssignProfileToController(string controllerId, string profileId)
    {
        Logger.Info($"Assigning profile '{profileId}' to controller '{controllerId}'");
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

        Logger.Debug($"Updating controller '{controllerInfo.Name}' profile assignment");
        controllerInfo.ProfileId = profileId;

        // Apply immediately if controller is connected
        DualSenseController? connectedController = FindConnectedController(controllerInfo);
        if (connectedController != null)
        {
            Logger.Debug("Controller is connected, applying profile immediately");
            ApplyProfile(connectedController, controllerInfo);
        }
        else
        {
            Logger.Debug("Controller is not currently connected, profile will be applied on next connection");
        }

        _settingsManager.SaveAll();
        Logger.Info($"Profile assignment saved successfully");
    }

    private DualSenseController? FindConnectedController(ControllerInfo info)
    {
        Logger.Trace($"Searching for connected controller: {info.Name}");

        // Try to find by MAC address
        if (!string.IsNullOrEmpty(info.MacAddress))
        {
            DualSenseController? controller = _dualSenseManager.Controllers.Values.FirstOrDefault(c => c.MacAddress == info.MacAddress);
            if (controller != null)
            {
                Logger.Trace($"Found by MAC address");
                return controller;
            }
        }

        // Try to find by serial number
        if (!string.IsNullOrEmpty(info.SerialNumber))
        {
            DualSenseController? controller = _dualSenseManager.Controllers.Values.FirstOrDefault(c => TryGetSerialNumber(c.Device) == info.SerialNumber);
            if (controller != null)
            {
                Logger.Trace($"Found by serial number");
                return controller;
            }
        }

        Logger.Trace("Controller not found in connected devices");
        return null;
    }

    /// <summary>
    /// Gets all available profiles
    /// </summary>
    public Dictionary<string, ControllerProfile> GetAllProfiles()
    {
        int count = _settingsManager.Application.Controllers.Profiles.Count;
        Logger.Trace($"GetAllProfiles: returning {count} profile(s)");
        return _settingsManager.Application.Controllers.Profiles;
    }

    /// <summary>
    /// Gets a profile by ID
    /// </summary>
    public ControllerProfile? GetProfile(string profileId)
    {
        Logger.Trace($"GetProfile: {profileId}");
        bool found = _settingsManager.Application.Controllers.Profiles.TryGetValue(profileId, out ControllerProfile? profile);
        Logger.Trace($"Profile {(found ? "found" : "not found")}");
        return profile;
    }

    /// <summary>
    /// Gets the default profile
    /// </summary>
    public ControllerProfile GetDefaultProfile()
    {
        Logger.Trace("GetDefaultProfile called");
        string? defaultId = _settingsManager.Application.Controllers.DefaultProfileId;

        if (defaultId != null && _settingsManager.Application.Controllers.Profiles.TryGetValue(defaultId, out ControllerProfile? profile))
        {
            Logger.Debug($"Returning default profile: {profile.Name} (ID: {defaultId})");
            return profile;
        }

        // Create default if it doesn't exist
        Logger.Warning("Default profile doesn't exist, creating it");
        CreateDefaultProfile();

        defaultId = _settingsManager.Application.Controllers.DefaultProfileId;
        return _settingsManager.Application.Controllers.Profiles[defaultId!];
    }

    /// <summary>
    /// Gets the profile assigned to a specific controller
    /// </summary>
    public ControllerProfile GetControllerProfile(string controllerId)
    {
        Logger.Trace($"GetControllerProfile: {controllerId}");

        if (_settingsManager.Application.Controllers.KnownControllers.TryGetValue(controllerId, out ControllerInfo? controllerInfo) &&
            controllerInfo.ProfileId != null &&
            _settingsManager.Application.Controllers.Profiles.TryGetValue(controllerInfo.ProfileId, out ControllerProfile? profile))
        {
            Logger.Debug($"Controller '{controllerInfo.Name}' assigned profile: {profile.Name}");
            return profile;
        }

        Logger.Debug($"No specific profile assigned to controller {controllerId}, returning default");
        return GetDefaultProfile();
    }

    /// <summary>
    /// Creates a new profile
    /// </summary>
    public ControllerProfile CreateProfile(string name)
    {
        Logger.Info($"Creating new profile: {name}");

        ControllerProfile profile = new ControllerProfile
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Lightbar = new LightbarSettings
            {
                Behavior = LightbarBehavior.Custom,
                Red = 0,
                Green = 0,
                Blue = 255
            },
            PlayerLeds = new PlayerLedSettings
            {
                Pattern = PlayerLed.None,
                Brightness = PlayerLedBrightness.High
            },
            MicLed = MicLed.Off
        };

        Logger.Debug($"Profile ID: {profile.Id}");
        Logger.Debug($"Default settings: Lightbar=Blue, PlayerLEDs=None, MicLED=Off");

        SaveProfile(profile);
        Logger.Info($"Profile '{name}' created successfully");

        return profile;
    }

    /// <summary>
    /// Saves a profile
    /// </summary>
    public void SaveProfile(ControllerProfile profile)
    {
        Logger.Debug($"Saving profile: {profile.Name} (ID: {profile.Id})");
        _settingsManager.Application.Controllers.Profiles[profile.Id] = profile;
        _settingsManager.SaveAll();
        Logger.Debug("Profile saved successfully");
    }

    /// <summary>
    /// Deletes a profile
    /// </summary>
    public bool DeleteProfile(string profileId)
    {
        Logger.Info($"Attempting to delete profile: {profileId}");

        if (_settingsManager.Application.Controllers.DefaultProfileId == profileId)
        {
            Logger.Warning($"Cannot delete default profile: {profileId}");
            return false;
        }

        // Get profile name for logging
        string? profileName = GetProfile(profileId)?.Name;

        // Remove profile assignment from controllers and assign default
        string? defaultProfileId = _settingsManager.Application.Controllers.DefaultProfileId;
        Logger.Debug($"Reassigning controllers from deleted profile to default: {defaultProfileId}");

        int reassignedCount = 0;
        foreach (ControllerInfo controller in _settingsManager.Application.Controllers.KnownControllers.Values)
        {
            if (controller.ProfileId == profileId)
            {
                Logger.Trace($"Reassigning controller '{controller.Name}' to default profile");
                controller.ProfileId = defaultProfileId;
                reassignedCount++;
            }
        }

        if (reassignedCount > 0)
        {
            Logger.Debug($"Reassigned {reassignedCount} controller(s) to default profile");
        }

        bool removed = _settingsManager.Application.Controllers.Profiles.Remove(profileId);
        if (removed)
        {
            _settingsManager.SaveAll();
            Logger.Info($"Profile '{profileName}' deleted successfully");
        }
        else
        {
            Logger.Warning($"Failed to remove profile '{profileName}' from profiles dictionary");
        }

        return removed;
    }

    /// <summary>
    /// Sets the default profile
    /// </summary>
    public void SetDefaultProfile(string profileId)
    {
        Logger.Info($"Setting default profile: {profileId}");

        if (_settingsManager.Application.Controllers.Profiles.ContainsKey(profileId))
        {
            string? profileName = GetProfile(profileId)?.Name;
            _settingsManager.Application.Controllers.DefaultProfileId = profileId;
            _settingsManager.SaveAll();
            Logger.Info($"Default profile set to: {profileName}");
        }
        else
        {
            Logger.Warning($"Cannot set default profile: profile {profileId} not found");
        }
    }

    /// <summary>
    /// Duplicates an existing profile
    /// </summary>
    public ControllerProfile DuplicateProfile(string sourceProfileId, string newName)
    {
        Logger.Info($"Duplicating profile {sourceProfileId} as '{newName}'");

        ControllerProfile? sourceProfile = GetProfile(sourceProfileId);
        if (sourceProfile == null)
        {
            Logger.Error($"Source profile not found: {sourceProfileId}");
            throw new ArgumentException($"Profile not found: {sourceProfileId}");
        }

        Logger.Debug($"Source profile: {sourceProfile.Name}");

        ControllerProfile newProfile = new ControllerProfile
        {
            Id = Guid.NewGuid().ToString(),
            Name = newName,
            Lightbar = new LightbarSettings
            {
                Behavior = sourceProfile.Lightbar.Behavior,
                Red = sourceProfile.Lightbar.Red,
                Green = sourceProfile.Lightbar.Green,
                Blue = sourceProfile.Lightbar.Blue
            },
            PlayerLeds = new PlayerLedSettings
            {
                Pattern = sourceProfile.PlayerLeds.Pattern,
                Brightness = sourceProfile.PlayerLeds.Brightness
            },
            MicLed = sourceProfile.MicLed
        };

        Logger.Debug($"New profile ID: {newProfile.Id}");
        Logger.Debug($"Copied settings: Lightbar=RGB({newProfile.Lightbar.Red},{newProfile.Lightbar.Green},{newProfile.Lightbar.Blue}), PlayerLEDs={newProfile.PlayerLeds.Pattern}");

        SaveProfile(newProfile);
        Logger.Info($"Profile duplicated successfully: '{newName}' (ID: {newProfile.Id})");

        return newProfile;
    }

    /// <summary>
    /// Creates a profile from current controller state
    /// </summary>
    public ControllerProfile CreateProfileFromController(DualSenseController controller, string name)
    {
        Logger.Info($"Creating profile '{name}' from controller state");

        LightbarColor lightbar = controller.CurrentLightbarColor;
        Logger.Debug($"Current controller state:");
        Logger.Debug($"  Lightbar: RGB({lightbar.Red}, {lightbar.Green}, {lightbar.Blue})");
        Logger.Debug($"  Player LEDs: {controller.CurrentPlayerLeds} @ {controller.CurrentPlayerLedBrightness}");
        Logger.Debug($"  Mic LED: {controller.CurrentMicLed}");

        ControllerProfile profile = new ControllerProfile
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Lightbar = new LightbarSettings
            {
                Behavior = controller.CurrentLightbarBehavior,
                Red = lightbar.Red,
                Green = lightbar.Green,
                Blue = lightbar.Blue
            },
            PlayerLeds = new PlayerLedSettings
            {
                Pattern = controller.CurrentPlayerLeds,
                Brightness = controller.CurrentPlayerLedBrightness
            },
            MicLed = controller.CurrentMicLed
        };

        Logger.Debug($"Profile ID: {profile.Id}");
        SaveProfile(profile);
        Logger.Info($"Profile created from controller state successfully");

        return profile;
    }

    /// <summary>
    /// Applies a profile to a controller
    /// </summary>
    public void ApplyProfileToController(DualSenseController controller, ControllerProfile profile)
    {
        Logger.Info($"Applying profile '{profile.Name}' to controller");
        Logger.Debug($"Profile settings:");
        Logger.Debug($"  Lightbar: RGB({profile.Lightbar.Red}, {profile.Lightbar.Green}, {profile.Lightbar.Blue})");
        Logger.Debug($"  Player LEDs: {profile.PlayerLeds.Pattern} @ {profile.PlayerLeds.Brightness}");
        Logger.Debug($"  Mic LED: {profile.MicLed}");

        try
        {
            // Apply lightbar settings
            controller.SetLightbar(profile.Lightbar.Red, profile.Lightbar.Green, profile.Lightbar.Blue);
            Logger.Trace("Lightbar applied");

            // Apply player LEDs
            controller.SetPlayerLeds(profile.PlayerLeds.Pattern, profile.PlayerLeds.Brightness);
            Logger.Trace("Player LEDs applied");

            // Apply mic LED
            controller.SetMicLed(profile.MicLed);
            Logger.Trace("Mic LED applied");

            Logger.Info("Profile applied to controller successfully");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to apply profile to controller");
            Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
            throw;
        }
    }

    /// <summary>
    /// Gets all known controllers
    /// </summary>
    public Dictionary<string, ControllerInfo> GetKnownControllers()
    {
        int count = _settingsManager.Application.Controllers.KnownControllers.Count;
        Logger.Trace($"GetKnownControllers: returning {count} controller(s)");
        return _settingsManager.Application.Controllers.KnownControllers;
    }

    /// <summary>
    /// Updates a controller's name
    /// </summary>
    public void UpdateControllerName(string controllerId, string newName)
    {
        Logger.Info($"Updating controller name: {controllerId} -> '{newName}'");

        if (_settingsManager.Application.Controllers.KnownControllers.TryGetValue(controllerId, out var controllerInfo))
        {
            string oldName = controllerInfo.Name;
            controllerInfo.Name = newName;
            _settingsManager.SaveAll();
            Logger.Info($"Controller name updated: '{oldName}' -> '{newName}'");
        }
        else
        {
            Logger.Warning($"Cannot update name: controller {controllerId} not found");
        }
    }

    public void Dispose()
    {
        Logger.Info("Disposing DualSenseProfileManager");

        _dualSenseManager.ControllerConnected -= OnControllerConnected;
        _dualSenseManager.ControllerDisconnected -= OnControllerDisconnected;

        Logger.Debug("DualSenseProfileManager disposed");
    }
}