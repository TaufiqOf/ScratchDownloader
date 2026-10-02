using System;
using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;

namespace ScratchDownloader.Models;

public partial class SegmentProgress : ViewModelBase
{
    [ObservableProperty] public partial int Index { get; set; }
    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial long BytesDownloaded { get; set; }
    [ObservableProperty] public partial long TotalBytes { get; set; }
    [ObservableProperty] public partial double BytesPerSecond { get; set; } // Bytes per second
}

public partial class DownloadProgress : ViewModelBase
{
    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial double BytesPerSecond { get; set; } // Bytes per second
    [ObservableProperty] public partial long TotalBytes { get; set; }
    [ObservableProperty] public partial long BytesDownloaded { get; set; }
    [ObservableProperty] public partial string ProgressText { get; set; } = "0.00%";
    [ObservableProperty] public partial string Speed { get; set; } = "0.00 B/s";
    [ObservableProperty] public partial string FileSizeDisplay { get; set; } = "0/0 MB";
    [ObservableProperty] public partial string Eta { get; set; }

    [ObservableProperty]
    public partial ConcurrentDictionary<int, SegmentProgress> SegmentProgress { get; set; } = new();

    partial void OnProgressChanged(double value)
    {
        ProgressText = $"{value:0.00}%";
    }

    partial void OnBytesPerSecondChanged(double value)
    {
        Speed = ByteSize.FromBytes(value).Humanize("0.00") + "/s";
    }

    partial void OnTotalBytesChanged(long value)
    {
        FileSizeDisplay =
            $"{ByteSize.FromBytes(BytesDownloaded).Humanize("0.0")}/{ByteSize.FromBytes(value).Humanize("0.00")}";
    }

    partial void OnBytesDownloadedChanged(long value)
    {
        FileSizeDisplay =
            $"{ByteSize.FromBytes(value).Humanize("0.0")}/{ByteSize.FromBytes(TotalBytes).Humanize("0.00")}";
        UpdateEta();
    }

    private void UpdateEta()
    {
        if (TotalBytes <= 0 || BytesPerSecond <= 0 || Progress >= 100)
        {
            Eta = "--";
            return;
        }

        // Calculate remaining bytes based on actual percent completed
        var remainingBytes = TotalBytes * (1.0 - Progress / 100.0);

        // Compute seconds remaining safely
        var secondsRemaining = remainingBytes / BytesPerSecond;

        if (double.IsNaN(secondsRemaining) || double.IsInfinity(secondsRemaining) || secondsRemaining < 0)
        {
            Eta = "--";
            return;
        }

        Eta = TimeSpan.FromSeconds(secondsRemaining).Humanize(2);
    }
}