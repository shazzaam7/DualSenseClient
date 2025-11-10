using CommunityToolkit.Mvvm.ComponentModel;

namespace DualSenseClient.Core.DualSense.Reports;

/// <summary>
/// Motion sensor state (Gyroscope and Accelerometer)
/// </summary>
public partial class MotionState : ObservableObject
{
    // Gyroscope
    [ObservableProperty] private short _gyroX;
    [ObservableProperty] private short _gyroY;
    [ObservableProperty] private short _gyroZ;

    // Accelerometer
    [ObservableProperty] private short _accelX;
    [ObservableProperty] private short _accelY;
    [ObservableProperty] private short _accelZ;
}