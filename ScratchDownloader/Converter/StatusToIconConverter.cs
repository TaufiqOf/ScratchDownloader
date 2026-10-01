using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ScratchDownloader.Models;

namespace ScratchDownloader.Converter;

public class StatusToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is DownloadStatus status)
        {
            return status switch
            {
                DownloadStatus.Downloading or DownloadStatus.Initializing => FluentIcons.Common.Icon.ArrowDownload,
                DownloadStatus.Completed => FluentIcons.Common.Icon.CheckmarkCircle,
                DownloadStatus.Failed => FluentIcons.Common.Icon.ErrorCircle,
                DownloadStatus.Paused => FluentIcons.Common.Icon.PauseCircle,
                DownloadStatus.Stopped => FluentIcons.Common.Icon.Stop,
                _ => FluentIcons.Common.Icon.Document
            };
        }
        return FluentIcons.Common.Icon.Document;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}