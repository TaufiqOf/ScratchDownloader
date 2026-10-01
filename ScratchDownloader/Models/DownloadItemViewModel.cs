using System;
using System.Data;
using System.Linq;
using System.Threading;
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
    CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    [ObservableProperty] public partial DownloadStatus Status { get; set; } = DownloadStatus.Queued;

    [ObservableProperty] public partial double Progress { get; set; } = 0;

    [ObservableProperty] public partial string ProgressText { get; set; } = "0.00%";

    [ObservableProperty] public partial string Speed { get; set; } = "0.00 B/s";

    [ObservableProperty] public partial double SpeedValue { get; set; } = 0;

    [ObservableProperty] public partial string Eta { get; set; } = "--";

    [ObservableProperty] public partial DownloadItemInformationViewModel? DownloadItemInformation { get; set; }

    private IDownloadService _downloadService;

    public DownloadItemViewModel(DownloadItemInformationViewModel downloadItemInformationViewModel,
        IDownloadService downloadService)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _downloadService = downloadService;
        _downloadService.ProgressChanged += OnProgressChanged;
        _downloadService.Completed += DownloadServiceOnCompleted;
        _downloadService.SegmentCount = downloadItemInformationViewModel.Segments;
        _downloadService.Uri = downloadItemInformationViewModel.Uri;
        _downloadService.DestinationFilePath = downloadItemInformationViewModel.SavePath;
        DownloadItemInformation = downloadItemInformationViewModel;
        UpdateDownloadStatus();
    }

    private void DownloadServiceOnCompleted(object? sender, EventArgs e)
    {
        Status = DownloadStatus.Completed;
        SpeedValue = 0;
        UpdateDownloadStatus();
    }

    private void OnProgressChanged(object? sender, DownloadProgress progress)
    {
        Progress = progress.Progress;
        SpeedValue = progress.BytesPerSecond;
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

    partial void OnProgressChanged(double value)
    {
        ProgressText = $"{value:0.00}%";
    }

    public void Resume()
    {
        if (Status == DownloadStatus.Paused || Status == DownloadStatus.Queued)
        {

            Status = DownloadStatus.Downloading;
            _downloadService.Resume(_cancellationTokenSource.Token);
        }
    }

    public void Stop()
    {
        if (Status == DownloadStatus.Completed 
            || Status == DownloadStatus.Stopped 
            || Status == DownloadStatus.Queued
            || Status == DownloadStatus.Failed)
        {
            return;
        }

        Status = DownloadStatus.Stopped;
        _downloadService.Stop();
    }

    public void Pause()
    {
        if (Status == DownloadStatus.Downloading)
        {
            Status = DownloadStatus.Paused;
            _downloadService.Pause();
        }
    }

    public void Start()
    {
        if (Status == DownloadStatus.Queued)
        {
            Status = DownloadStatus.Downloading;
            _downloadService.Start(_cancellationTokenSource.Token);
        }
    }
}