using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Timer = System.Timers.Timer;

namespace ScratchDownloader.Models;

public enum DownloadStatus
{
    Initializing,
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

    [ObservableProperty]
    public partial Dictionary<int, SegmentProgress> SegmentProgress { get; set; } =
        new Dictionary<int, SegmentProgress>();

    [ObservableProperty] public partial DownloadProgress Progress { get; set; }  = new DownloadProgress();

   

  

    [ObservableProperty] public partial DownloadItemInformationViewModel? DownloadItemInformation { get; set; }

    private IDownloadService _downloadService;
    private readonly Timer _timer;

    public DownloadItemViewModel(DownloadItemInformationViewModel downloadItemInformationViewModel,
        IDownloadService downloadService)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _downloadService = downloadService;
        _downloadService.Progress=Progress ;
        _downloadService.Initializing += DownloadServiceOnInitializing;
        _downloadService.Downloading += DownloadServiceOnDownloading;
        _downloadService.Completed += DownloadServiceOnCompleted;
        _downloadService.SegmentCount = downloadItemInformationViewModel.Segments;
        _downloadService.Uri = downloadItemInformationViewModel.Uri;
        _downloadService.DestinationFilePath = downloadItemInformationViewModel.SavePath;
        DownloadItemInformation = downloadItemInformationViewModel;
    }

    private void DownloadServiceOnDownloading(object? sender, EventArgs e)
    {
        Status = DownloadStatus.Downloading;
    }

    private void DownloadServiceOnInitializing(object? sender, EventArgs e)
    {
        Status = DownloadStatus.Initializing;
    }

    private void DownloadServiceOnCompleted(object? sender, EventArgs e)
    {
        Progress.BytesPerSecond = 0;
        Status = DownloadStatus.Completed;
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
        Progress.BytesPerSecond = 0;
        _downloadService.Stop();
        Status = DownloadStatus.Stopped;
    }

    public void Pause()
    {
        if (Status == DownloadStatus.Downloading)
        {
            _downloadService.Pause();
            Progress.BytesPerSecond = 0;
            Status = DownloadStatus.Paused;
        }
    }

    public void Start()
    {
        if (Status == DownloadStatus.Queued)
        {
            _downloadService.Start(_cancellationTokenSource.Token);
        }
    }
}