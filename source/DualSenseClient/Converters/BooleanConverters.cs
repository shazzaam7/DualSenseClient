using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DualSenseClient.Converters;

public class ObjectToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNotNull = value != null;

        // Check if the parameter is "Inverted" to return the opposite
        if (parameter?.ToString() == "Inverted")
        {
            return !isNotNull;
        }

        return isNotNull;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}