using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.DualSense.Reports;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.ViewModels;

/// <summary>
/// Extended ViewModel for monitoring controller input and LED states
/// </summary>
public class ControllerMonitorViewModel : ControllerViewModelBase
{
    // Properties
    public InputState InputState => _controller.Input;
    public LightbarColor CurrentLightbarColor => _controller.CurrentLightbarColor;
    public LightbarBehavior CurrentLightbarBehavior => _controller.CurrentLightbarBehavior;
    public PlayerLed CurrentPlayerLeds => _controller.CurrentPlayerLeds;
    public PlayerLedBrightness CurrentPlayerLedBrightness => _controller.CurrentPlayerLedBrightness;
    public MicLed CurrentMicLed => _controller.CurrentMicLed;

    // Constructor
    public ControllerMonitorViewModel(DualSenseController controller, ControllerInfo? controllerInfo) : base(controller, controllerInfo)
    {
        // Subscribe to input changes for real-time monitoring
        _controller.InputChanged += OnInputChanged;
    }

    // Functions
    private void OnInputChanged(object? sender, InputState inputState)
    {
        // Notify UI of all input-related property changes
        OnPropertyChanged(nameof(InputState));
        OnPropertyChanged(nameof(CurrentLightbarColor));
        OnPropertyChanged(nameof(CurrentLightbarBehavior));
        OnPropertyChanged(nameof(CurrentPlayerLeds));
        OnPropertyChanged(nameof(CurrentPlayerLedBrightness));
        OnPropertyChanged(nameof(CurrentMicLed));
    }

    public override void Dispose()
    {
        _controller.InputChanged -= OnInputChanged;
        base.Dispose();
    }
}