using CommunityToolkit.Mvvm.ComponentModel;

namespace DualSenseClient.Core.DualSense.Reports;

/// <summary>
/// Touchpad state containing both touch points
/// </summary>
public partial class TouchpadState : ObservableObject
{
    [ObservableProperty] private TouchPoint _touch1 = new TouchPoint();
    [ObservableProperty] private TouchPoint _touch2 = new TouchPoint();
}