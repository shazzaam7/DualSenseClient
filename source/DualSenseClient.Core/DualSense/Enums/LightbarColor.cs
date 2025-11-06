namespace DualSenseClient.Core.DualSense.Enums;

/// <summary>
/// Color of the DualSense Lightbar.
/// </summary>
public struct LightbarColor
{
    // Properties
    /// <summary>
    /// Red component of the color (0-255)
    /// </summary>
    public byte Red;

    /// <summary>
    /// Green component of the color (0-255)
    /// </summary>
    public byte Green;

    /// <summary>
    /// Blue component of the color (0-255)
    /// </summary>
    public byte Blue;

    // Constructor
    /// <summary>
    /// Creates a LightBar Color
    /// </summary>
    /// <param name="red">Red component of the color (0-255)</param>
    /// <param name="green">Green component of the color (0-255)</param>
    /// <param name="blue">Blue component of the color (0-255)</param>
    public LightbarColor(byte red, byte green, byte blue)
    {
        Red = red;
        Green = green;
        Blue = blue;
    }
}