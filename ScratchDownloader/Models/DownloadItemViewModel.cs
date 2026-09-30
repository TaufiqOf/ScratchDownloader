using System;
using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;

namespace ScratchDownloader.Models;

public enum DownloadStatus
{
    Queued,
    Downloading,
    Paused,
    Stopped,
    Completed,
    Failed
}

public partial class DownloadItemViewModel : ObservableObject
{
    [ObservableProperty] public partial string Url { get; set; } = string.Empty;

    [ObservableProperty] public partial DownloadStatus Status { get; set; } = DownloadStatus.Queued;

    [ObservableProperty] public partial double Progress { get; set; }

    [ObservableProperty] public partial string ProgressText { get; set; }

    [ObservableProperty] public partial string Speed { get; set; }

    [ObservableProperty] public partial double SpeedValue { get; set; }

    [ObservableProperty] public partial string Eta { get; set; } = "--";

    [ObservableProperty] public partial DownloadItemInformationViewModel? DownloadItemInformation { get; set; }

    public DownloadItemViewModel(DownloadItemInformationViewModel downloadItemInformationViewModel)
    {
        DownloadItemInformation = downloadItemInformationViewModel;
        UpdateDownloadStatus();
    }

    private void UpdateDownloadStatus()
    {
        ProgressText = $"{Progress:0.00}%";
        Speed = ByteSize.FromBytes(SpeedValue).Humanize("0.00") + "/s";
        if (DownloadItemInformation?.FileSizeBytes == null || DownloadItemInformation.FileSizeBytes == 0 ||
            SpeedValue == 0)
        {
            Eta = "--";
            return;
        }

        var totalBytes = DownloadItemInformation?.FileSizeBytes ?? 0;
        var remainingBytes = totalBytes * (100 - Progress) / 100.0;
        Eta = Progress < 100
            ? TimeSpan.FromSeconds(remainingBytes / (SpeedValue > 0 ? SpeedValue : 1)).Humanize(2)
            : "--";
    }


    public void Resume()
    {
        Status = DownloadStatus.Downloading;
    }

    public void Stop()
    {
        Status = DownloadStatus.Stopped;
    }

    public void Pause()
    {
        Status = DownloadStatus.Paused;
    }
}