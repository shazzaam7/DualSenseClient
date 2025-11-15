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
        Logger.Debug<ControllerMonitorViewModel>($"Creating ControllerMonitorViewModel for: {controllerInfo.Name}");

        // Subscribe to all relevant events for real-time monitoring
        Logger.Trace<ControllerMonitorViewModel>("Subscribing to controller events");

        _controller.InputChanged += OnInputChanged;
        _controller.TouchpadChanged += OnTouchpadChanged;
        _controller.MotionChanged += OnMotionChanged;
        _controller.ConnectionStatusChanged += OnConnectionStatusChanged;
        //_controller.ButtonPressed += OnButtonPressed;
        //_controller.ButtonReleased += OnButtonReleased;
        //_controller.TriggerChanged += OnTriggerChanged;
        //_controller.StickMoved += OnStickMoved;
        _controller.LightbarChanged += OnLightbarChanged;
        _controller.PlayerLedsChanged += OnPlayerLedsChanged;
        _controller.MicLedChanged += OnMicLedChanged;

        Logger.Debug<ControllerMonitorViewModel>("ControllerMonitorViewModel created successfully");
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
        // Could handle button pressed logic
    }

    private void OnButtonReleased(object? sender, ButtonEventArgs e)
    {
        // Could handle button release logic
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
        Logger.Info<ControllerMonitorViewModel>(
            $"Lightbar color changed: RGB({e.PreviousColor.Red}, {e.PreviousColor.Green}, {e.PreviousColor.Blue}) " +
            $"-> RGB({e.CurrentColor.Red}, {e.CurrentColor.Green}, {e.CurrentColor.Blue})");

        Logger.Debug<ControllerMonitorViewModel>(
            $"Lightbar behavior changed: {e.PreviousBehavior} -> {e.CurrentBehavior}");

        // Update UI or perform other actions
        OnPropertyChanged(nameof(CurrentLightbarColor));
        OnPropertyChanged(nameof(CurrentLightbarBehavior));
    }

    private void OnPlayerLedsChanged(object? sender, PlayerLedsChangedEventArgs e)
    {
        Logger.Info<ControllerMonitorViewModel>($"Player LEDs changed: {e.PreviousLeds} -> {e.CurrentLeds}");
        Logger.Debug<ControllerMonitorViewModel>($"Player LED brightness changed: {e.PreviousBrightness} -> {e.CurrentBrightness}");

        // Update UI
        OnPropertyChanged(nameof(CurrentPlayerLeds));
        OnPropertyChanged(nameof(CurrentPlayerLedBrightness));
    }

    private void OnMicLedChanged(object? sender, MicLedChangedEventArgs e)
    {
        Logger.Info<ControllerMonitorViewModel>($"Mic LED changed: {e.PreviousLed} -> {e.CurrentLed}");

        // Update UI
        OnPropertyChanged(nameof(CurrentMicLed));
    }

    public override void Dispose()
    {
        Logger.Debug<ControllerMonitorViewModel>($"Disposing ControllerMonitorViewModel for: {Name}");
        Logger.Trace<ControllerMonitorViewModel>("Unsubscribing from controller events");

        // Unsubscribe from all events
        _controller.InputChanged -= OnInputChanged;
        _controller.TouchpadChanged -= OnTouchpadChanged;
        _controller.MotionChanged -= OnMotionChanged;
        _controller.ConnectionStatusChanged -= OnConnectionStatusChanged;
        //_controller.ButtonPressed -= OnButtonPressed;
        //_controller.ButtonReleased -= OnButtonReleased;
        //_controller.TriggerChanged -= OnTriggerChanged;
        //_controller.StickMoved -= OnStickMoved;
        _controller.LightbarChanged -= OnLightbarChanged;
        _controller.PlayerLedsChanged -= OnPlayerLedsChanged;
        _controller.MicLedChanged -= OnMicLedChanged;

        Logger.Debug<ControllerMonitorViewModel>("ControllerMonitorViewModel disposed successfully");

        base.Dispose();
    }
}