using DualSenseClient.Core.DualSense.Reports;

namespace DualSenseClient.Core.DualSense.Events;

public class BatteryStateEventArgs : EventArgs
{
    public BatteryState CurrentState { get; }
    public BatteryState PreviousState { get; }

    public BatteryStateEventArgs(BatteryState current, BatteryState previous)
    {
        CurrentState = current;
        PreviousState = previous;
    }
}