using DualSenseClient.Core.DualSense.Enums;

namespace DualSenseClient.Core.DualSense.Events;

public class ButtonEventArgs : EventArgs
{
    public ButtonType Button { get; }

    public ButtonEventArgs(ButtonType button)
    {
        Button = button;
    }
}