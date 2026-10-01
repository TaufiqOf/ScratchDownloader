using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using ScratchDownloader.Models;
using ScratchDownloader.ViewModels.PageViewModel;

namespace ScratchDownloader.Converter;

public class QueueDayToggleArgsConverter : IMultiValueConverter
{
    public object? Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (values.Count < 2)
            return null;

        if (values[0] is not Queue queue)
            return null;

        if (values[1] is not QueueDaysOfWeek day)
            return null;

        return new QueueDayToggleArgs(queue, day);
    }
}