using DualSenseClient.Core.DualSense.Enums;

namespace DualSenseClient.Core.DualSense.Events;

public class TriggerEventArgs : EventArgs
{
    public TriggerType Trigger { get; }
    public byte CurrentValue { get; }
    public byte PreviousValue { get; }

    public TriggerEventArgs(TriggerType trigger, byte currentValue, byte previousValue)
    {
        Trigger = trigger;
        CurrentValue = currentValue;
        PreviousValue = previousValue;
    }
}