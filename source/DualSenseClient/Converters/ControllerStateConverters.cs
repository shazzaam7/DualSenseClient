using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.Logging;

namespace DualSenseClient.Converters;

public class StickToCanvasConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            Logger.Trace<StickToCanvasConverter>("null value or parameter, returning default");
            return 34.0;
        }

        double normalizedValue;

        // Convert byte (0-255) to normalized range (-1.0 to 1.0)
        if (value is byte byteValue)
        {
            normalizedValue = (byteValue - 128) / 128.0;
        }
        else
        {
            Logger.Warning<StickToCanvasConverter>($"unexpected value type {value.GetType()}");
            return 34.0;
        }

        if (parameter is string sizeStr && double.TryParse(sizeStr, out double size))
        {
            // Convert -1.0 to 1.0 range to canvas position (0 to size)
            // Subtract half the indicator size (6) to center it
            double position = ((normalizedValue + 1.0) / 2.0) * size - 6;
            double clamped = Math.Clamp(position, 0, size - 12);
            Logger.Trace<StickToCanvasConverter>($"byte={byteValue}, normalized={normalizedValue:F2}, position={clamped:F1}");
            return clamped;
        }

        Logger.Warning<StickToCanvasConverter>($"invalid parameter format '{parameter}'");
        return 34.0; // Default center position (40 - 6)
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TouchToCanvasConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            Logger.Trace<TouchToCanvasConverter>("null value or parameter, returning 0");
            return 0.0;
        }

        if (value is not ushort touchValue)
        {
            Logger.Warning<TouchToCanvasConverter>($"unexpected value type {value.GetType()}");
            return 0.0;
        }

        if (parameter is not string paramStr)
        {
            Logger.Warning<TouchToCanvasConverter>("parameter is not string");
            return 0.0;
        }

        // Format -> canvasSize;maxTouchValue
        string[] parts = paramStr.Split(';');
        if (parts.Length != 2)
        {
            Logger.Warning<TouchToCanvasConverter>($"invalid parameter format '{paramStr}'");
            return 0.0;
        }

        if (!double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double canvasSize))
        {
            Logger.Warning<TouchToCanvasConverter>($"cannot parse canvas size '{parts[0]}'");
            return 0.0;
        }

        if (!double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double maxValue))
        {
            Logger.Warning<TouchToCanvasConverter>($"cannot parse max value '{parts[1]}'");
            return 0.0;
        }

        // Touch Coordinate -> Canvas Position (Subtracting half of the indicator size)
        double position = (touchValue / maxValue) * canvasSize - 8;
        double clamped = Math.Clamp(position, 0, canvasSize - 16);
        Logger.Trace<TouchToCanvasConverter>($"touch={touchValue}, canvas={canvasSize}, position={clamped:F1}");

        return clamped;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ByteToPercentageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Convert byte (0-255) to percentage (0-100)
        if (value is not byte byteValue)
        {
            Logger.Trace<ByteToPercentageConverter>("unexpected value type, returning 0");
            return 0.0;
        }
        double percentage = (byteValue / 255.0) * 100.0;
        Logger.Trace<ByteToPercentageConverter>($"{byteValue} -> {percentage:F1}%");
        return percentage;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Convert percentage (0-100) back to byte (0-255)
        if (value is not double percentage)
        {
            Logger.Trace<ByteToPercentageConverter>("ConvertBack: unexpected value type, returning 0");
            return (byte)0;
        }
        byte byteValue = (byte)Math.Clamp((percentage / 100.0) * 255.0, 0, 255);
        Logger.Trace<ByteToPercentageConverter>($"ConvertBack: {percentage:F1}% -> {byteValue}");
        return byteValue;
    }
}

public class EnumToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            Logger.Trace<EnumToBooleanConverter>("null value or parameter");
            return false;
        }

        // Check if the current enum value matches the parameter
        string parameterString = parameter.ToString()!;
        bool matches = value.ToString() == parameterString;
        Logger.Trace<EnumToBooleanConverter>($"{value} == {parameterString} = {matches}");
        return matches;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            Logger.Trace<EnumToBooleanConverter>("ConvertBack: null value or parameter");
            return null;
        }

        // Only convert back if the radio button is checked
        if (value is bool isChecked && isChecked && parameter is string parameterString)
        {
            Logger.Trace<EnumToBooleanConverter>($"ConvertBack: parsing {parameterString} to {targetType.Name}");
            return Enum.Parse(targetType, parameterString);
        }

        return false;
    }
}

public class BoolToButtonBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPressed && isPressed)
        {
            Logger.Trace<BoolToButtonBackgroundConverter>("button pressed, returning accent color");
            return new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Accent color when pressed
        }
        Logger.Trace<BoolToButtonBackgroundConverter>("button not pressed, returning gray");
        return new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)); // Subtle gray when not pressed
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LightbarColor lightbarColor)
        {
            Logger.Trace<ColorToBrushConverter>($"RGB({lightbarColor.Red}, {lightbarColor.Green}, {lightbarColor.Blue})");
            return new SolidColorBrush(Color.FromRgb(
                lightbarColor.Red,
                lightbarColor.Green,
                lightbarColor.Blue
            ));
        }

        Logger.Warning<ColorToBrushConverter>("unexpected value type, returning gray");
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for individual Player LED indicators
/// Checks if a specific LED flag is set in the PlayerLed enum
/// </summary>
public class PlayerLedFlagToBrushConverter : IValueConverter
{
    private static readonly IBrush ActiveLedBrush = new SolidColorBrush(Colors.White);
    private static readonly IBrush InactiveLedBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PlayerLed playerLeds || parameter is not string ledFlag)
        {
            return InactiveLedBrush;
        }

        PlayerLed flag = ledFlag switch
        {
            "1" or "LED_1" => PlayerLed.LED_1,
            "2" or "LED_2" => PlayerLed.LED_2,
            "3" or "LED_3" => PlayerLed.LED_3,
            "4" or "LED_4" => PlayerLed.LED_4,
            "5" or "LED_5" => PlayerLed.LED_5,
            _ => PlayerLed.None
        };

        return playerLeds.HasFlag(flag) ? ActiveLedBrush : InactiveLedBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for Mic LED state to color
/// </summary>
public class MicLedToBrushConverter : IValueConverter
{
    private static readonly IBrush MicOnBrush = new SolidColorBrush(Color.FromRgb(255, 140, 0)); // Orange
    private static readonly IBrush MicPulseBrush = new SolidColorBrush(Color.FromRgb(255, 69, 0)); // Red-Orange
    private static readonly IBrush MicOffBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)); // Dim white

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MicLed micLed)
        {
            return MicOffBrush;
        }

        return micLed switch
        {
            MicLed.On => MicOnBrush,
            MicLed.Pulse => MicPulseBrush,
            MicLed.Off => MicOffBrush,
            _ => MicOffBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for PlayerLed enum to readable string
/// </summary>
public class PlayerLedToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PlayerLed playerLeds)
        {
            return "None";
        }

        if (playerLeds == PlayerLed.None)
        {
            return "None";
        }

        if (playerLeds == PlayerLed.All)
        {
            return "All (5)";
        }

        List<string> activeLeds = new List<string>();

        if (playerLeds.HasFlag(PlayerLed.LED_1)) activeLeds.Add("1");
        if (playerLeds.HasFlag(PlayerLed.LED_2)) activeLeds.Add("2");
        if (playerLeds.HasFlag(PlayerLed.LED_3)) activeLeds.Add("3");
        if (playerLeds.HasFlag(PlayerLed.LED_4)) activeLeds.Add("4");
        if (playerLeds.HasFlag(PlayerLed.LED_5)) activeLeds.Add("5");

        return activeLeds.Count > 0 ? string.Join(", ", activeLeds) : "None";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for LightbarColor struct to Brush
/// </summary>
public class LightbarColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LightbarColor lightbarColor)
        {
            return new SolidColorBrush(Colors.Black);
        }

        Color color = Color.FromRgb(lightbarColor.Red, lightbarColor.Green, lightbarColor.Blue);
        return new SolidColorBrush(color);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for PlayerLedBrightness enum to opacity value
/// </summary>
public class PlayerLedBrightnessToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PlayerLedBrightness brightness)
        {
            return 1.0;
        }

        return brightness switch
        {
            PlayerLedBrightness.High => 1.0,
            PlayerLedBrightness.Medium => 0.66,
            PlayerLedBrightness.Low => 0.33,
            _ => 1.0
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for boolean to color (for touch active state)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    private static readonly IBrush ActiveBrush = new SolidColorBrush(Color.FromRgb(0, 217, 163)); // Green
    private static readonly IBrush InactiveBrush = new SolidColorBrush(Color.FromRgb(150, 150, 150)); // Gray

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? ActiveBrush : InactiveBrush;
        }
        return InactiveBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}