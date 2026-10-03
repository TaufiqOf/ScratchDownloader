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
    private bool _startAfterLoad;
    public Action? ItemUpdated { get; set; }

    public DownloadManager()
    {
        _timer = new Timer(700);
        _timer.Elapsed += TimerOnElapsed;
        _timer.Start();
    }

    public ObservableCollection<DownloadItemViewModel> Downloads { get; set; } = new();

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

    public void Add(
        DownloadItemViewModel downloadItemViewModel,
        bool startImmediately = true,
        bool startAfterLoad = false)
    {
        _startAfterLoad = startAfterLoad;
        if (downloadItemViewModel == null)
            throw new ArgumentNullException(nameof(downloadItemViewModel));
        if (downloadItemViewModel.DownloadItemInformation == null)
            throw new ArgumentNullException(nameof(downloadItemViewModel.DownloadItemInformation));
        Add(downloadItemViewModel, downloadItemViewModel.DownloadItemInformation, startImmediately);
    }

    public void Add(
        DownloadItemInformationViewModel downloadItemInformationViewModel)
    {
        if (downloadItemInformationViewModel == null)
            throw new ArgumentNullException(nameof(downloadItemInformationViewModel));
        var downloadItemViewModel =
            new DownloadItemViewModel();
        if (downloadItemInformationViewModel.StartImmediately)
            downloadItemViewModel.Status = DownloadStatus.Queued;
        Add(downloadItemViewModel, downloadItemInformationViewModel, downloadItemInformationViewModel.StartImmediately);
    }

    private void Add(DownloadItemViewModel downloadItemViewModel,
        DownloadItemInformationViewModel downloadItemInformationViewModel,
        bool startImmediately)
    {
        downloadItemViewModel.Initialize(downloadItemInformationViewModel, new DirectDownloadService());
        Downloads.Add(downloadItemViewModel);
        
        if (startImmediately)
        {
            downloadItemViewModel.Start();
            if (SettingsService.Settings.OpenWidgetEnabled) downloadItemViewModel.ShowWidget();
        }

        downloadItemViewModel.AddedDateTime ??= DateTime.UtcNow;
        downloadItemViewModel.StatusChanged += DownloadItemViewModelOnStatusChanged;
        UpdateStatus();
        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
        ItemUpdated?.Invoke();
    }


    private void DownloadItemViewModelOnStatusChanged(object? sender, DownloadStatus e)
    {
        if (e == DownloadStatus.Completed)
            NotificationManager.Success($"Downloaded {e}",
                $"Download {e}: {((DownloadItemViewModel)sender)?.DownloadItemInformation?.FileName}");
        if (e == DownloadStatus.Failed)
            NotificationManager.Error($"Downloaded {e}",
                $"Download {e}: {((DownloadItemViewModel)sender)?.DownloadItemInformation?.FileName}");
        if (e == DownloadStatus.ChecksumFailed)
        {
            NotificationManager.Error($"Downloaded Completed but Checksum Failed",
                $"Downloaded Completed but Checksum Failed: {((DownloadItemViewModel)sender)?.DownloadItemInformation?.FileName}");
        }

        ((DownloadItemViewModel)sender)?.Status = e;
        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        DownloadSummary = $"Downloads: {Downloads.Count} " +
                          $"| Paused: {Downloads.Count(d => d.Status == DownloadStatus.Paused)} " +
                          $"| Completed: {Downloads.Count(d => d.Status == DownloadStatus.Completed)} " +
                          $"| Failed: {Downloads.Count(d => d.Status == DownloadStatus.Failed)}";

        ActiveCount =
            $"Active: {Downloads.Count(d => d.Status == DownloadStatus.Downloading || d.Status == DownloadStatus.Initializing)}";
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
        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
    }


    public void Pause(DownloadItemViewModel download)
    {
        download.Pause();
        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
    }

    public void Stop(DownloadItemViewModel download)
    {
        download.Stop();
        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
    }

    public void Start(DownloadItemViewModel download)
    {
        download.Start();
        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
    }

    public void Remove(DownloadItemViewModel downloadItemViewModel)
    {
        downloadItemViewModel.Stop();
        downloadItemViewModel.StatusChanged -= DownloadItemViewModelOnStatusChanged;
        Downloads.Remove(downloadItemViewModel);

        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
        ItemUpdated?.Invoke();
    }

    public void StartPendingDownloads()
    {
        foreach (var download in Downloads)
        {
            if (_startAfterLoad && (download.Status == DownloadStatus.Downloading
                                    || download.Status == DownloadStatus.Initializing))
            {
                download.Start();
                if(SettingsService.Settings.OpenWidgetEnabled) download.ShowWidget();
            }
        }

        _startAfterLoad = false;

        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
    }

    public void Clear()
    {
        var downloadItemViewModels = Downloads.Where(d =>
            d.Status == DownloadStatus.Completed
            || d.Status == DownloadStatus.Failed
            || d.Status == DownloadStatus.ChecksumFailed
            || d.Status == DownloadStatus.Stopped).ToList();
        foreach (var download in downloadItemViewModels)
        {
            download.Stop();
            download.StatusChanged -= DownloadItemViewModelOnStatusChanged;
            Downloads.Remove(download);
        }

        ItemUpdated?.Invoke();

        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
    }
}