using CommunityToolkit.Mvvm.ComponentModel;

namespace DualSenseClient.Core.DualSense.Reports;

/// <summary>
/// Connection status for peripherals and power
/// </summary>
public partial class ConnectionStatus : ObservableObject
{
    [ObservableProperty] private bool _isHeadphoneConnected;
    [ObservableProperty] private bool _isMicConnected;
    [ObservableProperty] private bool _isMicMuted;
    [ObservableProperty] private bool _isUsbDataConnected;
    [ObservableProperty] private bool _isUsbPowerConnected;
}