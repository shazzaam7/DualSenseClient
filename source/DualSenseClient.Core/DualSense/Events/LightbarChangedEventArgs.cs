using DualSenseClient.Core.DualSense.Enums;

namespace DualSenseClient.Core.DualSense.Events;

/// <summary>
/// Event args for lightbar state changes
/// </summary>
public class LightbarChangedEventArgs : EventArgs
{
    public LightbarColor CurrentColor { get; }
    public LightbarColor PreviousColor { get; }
    public LightbarBehavior CurrentBehavior { get; }
    public LightbarBehavior PreviousBehavior { get; }

    public LightbarChangedEventArgs(LightbarColor currentColor, LightbarColor previousColor, LightbarBehavior currentBehavior, LightbarBehavior previousBehavior)
    {
        CurrentColor = currentColor;
        PreviousColor = previousColor;
        CurrentBehavior = currentBehavior;
        PreviousBehavior = previousBehavior;
    }
}