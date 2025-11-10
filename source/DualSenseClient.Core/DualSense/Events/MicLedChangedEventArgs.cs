using DualSenseClient.Core.DualSense.Enums;

namespace DualSenseClient.Core.DualSense.Events;

/// <summary>
/// Event args for mic LED state changes
/// </summary>
public class MicLedChangedEventArgs : EventArgs
{
    public MicLed CurrentLed { get; }
    public MicLed PreviousLed { get; }

    public MicLedChangedEventArgs(MicLed currentLed, MicLed previousLed)
    {
        CurrentLed = currentLed;
        PreviousLed = previousLed;
    }
}