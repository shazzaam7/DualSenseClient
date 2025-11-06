namespace DualSenseClient.Core.DualSense.Enums;

/// <summary>
/// Behavior for the microphone mute LED button on the Dualsense
/// </summary>
public enum MicLed
{
    /// <summary>
    /// Microphone LED is turned off
    /// </summary>
    Off = 0,

    /// <summary>
    /// Microphone LED is turned on
    /// </summary>
    On = 1,

    /// <summary>
    /// Microphone LED pulses between on and off
    /// </summary>
    Pulse = 2
}