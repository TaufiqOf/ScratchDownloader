using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using ScratchDownloader.Models;

namespace ScratchDownloader.Converter;

public class DayIsCheckedConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // values[0] -> Queue.DaysOfWeek (List<QueueDaysOfWeek>)
        // values[1] -> Current day (QueueDaysOfWeek)
        if (values.Count >= 2 &&
            values[0] is IEnumerable<QueueDaysOfWeek> daysList &&
            values[1] is QueueDaysOfWeek currentDay)
            return daysList.Contains(currentDay);

        return false;
    }
}