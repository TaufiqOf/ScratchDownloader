using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using ScratchDownloader.Models;
using ScratchDownloader.Services;
using Timer = System.Timers.Timer;

namespace ScratchDownloader.Helper;

public partial class DownloadManager : ViewModelBase
{
    private readonly Timer _timer;

    public DownloadManager()
    {
        _timer = new Timer(700);
        _timer.Elapsed += TimerOnElapsed;
        _timer.Start();
    }

    public ObservableCollection<DownloadItemViewModel> Downloads { get; } = new();

    [ObservableProperty] public partial string DownloadSummary { get; set; }

    [ObservableProperty] public partial string ActiveCount { get; set; }

    [ObservableProperty] public partial string TotalSpeed { get; set; }

    [ObservableProperty] public partial string TotalDownloadedSize { get; set; }

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _timer.Stop();
        Application.Current?.Dispatcher.Post(UpdateStatus);
        _timer.Start();
    }

    public async Task<DownloadItemInformationViewModel> GetDownloadInfoFromUrl(string url,
        CancellationToken cancellationToken)
    {
        var newDownloadItemInformationViewModel = new DownloadItemInformationViewModel();
        await newDownloadItemInformationViewModel.GetDataFromUrl(url, cancellationToken);
        return newDownloadItemInformationViewModel;
    }

    public void Add(DownloadItemInformationViewModel downloadItemInformationViewModel)
    {
        if (downloadItemInformationViewModel == null)
            throw new ArgumentNullException(nameof(downloadItemInformationViewModel));

        var downloadItemViewModel =
            new DownloadItemViewModel(downloadItemInformationViewModel, new DirectDownloadService());
        Downloads.Add(downloadItemViewModel);
        downloadItemViewModel.Start();
        downloadItemViewModel.StatusChanged += DownloadItemViewModelOnStatusChanged;
        UpdateStatus();
    }

    private void DownloadItemViewModelOnStatusChanged(object? sender, DownloadStatus e)
    {
        if (e == DownloadStatus.Completed)
            NotificationManager.Success($"Downloaded {e}",
                $"Download {e}: {((DownloadItemViewModel)sender)?.DownloadItemInformation?.FileName}");
        if (e == DownloadStatus.Failed)
            NotificationManager.Error($"Downloaded {e}",
                $"Download {e}: {((DownloadItemViewModel)sender)?.DownloadItemInformation?.FileName}");


        UpdateStatus();
    }

    private void UpdateStatus()
    {
        DownloadSummary = $"Downloads: {Downloads.Count} " +
                          $"| Paused: {Downloads.Count(d => d.Status == DownloadStatus.Paused)} " +
                          $"| Completed: {Downloads.Count(d => d.Status == DownloadStatus.Completed)} " +
                          $"| Failed: {Downloads.Count(d => d.Status == DownloadStatus.Failed)}";

        ActiveCount = $"Active: {Downloads.Count(d => d.Status == DownloadStatus.Downloading)}";
        TotalSpeed =
            $"Total Speed: {ByteSize.FromBytes(Downloads.Sum(d => d.Progress.BytesPerSecond))
                .Humanize("0.00")}/s";
        TotalDownloadedSize =
            $"Total Size: {ByteSize.FromBytes(Downloads.Sum(d => d.DownloadItemInformation?.FileSizeBytes ?? 0))
                .Humanize("0.00")}";
    }

    public void Resume(DownloadItemViewModel download)
    {
        download.Resume();
    }

    public void Pause(DownloadItemViewModel download)
    {
        download.Pause();
    }

    public void Stop(DownloadItemViewModel download)
    {
        download.Stop();
    }

    public void Start(DownloadItemViewModel download)
    {
        download.Start();
    }
}