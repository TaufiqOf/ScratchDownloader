using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ScratchDownloader.Converter;

public class TypeMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        var targetTypeName = parameter.ToString()!;

        // Match either full type name or short class name (e.g., "HomePageViewModel")
        var valueType = value.GetType();
        return valueType.Name == targetTypeName || valueType.FullName == targetTypeName;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}