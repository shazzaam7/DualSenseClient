using DualSenseClient.Core.DualSense.Reports;

namespace DualSenseClient.Core.DualSense.Events;

public class ConnectionStatusEventArgs : EventArgs
{
    public ConnectionStatus CurrentStatus { get; }
    public ConnectionStatus PreviousStatus { get; }

    public ConnectionStatusEventArgs(ConnectionStatus current, ConnectionStatus previous)
    {
        CurrentStatus = current;
        PreviousStatus = previous;
    }
}