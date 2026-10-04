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
            QueueDaysOfWeek.Sunday => Language.QueueDay.Sunday,
            QueueDaysOfWeek.Monday => Language.QueueDay.Monday,
            QueueDaysOfWeek.Tuesday => Language.QueueDay.Tuesday,
            QueueDaysOfWeek.Wednesday => Language.QueueDay.Wednesday,
            QueueDaysOfWeek.Thursday => Language.QueueDay.Thursday,
            QueueDaysOfWeek.Friday => Language.QueueDay.Friday,
            QueueDaysOfWeek.Saturday => Language.QueueDay.Saturday,
            OperationMode.Nothing => Language.OperationMode.Nothing,
            OperationMode.Notify => Language.OperationMode.Notify,
            OperationMode.Sleep => Language.OperationMode.Sleep,
            OperationMode.Shutdown => Language.OperationMode.Shutdown,
            OperationMode.RunScript => Language.OperationMode.RunScript,
            _ => null
        };

        return key is null ? value?.ToString() ?? string.Empty : Strings.Get(key);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
