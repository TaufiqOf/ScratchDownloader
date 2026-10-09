using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ScratchDownloader.Models;

namespace ScratchDownloader.Converter;

public class StatusToColorConverter:IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is not DownloadStatus status) return null;
        if (status == DownloadStatus.Failed || status == DownloadStatus.ChecksumFailed || status == DownloadStatus.Stopped)
        {
            return Avalonia.Media.Brushes.IndianRed;
        }
        if (status == DownloadStatus.Completed)
        {
            return Avalonia.Media.Brushes.LimeGreen;
        }
        return Avalonia.Media.Brushes.DodgerBlue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}