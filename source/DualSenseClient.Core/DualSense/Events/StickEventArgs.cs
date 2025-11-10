using DualSenseClient.Core.DualSense.Enums;

namespace DualSenseClient.Core.DualSense.Events;

public class StickEventArgs : EventArgs
{
    public StickType Stick { get; }
    public byte X { get; }
    public byte Y { get; }
    public byte PreviousX { get; }
    public byte PreviousY { get; }

    public StickEventArgs(StickType stick, byte x, byte y, byte previousX, byte previousY)
    {
        Stick = stick;
        X = x;
        Y = y;
        PreviousX = previousX;
        PreviousY = previousY;
    }
}