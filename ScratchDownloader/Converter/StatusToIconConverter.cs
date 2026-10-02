using System;
using System.Globalization;
using Avalonia.Data.Converters;
using FluentIcons.Common;
using ScratchDownloader.Models;

namespace ScratchDownloader.Converter;

public class StatusToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DownloadStatus status)
            return status switch
            {
                DownloadStatus.Downloading or DownloadStatus.Initializing => Icon.ArrowCircleDown,
                DownloadStatus.Completed => Icon.CheckmarkCircle,
                DownloadStatus.Failed => Icon.ErrorCircle,
                DownloadStatus.Paused => Icon.PauseCircle,
                DownloadStatus.Stopped => Icon.Stop,
                DownloadStatus.CheckingChecksum => Icon.MatchAppLayout,
                DownloadStatus.ChecksumFailed => Icon.Warning,
                _ => Icon.Document
            };
        return Icon.Document;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}