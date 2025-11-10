using DualSenseClient.Core.DualSense.Reports;

namespace DualSenseClient.Core.DualSense.Events;

public class MotionEventArgs : EventArgs
{
    public MotionState CurrentState { get; }
    public MotionState PreviousState { get; }

    public MotionEventArgs(MotionState current, MotionState previous)
    {
        CurrentState = current;
        PreviousState = previous;
    }
}