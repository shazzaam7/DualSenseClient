using DualSenseClient.Core.DualSense.Enums;

namespace DualSenseClient.Core.DualSense.Events;

/// <summary>
/// Event args for player LED state changes
/// </summary>
public class PlayerLedsChangedEventArgs : EventArgs
{
    public PlayerLed CurrentLeds { get; }
    public PlayerLed PreviousLeds { get; }
    public PlayerLedBrightness CurrentBrightness { get; }
    public PlayerLedBrightness PreviousBrightness { get; }

    public PlayerLedsChangedEventArgs(
        PlayerLed currentLeds,
        PlayerLed previousLeds,
        PlayerLedBrightness currentBrightness,
        PlayerLedBrightness previousBrightness)
    {
        CurrentLeds = currentLeds;
        PreviousLeds = previousLeds;
        CurrentBrightness = currentBrightness;
        PreviousBrightness = previousBrightness;
    }
}