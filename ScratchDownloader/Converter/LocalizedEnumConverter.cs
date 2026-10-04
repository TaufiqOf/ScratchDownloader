using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ScratchDownloader.Localization;
using ScratchDownloader.Models;

namespace ScratchDownloader.Converter;

public sealed class LocalizedEnumConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            QueueDaysOfWeek.Sunday => "DaySunday",
            QueueDaysOfWeek.Monday => "DayMonday",
            QueueDaysOfWeek.Tuesday => "DayTuesday",
            QueueDaysOfWeek.Wednesday => "DayWednesday",
            QueueDaysOfWeek.Thursday => "DayThursday",
            QueueDaysOfWeek.Friday => "DayFriday",
            QueueDaysOfWeek.Saturday => "DaySaturday",
            OperationMode.Nothing => "OperationNothing",
            OperationMode.Notify => "OperationNotify",
            OperationMode.Sleep => "OperationSleep",
            OperationMode.Shutdown => "OperationShutdown",
            OperationMode.RunScript => "OperationRunScript",
            _ => null
        };

        return key is null ? value?.ToString() ?? string.Empty : Strings.Get(key);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
