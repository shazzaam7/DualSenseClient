using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.DualSense.Events;
using DualSenseClient.Core.DualSense.Reports;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.ViewModels;

/// <summary>
/// Extended ViewModel for monitoring controller input and LED states
/// </summary>
public class ControllerMonitorViewModel : ControllerViewModelBase
{
    // Properties
    public InputState InputState => _controller.Input;
    public TouchpadState TouchpadState => _controller.Touchpad;
    public MotionState MotionState => _controller.Motion;
    public ConnectionStatus ConnectionStatus => _controller.ConnectionStatus;
    public LightbarColor CurrentLightbarColor => _controller.CurrentLightbarColor;
    public LightbarBehavior CurrentLightbarBehavior => _controller.CurrentLightbarBehavior;
    public PlayerLed CurrentPlayerLeds => _controller.CurrentPlayerLeds;
    public PlayerLedBrightness CurrentPlayerLedBrightness => _controller.CurrentPlayerLedBrightness;
    public MicLed CurrentMicLed => _controller.CurrentMicLed;

    // Constructor
    public ControllerMonitorViewModel(DualSenseController controller, ControllerInfo controllerInfo) : base(controller, controllerInfo)
    {
        // Subscribe to all relevant events for real-time monitoring
        _controller.InputChanged += OnInputChanged;
        _controller.TouchpadChanged += OnTouchpadChanged;
        _controller.MotionChanged += OnMotionChanged;
        _controller.ConnectionStatusChanged += OnConnectionStatusChanged;
        _controller.ButtonPressed += OnButtonPressed;
        _controller.ButtonReleased += OnButtonReleased;
        _controller.TriggerChanged += OnTriggerChanged;
        _controller.StickMoved += OnStickMoved;

        controller.LightbarChanged += OnLightbarChanged;
        controller.PlayerLedsChanged += OnPlayerLedsChanged;
        controller.MicLedChanged += OnMicLedChanged;
    }

    // Event Handlers
    private void OnInputChanged(object? sender, InputStateEventArgs e)
    {
        // Notify UI of input-related property changes
        OnPropertyChanged(nameof(InputState));
    }

    private void OnTouchpadChanged(object? sender, TouchpadEventArgs e)
    {
        OnPropertyChanged(nameof(TouchpadState));
    }

    private void OnMotionChanged(object? sender, MotionEventArgs e)
    {
        OnPropertyChanged(nameof(MotionState));
    }

    private void OnConnectionStatusChanged(object? sender, ConnectionStatusEventArgs e)
    {
        OnPropertyChanged(nameof(ConnectionStatus));
    }

    private void OnButtonPressed(object? sender, ButtonEventArgs e)
    {
        // Could log button presses
    }

    private void OnButtonReleased(object? sender, ButtonEventArgs e)
    {
        // Could log button presses
    }

    private void OnTriggerChanged(object? sender, TriggerEventArgs e)
    {
        // Could handle trigger-specific logic
    }

    private void OnStickMoved(object? sender, StickEventArgs e)
    {
        // Could handle stick-specific logic
    }

    private void OnLightbarChanged(object? sender, LightbarChangedEventArgs e)
    {
        Logger.Info($"Lightbar changed from RGB({e.PreviousColor.Red}, {e.PreviousColor.Green}, {e.PreviousColor.Blue}) " +
                    $"to RGB({e.CurrentColor.Red}, {e.CurrentColor.Green}, {e.CurrentColor.Blue})");
        Logger.Info($"Behavior changed from {e.PreviousBehavior} to {e.CurrentBehavior}");

        // Update UI or perform other actions
        OnPropertyChanged(nameof(CurrentLightbarColor));
        OnPropertyChanged(nameof(CurrentLightbarBehavior));
    }

    private void OnPlayerLedsChanged(object? sender, PlayerLedsChangedEventArgs e)
    {
        Logger.Info($"Player LEDs changed from {e.PreviousLeds} to {e.CurrentLeds}");
        Logger.Info($"Brightness changed from {e.PreviousBrightness} to {e.CurrentBrightness}");

        // Update UI
        OnPropertyChanged(nameof(CurrentPlayerLeds));
        OnPropertyChanged(nameof(CurrentPlayerLedBrightness));
    }

    private void OnMicLedChanged(object? sender, MicLedChangedEventArgs e)
    {
        Logger.Info($"Mic LED changed from {e.PreviousLed} to {e.CurrentLed}");

        // Update UI
        OnPropertyChanged(nameof(CurrentMicLed));
    }

    public override void Dispose()
    {
        // Unsubscribe from all events
        _controller.InputChanged -= OnInputChanged;
        _controller.TouchpadChanged -= OnTouchpadChanged;
        _controller.MotionChanged -= OnMotionChanged;
        _controller.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _controller.ButtonPressed -= OnButtonPressed;
        _controller.ButtonReleased -= OnButtonReleased;
        _controller.TriggerChanged -= OnTriggerChanged;
        _controller.StickMoved -= OnStickMoved;

        base.Dispose();
    }
}