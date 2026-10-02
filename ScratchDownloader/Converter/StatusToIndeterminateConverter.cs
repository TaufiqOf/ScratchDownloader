using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ScratchDownloader.Models;

namespace ScratchDownloader.Converter;

public class StatusToIndeterminateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is DownloadStatus status)
        {
            return status == DownloadStatus.Initializing || status == DownloadStatus.CheckingChecksum;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}