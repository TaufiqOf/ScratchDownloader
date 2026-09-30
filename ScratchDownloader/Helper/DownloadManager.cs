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
using DownloadItemViewModel = ScratchDownloader.Models.DownloadItemViewModel;

namespace ScratchDownloader.Helper;

public partial class DownloadManager : ViewModelBase
{
    public ObservableCollection<DownloadItemViewModel> Downloads { get; } = new();
    
    [ObservableProperty] public partial string DownloadSummary { get; set; }

    [ObservableProperty] public partial string ActiveCount { get; set; }

    [ObservableProperty] public partial string TotalSpeed { get; set; }

    [ObservableProperty] public partial string TotalDownloadedSize { get; set; }

    private readonly System.Timers.Timer _timer;

    public DownloadManager()
    {
        _timer = new System.Timers.Timer(700);
        _timer.Elapsed += TimerOnElapsed;
        _timer.Start();
    }

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
        {
            throw new ArgumentNullException(nameof(downloadItemInformationViewModel));
        }

        var downloadItemViewModel = new DownloadItemViewModel(downloadItemInformationViewModel);
        Downloads.Add(downloadItemViewModel);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        DownloadSummary = $"Downloads: {Downloads.Count} | Active: {Downloads.Count(d => d.Status == DownloadStatus.Downloading)} | Paused: {Downloads.Count(d => d.Status == DownloadStatus.Paused)} | Completed: {Downloads.Count(d => d.Status == DownloadStatus.Completed)} | Failed: {Downloads.Count(d => d.Status == DownloadStatus.Failed)}";
        ActiveCount = $"Active: {Downloads.Count(d => d.Status == DownloadStatus.Downloading)}";
        TotalSpeed =
            $"Total Speed: {ByteSize.FromBytes(Downloads.Sum(d => d.SpeedValue)).Humanize("0.00")}/s";
        TotalDownloadedSize =
            $"Total Size: {ByteSize.FromBytes(Downloads.Sum(d => d.DownloadItemInformation?.FileSizeBytes ?? 0)).Humanize("0.00")}";
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
}