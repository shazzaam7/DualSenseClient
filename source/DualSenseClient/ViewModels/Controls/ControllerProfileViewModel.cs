using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.Settings.Models;
using DualSenseClient.Core.Logging;

namespace DualSenseClient.ViewModels.Controls;

public partial class ControllerProfileViewModel : ControllerViewModelBase
{
    // Properties
    private readonly DualSenseProfileManager _profileManager;

    // Lightbar Control
    [ObservableProperty] private byte _lightbarRed;
    [ObservableProperty] private byte _lightbarGreen;
    [ObservableProperty] private byte _lightbarBlue;
    [ObservableProperty] private SolidColorBrush _lightbarPreviewColor = new SolidColorBrush(Colors.Black);
    [ObservableProperty] private string _lightbarColorHex = "#000000";

    // Player LED Control
    [ObservableProperty] private bool _playerLed1;
    [ObservableProperty] private bool _playerLed2;
    [ObservableProperty] private bool _playerLed3;
    [ObservableProperty] private bool _playerLed4;
    [ObservableProperty] private bool _playerLed5;
    [ObservableProperty] private int _playerLedBrightnessIndex;

    // Microphone LED Control
    [ObservableProperty] private MicLed _micLed;

    [ObservableProperty] private ObservableCollection<ControllerProfile> _profiles = new ObservableCollection<ControllerProfile>();
    [ObservableProperty] private ControllerProfile? _selectedProfile;
    [ObservableProperty] private string _newProfileName = string.Empty;

    // Constructor
    public ControllerProfileViewModel(DualSenseController controller, ControllerInfo? controllerInfo, DualSenseProfileManager profileManager) : base(controller, controllerInfo)
    {
        Logger.Debug($"Creating ControllerProfileViewModel for controller: {controllerInfo?.Name ?? "Unknown"}");
        _profileManager = profileManager;
        InitializeLightControls();
        LoadProfiles();
        Logger.Debug("ControllerProfileViewModel initialized successfully");
    }

    // Functions
    private void InitializeLightControls()
    {
        Logger.Debug("Initializing light controls from controller state");

        // Lightbar
        LightbarRed = _controller.CurrentLightbarColor.Red;
        LightbarGreen = _controller.CurrentLightbarColor.Green;
        LightbarBlue = _controller.CurrentLightbarColor.Blue;
        Logger.Trace($"Lightbar: RGB({LightbarRed}, {LightbarGreen}, {LightbarBlue})");

        // Player Leds
        PlayerLed1 = _controller.CurrentPlayerLeds.HasFlag(PlayerLed.LED_1);
        PlayerLed2 = _controller.CurrentPlayerLeds.HasFlag(PlayerLed.LED_2);
        PlayerLed3 = _controller.CurrentPlayerLeds.HasFlag(PlayerLed.LED_3);
        PlayerLed4 = _controller.CurrentPlayerLeds.HasFlag(PlayerLed.LED_4);
        PlayerLed5 = _controller.CurrentPlayerLeds.HasFlag(PlayerLed.LED_5);
        Logger.Trace($"Player LEDs: {_controller.CurrentPlayerLeds}");

        PlayerLedBrightnessIndex = _controller.CurrentPlayerLedBrightness switch
        {
            PlayerLedBrightness.High => 0,
            PlayerLedBrightness.Medium => 1,
            PlayerLedBrightness.Low => 2,
            _ => 0
        };
        Logger.Trace($"Player LED Brightness: {_controller.CurrentPlayerLedBrightness} (Index: {PlayerLedBrightnessIndex})");

        // Microphone Led
        MicLed = _controller.CurrentMicLed;
        Logger.Trace($"Mic LED: {MicLed}");
    }

    private void LoadProfiles()
    {
        Logger.Debug("Loading profiles into ViewModel");
        Profiles.Clear();

        foreach (ControllerProfile profile in _profileManager.GetAllProfiles().Values)
        {
            Profiles.Add(profile);
        }
        Logger.Debug($"Loaded {Profiles.Count} profile(s)");

        if (_controllerInfo != null)
        {
            ControllerProfile assignedProfile = _profileManager.GetControllerProfile(_controllerInfo.Id);
            SelectedProfile = Profiles.FirstOrDefault(profile => profile.Id == assignedProfile.Id);
            Logger.Debug($"Assigned profile for controller '{_controllerInfo.Name}': {assignedProfile.Name} (ID: {assignedProfile.Id})");
        }

        if (SelectedProfile == null)
        {
            SelectedProfile = Profiles.FirstOrDefault();
            Logger.Debug($"No assigned profile found, selected first available: {SelectedProfile?.Name ?? "None"}");
        }
    }

    partial void OnLightbarRedChanged(byte value)
    {
        Logger.Trace($"Lightbar Red changed: {value}");
        UpdateLightbarPreview();
    }

    partial void OnLightbarGreenChanged(byte value)
    {
        Logger.Trace($"Lightbar Green changed: {value}");
        UpdateLightbarPreview();
    }

    partial void OnLightbarBlueChanged(byte value)
    {
        Logger.Trace($"Lightbar Blue changed: {value}");
        UpdateLightbarPreview();
    }

    private void UpdateLightbarPreview()
    {
        LightbarPreviewColor = new SolidColorBrush(Color.FromRgb(LightbarRed, LightbarGreen, LightbarBlue));
        LightbarColorHex = $"#{LightbarRed:X2}{LightbarGreen:X2}{LightbarBlue:X2}";
        Logger.Trace($"Lightbar preview updated: {LightbarColorHex}");
    }

    partial void OnSelectedProfileChanged(ControllerProfile? value)
    {
        if (value != null)
        {
            Logger.Debug($"Selected profile changed to: {value.Name} (ID: {value.Id})");
            LoadProfileIntoControls(value);
        }
        else
        {
            Logger.Debug("Selected profile cleared");
        }
    }

    private void LoadProfileIntoControls(ControllerProfile profile)
    {
        Logger.Debug($"Loading profile '{profile.Name}' into controls");

        // Load Lightbar
        LightbarRed = profile.Lightbar.Red;
        LightbarGreen = profile.Lightbar.Green;
        LightbarBlue = profile.Lightbar.Blue;
        UpdateLightbarPreview();
        Logger.Trace($"  Lightbar: RGB({LightbarRed}, {LightbarGreen}, {LightbarBlue})");

        // Load Player LED
        PlayerLed1 = profile.PlayerLeds.Pattern.HasFlag(PlayerLed.LED_1);
        PlayerLed2 = profile.PlayerLeds.Pattern.HasFlag(PlayerLed.LED_2);
        PlayerLed3 = profile.PlayerLeds.Pattern.HasFlag(PlayerLed.LED_3);
        PlayerLed4 = profile.PlayerLeds.Pattern.HasFlag(PlayerLed.LED_4);
        PlayerLed5 = profile.PlayerLeds.Pattern.HasFlag(PlayerLed.LED_5);
        PlayerLedBrightnessIndex = profile.PlayerLeds.Brightness switch
        {
            PlayerLedBrightness.High => 0,
            PlayerLedBrightness.Medium => 1,
            PlayerLedBrightness.Low => 2,
            _ => 0
        };
        Logger.Trace($"  Player LEDs: {profile.PlayerLeds.Pattern}, Brightness: {profile.PlayerLeds.Brightness}");

        // Microphone Led
        MicLed = profile.MicLed;
        Logger.Trace($"  Mic LED: {MicLed}");

        Logger.Debug("Profile loaded into controls successfully");
    }

    private PlayerLed GetCurrentPlayerLedPattern()
    {
        PlayerLed pattern = PlayerLed.None;
        if (PlayerLed1) pattern |= PlayerLed.LED_1;
        if (PlayerLed2) pattern |= PlayerLed.LED_2;
        if (PlayerLed3) pattern |= PlayerLed.LED_3;
        if (PlayerLed4) pattern |= PlayerLed.LED_4;
        if (PlayerLed5) pattern |= PlayerLed.LED_5;
        Logger.Trace($"Current player LED pattern: {pattern}");
        return pattern;
    }

    private PlayerLedBrightness GetCurrentPlayerLedBrightness()
    {
        PlayerLedBrightness brightness = PlayerLedBrightnessIndex switch
        {
            0 => PlayerLedBrightness.High,
            1 => PlayerLedBrightness.Medium,
            2 => PlayerLedBrightness.Low,
            _ => PlayerLedBrightness.High
        };
        Logger.Trace($"Current player LED brightness: {brightness}");
        return brightness;
    }

    [RelayCommand]
    private void ApplySelectedProfile()
    {
        if (SelectedProfile == null)
        {
            Logger.Warning("ApplySelectedProfile called but no profile is selected");
            return;
        }

        Logger.Info($"Applying profile '{SelectedProfile.Name}' to controller");
        _profileManager.ApplyProfileToController(_controller, SelectedProfile);

        if (_controllerInfo != null)
        {
            Logger.Debug($"Assigning profile '{SelectedProfile.Name}' to controller '{_controllerInfo.Name}'");
            _profileManager.AssignProfileToController(_controllerInfo.Id, SelectedProfile.Id);
        }

        InitializeLightControls();
        Logger.Info("Profile applied successfully");
    }

    [RelayCommand]
    private void SaveCurrentAsProfile()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            Logger.Warning("SaveCurrentAsProfile called with empty profile name");
            return;
        }

        Logger.Info($"Creating new profile '{NewProfileName}' from current controls");

        // Create profile from current control values
        ControllerProfile profile = new ControllerProfile
        {
            Id = Guid.NewGuid().ToString(),
            Name = NewProfileName,
            Lightbar = new LightbarSettings
            {
                Behavior = LightbarBehavior.Custom,
                Red = LightbarRed,
                Green = LightbarGreen,
                Blue = LightbarBlue
            },
            PlayerLeds = new PlayerLedSettings
            {
                Pattern = GetCurrentPlayerLedPattern(),
                Brightness = GetCurrentPlayerLedBrightness()
            },
            MicLed = MicLed
        };

        Logger.Debug($"Profile details: ID={profile.Id}, Lightbar=RGB({profile.Lightbar.Red},{profile.Lightbar.Green},{profile.Lightbar.Blue}), PlayerLEDs={profile.PlayerLeds.Pattern}, MicLED={profile.MicLed}");

        _profileManager.SaveProfile(profile);
        LoadProfiles();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == profile.Id);
        NewProfileName = string.Empty;

        Logger.Info($"Profile '{profile.Name}' created and saved successfully");
    }

    [RelayCommand]
    private void UpdateSelectedProfile()
    {
        if (SelectedProfile == null)
        {
            Logger.Warning("UpdateSelectedProfile called but no profile is selected");
            return;
        }

        Logger.Info($"Updating profile '{SelectedProfile.Name}' (ID: {SelectedProfile.Id})");

        // Update the selected profile with current control values
        SelectedProfile.Lightbar.Red = LightbarRed;
        SelectedProfile.Lightbar.Green = LightbarGreen;
        SelectedProfile.Lightbar.Blue = LightbarBlue;
        SelectedProfile.PlayerLeds.Pattern = GetCurrentPlayerLedPattern();
        SelectedProfile.PlayerLeds.Brightness = GetCurrentPlayerLedBrightness();
        SelectedProfile.MicLed = MicLed;

        Logger.Debug($"Updated values: Lightbar=RGB({LightbarRed},{LightbarGreen},{LightbarBlue}), PlayerLEDs={SelectedProfile.PlayerLeds.Pattern}, MicLED={MicLed}");

        _profileManager.SaveProfile(SelectedProfile);
        Logger.Info("Profile updated successfully");
    }

    [RelayCommand]
    private void DuplicateSelectedProfile()
    {
        if (SelectedProfile == null)
        {
            Logger.Warning("DuplicateSelectedProfile called but no profile is selected");
            return;
        }

        Logger.Info($"Duplicating profile '{SelectedProfile.Name}' (ID: {SelectedProfile.Id})");

        ControllerProfile newProfile = _profileManager.DuplicateProfile(SelectedProfile.Id, $"{SelectedProfile.Name} (Copy)");

        Logger.Debug($"New profile created: '{newProfile.Name}' (ID: {newProfile.Id})");

        LoadProfiles();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == newProfile.Id);

        Logger.Info("Profile duplicated successfully");
    }

    [RelayCommand]
    private void DeleteSelectedProfile()
    {
        if (SelectedProfile == null)
        {
            Logger.Warning("DeleteSelectedProfile called but no profile is selected");
            return;
        }

        string profileId = SelectedProfile.Id;
        string profileName = SelectedProfile.Name;

        Logger.Info($"Deleting profile '{profileName}' (ID: {profileId})");

        if (_profileManager.DeleteProfile(profileId))
        {
            Logger.Debug("Profile deleted from manager, reloading profile list");
            LoadProfiles();
            SelectedProfile = Profiles.FirstOrDefault();
            Logger.Info($"Profile '{profileName}' deleted successfully");
        }
        else
        {
            Logger.Warning($"Failed to delete profile '{profileName}' (possibly default profile)");
        }
    }

    [RelayCommand]
    private void SaveControllerStateAsProfile()
    {
        string profileName = string.IsNullOrWhiteSpace(NewProfileName) ? $"Profile {DateTime.Now:yyyy-MM-dd HH:mm}" : NewProfileName;

        Logger.Info($"Creating profile '{profileName}' from current controller state");

        ControllerProfile profile = _profileManager.CreateProfileFromController(_controller, profileName);

        Logger.Debug($"Profile created with ID: {profile.Id}");

        LoadProfiles();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == profile.Id);
        NewProfileName = string.Empty;

        Logger.Info("Controller state saved as profile successfully");
    }

    [RelayCommand]
    private void SetLightbarPreset(string colorString)
    {
        Logger.Debug($"Setting lightbar preset: {colorString}");

        string[] parts = colorString.Split(',');
        if (parts.Length != 3 || !byte.TryParse(parts[0], out byte r) || !byte.TryParse(parts[1], out byte g) || !byte.TryParse(parts[2], out byte b))
        {
            Logger.Warning($"Invalid color string format: {colorString}");
            return;
        }

        LightbarRed = r;
        LightbarGreen = g;
        LightbarBlue = b;

        Logger.Debug($"Lightbar preset applied: RGB({r}, {g}, {b})");
    }

    [RelayCommand]
    private void ApplyLightbar()
    {
        Logger.Info($"Applying lightbar color to controller: RGB({LightbarRed}, {LightbarGreen}, {LightbarBlue})");
        _controller.SetLightbar(LightbarRed, LightbarGreen, LightbarBlue);
        Logger.Debug("Lightbar color applied successfully");
    }

    [RelayCommand]
    private void SetPlayerLeds(string preset)
    {
        Logger.Debug($"Setting player LED preset: {preset}");

        switch (preset)
        {
            case "All":
                PlayerLed1 = PlayerLed2 = PlayerLed3 = PlayerLed4 = PlayerLed5 = true;
                Logger.Trace("All player LEDs enabled");
                break;
            case "None":
                PlayerLed1 = PlayerLed2 = PlayerLed3 = PlayerLed4 = PlayerLed5 = false;
                Logger.Trace("All player LEDs disabled");
                break;
            case "Center":
                PlayerLed1 = PlayerLed5 = false;
                PlayerLed2 = PlayerLed3 = PlayerLed4 = true;
                Logger.Trace("Center player LEDs enabled");
                break;
            default:
                Logger.Warning($"Unknown player LED preset: {preset}");
                break;
        }
    }

    [RelayCommand]
    private void ApplyPlayerLeds()
    {
        PlayerLed leds = PlayerLed.None;
        if (PlayerLed1) leds |= PlayerLed.LED_1;
        if (PlayerLed2) leds |= PlayerLed.LED_2;
        if (PlayerLed3) leds |= PlayerLed.LED_3;
        if (PlayerLed4) leds |= PlayerLed.LED_4;
        if (PlayerLed5) leds |= PlayerLed.LED_5;

        PlayerLedBrightness brightness = PlayerLedBrightnessIndex switch
        {
            0 => PlayerLedBrightness.High,
            1 => PlayerLedBrightness.Medium,
            2 => PlayerLedBrightness.Low,
            _ => PlayerLedBrightness.High
        };

        Logger.Info($"Applying player LEDs to controller: Pattern={leds}, Brightness={brightness}");
        _controller.SetPlayerLeds(leds, brightness);
        InitializeLightControls();
        Logger.Debug("Player LEDs applied successfully");
    }

    [RelayCommand]
    private void ApplyMicLed()
    {
        Logger.Info($"Applying mic LED to controller: {MicLed}");
        _controller.SetMicLed(MicLed);
        InitializeLightControls();
        Logger.Debug("Mic LED applied successfully");
    }

    [RelayCommand]
    private void ResetLightsToDefault()
    {
        Logger.Info("Resetting all lights to default values");
        _controller.SetLightbar(0, 0, 255);
        _controller.SetPlayerLeds(PlayerLed.None, PlayerLedBrightness.Low);
        _controller.SetMicLed(MicLed.Off);
        InitializeLightControls();
        Logger.Debug("Lights reset to default successfully");
    }

    [RelayCommand]
    private void TurnOffAllLights()
    {
        Logger.Info("Turning off all lights");
        _controller.SetLightbar(0, 0, 0);
        _controller.SetPlayerLeds(PlayerLed.None, PlayerLedBrightness.High);
        _controller.SetMicLed(MicLed.Off);
        InitializeLightControls();
        Logger.Debug("All lights turned off successfully");
    }

    public override void Dispose()
    {
        Logger.Debug($"Disposing ControllerProfileViewModel for controller: {_controllerInfo?.Name ?? "Unknown"}");
        base.Dispose();
    }
}