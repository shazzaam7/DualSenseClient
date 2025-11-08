using CommunityToolkit.Mvvm.ComponentModel;

namespace DualSenseClient.Core.DualSense.Enums;

/// <summary>
/// Color of the DualSense Lightbar.
/// </summary>
public partial class LightbarColor : ObservableObject
{
    // Properties
    /// <summary>
    /// Red component of the color (0-255)
    /// </summary>
    [ObservableProperty] private byte _red;

    /// <summary>
    /// Green component of the color (0-255)
    /// </summary>
    [ObservableProperty] private byte _green;

    /// <summary>
    /// Blue component of the color (0-255)
    /// </summary>
    [ObservableProperty] private byte _blue;

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