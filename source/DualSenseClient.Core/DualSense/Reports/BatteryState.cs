namespace DualSenseClient.Core.DualSense.Reports;

/// <summary>
/// DualSense battery status
/// </summary>
public class BatteryState
{
    /// <summary>
    /// Check to see if the battery is charging
    /// </summary>
    public bool IsCharging;

    /// <summary>
    /// Check if DualSense is done charging
    /// </summary>
    public bool IsFullyCharged;

    /// <summary>
    /// Level of battery
    /// </summary>
    public float BatteryLevel;
}