using DualSenseClient.Core.DualSense.Reports;

namespace DualSenseClient.Core.DualSense.Events;

public class InputStateEventArgs : EventArgs
{
    public InputState CurrentState { get; }
    public InputState PreviousState { get; }

    public InputStateEventArgs(InputState current, InputState previous)
    {
        CurrentState = current;
        PreviousState = previous;
    }
}