namespace DualSenseClient.Core.DualSense.Reports;

public class InputState
{
    // Analog sticks (0-255, center is 128)
    public byte LeftStickX { get; set; } = 128;
    public byte LeftStickY { get; set; } = 128;
    public byte RightStickX { get; set; } = 128;
    public byte RightStickY { get; set; } = 128;

    // Analog triggers (0-255)
    public byte L2 { get; set; }
    public byte R2 { get; set; }

    // Digital trigger buttons
    public bool L2Button { get; set; }
    public bool R2Button { get; set; }

    // D-Pad
    public bool DPadUp { get; set; }
    public bool DPadDown { get; set; }
    public bool DPadLeft { get; set; }
    public bool DPadRight { get; set; }

    // Face buttons
    public bool Cross { get; set; }
    public bool Circle { get; set; }
    public bool Square { get; set; }
    public bool Triangle { get; set; }

    // Shoulder buttons
    public bool L1 { get; set; }
    public bool R1 { get; set; }
    public bool L3 { get; set; }
    public bool R3 { get; set; }

    // System buttons
    public bool Create { get; set; }
    public bool Options { get; set; }
    public bool PS { get; set; }
    public bool TouchPadClick { get; set; }
    public bool Mute { get; set; }

    // Gyroscope (Angular Velocity)
    public short GyroX { get; set; }
    public short GyroY { get; set; }
    public short GyroZ { get; set; }

    // Accelerometer
    public short AccelX { get; set; }
    public short AccelY { get; set; }
    public short AccelZ { get; set; }

    // Touchpad
    public TouchPoint Touch1 { get; set; } = new();
    public TouchPoint Touch2 { get; set; } = new();

    // Connection status
    public bool IsHeadphoneConnected { get; set; }
    public bool IsMicConnected { get; set; }
    public bool IsMicMuted { get; set; }
    public bool IsUsbDataConnected { get; set; }
    public bool IsUsbPowerConnected { get; set; }
}

public class TouchPoint
{
    public byte Index { get; set; }
    public bool IsActive { get; set; }
    public ushort X { get; set; } // 0-1919
    public ushort Y { get; set; } // 0-1079
}