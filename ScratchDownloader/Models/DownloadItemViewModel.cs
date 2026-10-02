using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ScratchDownloader.Services;

namespace ScratchDownloader.Models;

public partial class DownloadItemViewModel : ObservableObject
{
    private CancellationTokenSource _cancellationTokenSource = new();
    private ICheckSumService _checkSumService;

    private IDownloadService _downloadService;
    private readonly Timer _timer;

    public DownloadItemViewModel()
    {

    }

    public void Initialize(
        DownloadItemInformationViewModel downloadItemInformationViewModel,
        IDownloadService downloadService)
    {
        DownloadItemInformation = downloadItemInformationViewModel;
        _cancellationTokenSource = new CancellationTokenSource();
        _downloadService = downloadService;
        _downloadService.Initializing += DownloadServiceOnInitializing;
        _downloadService.Downloading += DownloadServiceOnDownloading;
        _downloadService.Completed += DownloadServiceOnCompleted;
        _downloadService.ErrorOccurred += DownloadServiceOnErrorOccurred;
        _downloadService.SegmentCount = downloadItemInformationViewModel.Segments;
        _downloadService.Uri = downloadItemInformationViewModel.Uri;
        _downloadService.DestinationFilePath = downloadItemInformationViewModel.SavePath;
        _downloadService.Progress = Progress;
        Progress?.TotalBytes = downloadItemInformationViewModel.FileSizeBytes;
        _checkSumService = new CheckSumService();
    }

    [ObservableProperty] public partial DownloadStatus Status { get; set; } = DownloadStatus.Queued;
    [ObservableProperty] public partial DownloadProgress Progress { get; set; } = new();
    [ObservableProperty] public partial DownloadItemInformationViewModel? DownloadItemInformation { get; set; }
    public event EventHandler<DownloadStatus>? StatusChanged;

    private void DownloadServiceOnErrorOccurred(object? sender, string e)
    {
        Progress.BytesPerSecond = 0;
        StatusChanged?.Invoke(this, DownloadStatus.Failed);
        Status = DownloadStatus.Failed;
    }

    private void DownloadServiceOnDownloading(object? sender, EventArgs e)
    {
        StatusChanged?.Invoke(this, DownloadStatus.Downloading);
        Status = DownloadStatus.Downloading;
    }

    private void DownloadServiceOnInitializing(object? sender, EventArgs e)
    {
        StatusChanged?.Invoke(this, DownloadStatus.Initializing);
        Status = DownloadStatus.Initializing;
    }

    private async void DownloadServiceOnCompleted(object? sender, EventArgs e)
    {
        Progress.BytesPerSecond = 0;
        if(DownloadItemInformation?.SavePath ==null)
            return;
        if(!string.IsNullOrEmpty(DownloadItemInformation?.Checksum))
        {
            Status = DownloadStatus.CheckingChecksum;
            StatusChanged?.Invoke(this, DownloadStatus.CheckingChecksum);
            var match = await _checkSumService.Check(DownloadItemInformation.SavePath, DownloadItemInformation.Checksum);
            if(!match)
            {
                Status = DownloadStatus.ChecksumFailed;
                StatusChanged?.Invoke(this, DownloadStatus.ChecksumFailed);
                return;
            }
        }
        Status = DownloadStatus.Completed;
        StatusChanged?.Invoke(this, DownloadStatus.Completed);
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
            return;
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
        _downloadService.Start(_cancellationTokenSource.Token);
    }
}