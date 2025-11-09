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
        _profileManager = profileManager;
        InitializeLightControls();
        LoadProfiles();
    }

    // Functions
    private void InitializeLightControls()
    {
        // Lightbar
        LightbarRed = _controller.CurrentLightbarColor.Red;
        LightbarGreen = _controller.CurrentLightbarColor.Green;
        LightbarBlue = _controller.CurrentLightbarColor.Blue;

        // Player Leds
        PlayerLed1 = _controller.CurrentPlayerLeds.HasFlag(PlayerLed.LED_1);
        PlayerLed2 = _controller.CurrentPlayerLeds.HasFlag(PlayerLed.LED_2);
        PlayerLed3 = _controller.CurrentPlayerLeds.HasFlag(PlayerLed.LED_3);
        PlayerLed4 = _controller.CurrentPlayerLeds.HasFlag(PlayerLed.LED_4);
        PlayerLed5 = _controller.CurrentPlayerLeds.HasFlag(PlayerLed.LED_5);

        PlayerLedBrightnessIndex = _controller.CurrentPlayerLedBrightness switch
        {
            PlayerLedBrightness.High => 0,
            PlayerLedBrightness.Medium => 1,
            PlayerLedBrightness.Low => 2,
            _ => 0
        };

        // Microphone Led
        MicLed = _controller.CurrentMicLed;
    }

    private void LoadProfiles()
    {
        Profiles.Clear();

        foreach (ControllerProfile profile in _profileManager.GetAllProfiles().Values)
        {
            Profiles.Add(profile);
        }

        if (_controllerInfo != null)
        {
            ControllerProfile assignedProfile = _profileManager.GetControllerProfile(_controllerInfo.Id);
            SelectedProfile = Profiles.FirstOrDefault(profile => profile.Id == assignedProfile.Id);
        }

        if (SelectedProfile == null)
        {
            SelectedProfile = Profiles.FirstOrDefault();
        }
    }

    partial void OnLightbarRedChanged(byte value) => UpdateLightbarPreview();
    partial void OnLightbarGreenChanged(byte value) => UpdateLightbarPreview();
    partial void OnLightbarBlueChanged(byte value) => UpdateLightbarPreview();

    private void UpdateLightbarPreview()
    {
        LightbarPreviewColor = new SolidColorBrush(Color.FromRgb(LightbarRed, LightbarGreen, LightbarBlue));
        LightbarColorHex = $"#{LightbarRed:X2}{LightbarGreen:X2}{LightbarBlue:X2}";
    }

    partial void OnSelectedProfileChanged(ControllerProfile? value)
    {
        if (value != null)
        {
            LoadProfileIntoControls(value);
        }
    }

    private void LoadProfileIntoControls(ControllerProfile profile)
    {
        // Load Lightbar
        LightbarRed = profile.Lightbar.Red;
        LightbarGreen = profile.Lightbar.Green;
        LightbarBlue = profile.Lightbar.Blue;
        UpdateLightbarPreview();

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

        // Microphone Led
        MicLed = profile.MicLed;
    }

    private PlayerLed GetCurrentPlayerLedPattern()
    {
        PlayerLed pattern = PlayerLed.None;
        if (PlayerLed1) pattern |= PlayerLed.LED_1;
        if (PlayerLed2) pattern |= PlayerLed.LED_2;
        if (PlayerLed3) pattern |= PlayerLed.LED_3;
        if (PlayerLed4) pattern |= PlayerLed.LED_4;
        if (PlayerLed5) pattern |= PlayerLed.LED_5;
        return pattern;
    }

    private PlayerLedBrightness GetCurrentPlayerLedBrightness()
    {
        return PlayerLedBrightnessIndex switch
        {
            0 => PlayerLedBrightness.High,
            1 => PlayerLedBrightness.Medium,
            2 => PlayerLedBrightness.Low,
            _ => PlayerLedBrightness.High
        };
    }

    [RelayCommand]
    private void ApplySelectedProfile()
    {
        if (SelectedProfile != null)
        {
            _profileManager.ApplyProfileToController(_controller, SelectedProfile);

            if (_controllerInfo != null)
            {
                _profileManager.AssignProfileToController(_controllerInfo.Id, SelectedProfile.Id);
            }

            InitializeLightControls();
        }
    }

    [RelayCommand]
    private void SaveCurrentAsProfile()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            return;
        }

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

        _profileManager.SaveProfile(profile);
        LoadProfiles();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == profile.Id);
        NewProfileName = string.Empty;
    }

    [RelayCommand]
    private void UpdateSelectedProfile()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        // Update the selected profile with current control values
        SelectedProfile.Lightbar.Red = LightbarRed;
        SelectedProfile.Lightbar.Green = LightbarGreen;
        SelectedProfile.Lightbar.Blue = LightbarBlue;
        SelectedProfile.PlayerLeds.Pattern = GetCurrentPlayerLedPattern();
        SelectedProfile.PlayerLeds.Brightness = GetCurrentPlayerLedBrightness();
        SelectedProfile.MicLed = MicLed;

        _profileManager.SaveProfile(SelectedProfile);
    }


    [RelayCommand]
    private void DuplicateSelectedProfile()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        ControllerProfile newProfile = _profileManager.DuplicateProfile(SelectedProfile.Id, $"{SelectedProfile.Name} (Copy)");
        LoadProfiles();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == newProfile.Id);
    }

    [RelayCommand]
    private void DeleteSelectedProfile()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        string profileId = SelectedProfile.Id;
        if (_profileManager.DeleteProfile(profileId))
        {
            LoadProfiles();
            SelectedProfile = Profiles.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void SaveControllerStateAsProfile()
    {
        string profileName = string.IsNullOrWhiteSpace(NewProfileName)
            ? $"Profile {DateTime.Now:yyyy-MM-dd HH:mm}"
            : NewProfileName;

        ControllerProfile profile = _profileManager.CreateProfileFromController(_controller, profileName);
        LoadProfiles();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == profile.Id);
        NewProfileName = string.Empty;
    }

    [RelayCommand]
    private void SetLightbarPreset(string colorString)
    {
        string[] parts = colorString.Split(',');
        if (parts.Length != 3 || !byte.TryParse(parts[0], out byte r) || !byte.TryParse(parts[1], out byte g) || !byte.TryParse(parts[2], out byte b))
        {
            return;
        }
        LightbarRed = r;
        LightbarGreen = g;
        LightbarBlue = b;
    }

    [RelayCommand]
    private void ApplyLightbar()
    {
        _controller.SetLightbar(LightbarRed, LightbarGreen, LightbarBlue);
    }

    [RelayCommand]
    private void SetPlayerLeds(string preset)
    {
        switch (preset)
        {
            case "All":
                PlayerLed1 = PlayerLed2 = PlayerLed3 = PlayerLed4 = PlayerLed5 = true;
                break;
            case "None":
                PlayerLed1 = PlayerLed2 = PlayerLed3 = PlayerLed4 = PlayerLed5 = false;
                break;
            case "Center":
                PlayerLed1 = PlayerLed5 = false;
                PlayerLed2 = PlayerLed3 = PlayerLed4 = true;
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

        _controller.SetPlayerLeds(leds, brightness);
        InitializeLightControls();
    }

    [RelayCommand]
    private void ApplyMicLed()
    {
        _controller.SetMicLed(MicLed);
        InitializeLightControls();
    }

    [RelayCommand]
    private void ResetLightsToDefault()
    {
        _controller.SetLightbar(0, 0, 255);
        _controller.SetPlayerLeds(PlayerLed.None, PlayerLedBrightness.Low);
        _controller.SetMicLed(MicLed.Off);
        InitializeLightControls();
    }

    [RelayCommand]
    private void TurnOffAllLights()
    {
        _controller.SetLightbar(0, 0, 0);
        _controller.SetPlayerLeds(PlayerLed.None, PlayerLedBrightness.High);
        _controller.SetMicLed(MicLed.Off);

        InitializeLightControls();
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}