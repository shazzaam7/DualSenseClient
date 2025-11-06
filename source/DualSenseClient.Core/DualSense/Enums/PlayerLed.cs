namespace DualSenseClient.Core.DualSense.Enums;

/// <summary>
/// Player LED flags (going from left to right)
/// </summary>
public enum PlayerLed
{
    /// <summary>
    /// Player LED's turned off
    /// </summary>
    None = 0x00,

    /// <summary>
    /// LED 1 turned on
    /// </summary>
    LED_1 = 0x01,

    /// <summary>
    /// LED 2 turned on
    /// </summary>
    LED_2 = 0x02,

    /// <summary>
    /// LED 3 turned on
    /// </summary>
    LED_3 = 0x04,

    /// <summary>
    /// LED 4 turned on
    /// </summary>
    LED_4 = 0x08,

    /// <summary>
    /// LED 5 turned on
    /// </summary>
    LED_5 = 0x10,

    /// <summary>
    /// ALL LED's turned on
    /// </summary>
    All = LED_1 | LED_2 | LED_3 | LED_4 | LED_5
}