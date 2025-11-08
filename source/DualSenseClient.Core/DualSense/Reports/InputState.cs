using CommunityToolkit.Mvvm.ComponentModel;

namespace DualSenseClient.Core.DualSense.Reports;

public partial class InputState : ObservableObject
{
    // Analog sticks (0-255, center is 128)
    [ObservableProperty] private byte _leftStickX = 128;
    [ObservableProperty] private byte _leftStickY = 128;
    [ObservableProperty] private byte _rightStickX = 128;
    [ObservableProperty] private byte _rightStickY = 128;

    // Analog triggers (0-255)
    [ObservableProperty] private byte _l2;
    [ObservableProperty] private byte _r2;

    // Digital trigger buttons
    [ObservableProperty] private bool _l2Button;
    [ObservableProperty] private bool _r2Button;

    // D-Pad
    [ObservableProperty] private bool _dPadUp;
    [ObservableProperty] private bool _dPadDown;
    [ObservableProperty] private bool _dPadLeft;
    [ObservableProperty] private bool _dPadRight;

    // Face buttons
    [ObservableProperty] private bool _cross;
    [ObservableProperty] private bool _circle;
    [ObservableProperty] private bool _square;
    [ObservableProperty] private bool _triangle;

    // Shoulder buttons
    [ObservableProperty] private bool _l1;
    [ObservableProperty] private bool _r1;
    [ObservableProperty] private bool _l3;
    [ObservableProperty] private bool _r3;

    // System buttons
    [ObservableProperty] private bool _create;
    [ObservableProperty] private bool _options;
    [ObservableProperty] private bool _pS;
    [ObservableProperty] private bool _touchPadClick;
    [ObservableProperty] private bool _mute;

    // Gyroscope (Angular Velocity)
    [ObservableProperty] private short _gyroX;
    [ObservableProperty] private short _gyroY;
    [ObservableProperty] private short _gyroZ;

    // Accelerometer
    [ObservableProperty] private short _accelX;
    [ObservableProperty] private short _accelY;
    [ObservableProperty] private short _accelZ;

    // Touchpad
    [ObservableProperty] private TouchPoint _touch1 = new TouchPoint();
    [ObservableProperty] private TouchPoint _touch2 = new TouchPoint();

    // Connection status
    [ObservableProperty] private bool _isHeadphoneConnected;
    [ObservableProperty] private bool _isMicConnected;
    [ObservableProperty] private bool _isMicMuted;
    [ObservableProperty] private bool _isUsbDataConnected;
    [ObservableProperty] private bool _isUsbPowerConnected;
}

public partial class TouchPoint : ObservableObject
{
    [ObservableProperty] private byte _index;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private ushort _x; // 0-1919
    [ObservableProperty] private ushort _y; // 0-1079
}