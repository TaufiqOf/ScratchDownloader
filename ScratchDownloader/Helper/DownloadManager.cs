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
using ScratchDownloader.Localization;
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
        SettingsService.Settings.Categories[downloadItemInformationViewModel.Category.Id].ApplicationCount++;
        SettingsService.Settings.Queues[ downloadItemInformationViewModel.Queue.Id].ApplicationCount++;

        UpdateStatus();
        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
        ItemUpdated?.Invoke();
    }

    private void DownloadItemViewModelOnStatusChanged(object? sender, DownloadStatus e)
    {
        var downloadItemViewModel = sender as DownloadItemViewModel;
        if (downloadItemViewModel == null) return;
        SetCapToQueueItems(downloadItemViewModel);
        if (e == DownloadStatus.Completed)
            NotificationManager.Success(Strings.Get("NotificationDownloadComplete"),
                Strings.Format("NotificationDownloadCompleteMessage",
                    downloadItemViewModel.DownloadItemInformation?.FileName ?? string.Empty));
        if (e == DownloadStatus.Failed)
            NotificationManager.Error(Strings.Get("NotificationDownloadFailed"),
                Strings.Format("NotificationDownloadFailedMessage",
                    downloadItemViewModel.DownloadItemInformation?.FileName ?? string.Empty));
        if (e == DownloadStatus.ChecksumFailed)
        {
            NotificationManager.Error(Strings.Get("NotificationChecksumFailed"),
                Strings.Format("NotificationChecksumFailedMessage",
                    downloadItemViewModel.DownloadItemInformation?.FileName ?? string.Empty));
        }

        downloadItemViewModel.Status = e;
        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
        UpdateStatus();
    }

    private void SetCapToQueueItems(DownloadItemViewModel downloadItemViewModel)
    {
        var currentQueue = downloadItemViewModel.DownloadItemInformation?.Queue;
        if (currentQueue == null) return;
        var activeDownloads = Downloads.Where(d =>
                d.Status == DownloadStatus.Downloading ||
                d.Status == DownloadStatus.Initializing)
            .ToList();
        var capSpeed = 0d;
        if (currentQueue.MaxSpeedLimitInKiloBytes >= 0)
        {
            capSpeed = currentQueue.MaxSpeedLimitInKiloBytes / activeDownloads.Count;
        }

        activeDownloads.ForEach(d => d.CapSpeedInKBps = capSpeed);
        var inActiveDownloads = Downloads.Where(d =>
                !(d.Status == DownloadStatus.Downloading ||
                  d.Status == DownloadStatus.Initializing))
            .ToList();
        inActiveDownloads.ForEach(d => d.CapSpeedInKBps = 0);
    }

    private void UpdateStatus()
    {
        DownloadSummary = $"{Strings.Format("DownloadsCount", Downloads.Count)} " +
                          $"| {Strings.Format("PausedCount", Downloads.Count(d => d.Status == DownloadStatus.Paused))} " +
                          $"| {Strings.Format("CompletedCount", Downloads.Count(d => d.Status == DownloadStatus.Completed))} " +
                          $"| {Strings.Format("FailedCount", Downloads.Count(d => d.Status == DownloadStatus.Failed))}";

        ActiveCount =
            Strings.Format("ActiveCount",
                Downloads.Count(d => d.Status == DownloadStatus.Downloading || d.Status == DownloadStatus.Initializing));
        TotalSpeed =
            $"{Strings.Get("TotalSpeed")} {ByteSize.FromBytes(Downloads.Sum(d => d.Progress.BytesPerSecond))
                .Humanize("0.00")}/s";
        TotalDownloadedSize =
            $"{Strings.Get("TotalSize")} {ByteSize.FromBytes(Downloads.Sum(d => d.DownloadItemInformation?.FileSizeBytes ?? 0))
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
        SettingsService.Settings.Categories[downloadItemViewModel.DownloadItemInformation.Category.Id].ApplicationCount--;
        SettingsService.Settings.Queues[downloadItemViewModel.DownloadItemInformation.Queue.Id].ApplicationCount--;
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
                if (SettingsService.Settings.OpenWidgetEnabled) download.ShowWidget();
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
            SettingsService.Settings.Categories[download.DownloadItemInformation.Category.Id].ApplicationCount--;
            SettingsService.Settings.Queues[download.DownloadItemInformation.Queue.Id].ApplicationCount--;
            Downloads.Remove(download);
        }

        ItemUpdated?.Invoke();

        SettingsService.HistorySettings.DownloadItems = Downloads.ToList();
        SettingsService.Save();
    }
}