using DualSenseClient.Core.DualSense.Reports;

namespace DualSenseClient.Core.DualSense.Events;

public class TouchpadEventArgs : EventArgs
{
    public TouchpadState CurrentState { get; }
    public TouchpadState PreviousState { get; }

    public TouchpadEventArgs(TouchpadState current, TouchpadState previous)
    {
        CurrentState = current;
        PreviousState = previous;
    }
}