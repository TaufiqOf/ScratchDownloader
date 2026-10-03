using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore.SkiaSharpView.Painting;
using ScratchDownloader.Helper;
using ScratchDownloader.Services;
using ScratchDownloader.Views.Windows;

namespace ScratchDownloader.Models;

public partial class DownloadItemViewModel : ObservableObject
{
    private CancellationTokenSource _cancellationTokenSource = new();
    private ICheckSumService _checkSumService;
    private IDownloadService _downloadService;
    private DownloadWidgetWindow? _widgetWindow;
    
    private const int MaxSpeedSamples = 60;

    private readonly DispatcherTimer _speedChartTimer = new()
    {
        Interval = TimeSpan.FromSeconds(1)
    };
    
    [JsonIgnore]
    public SolidColorPaint SpeedChartStroke { get; set; }

    [JsonIgnore]
    public SolidColorPaint SpeedChartFill { get; set; }
 
    [JsonIgnore]
    public Func<double, string> SpeedAxisLabeler { get; } = value => $"{value:0.#} MB/s";
    
    [JsonIgnore]
    public ObservableCollection<double> SpeedHistory { get; } = new();
    
    public DownloadItemViewModel()
    {
        
    }

    private void SpeedChartTimerOnTick(object? sender, EventArgs e)
    {
        var speed = Progress.BytesPerSecond / (1024d * 1024d);

        SpeedHistory.Add(speed);

        while (SpeedHistory.Count > MaxSpeedSamples)
            SpeedHistory.RemoveAt(0);
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
        _speedChartTimer.Tick += SpeedChartTimerOnTick;
    }

  
    public bool CanOpen =>
        Status == DownloadStatus.Completed &&
        !string.IsNullOrWhiteSpace(DownloadItemInformation?.SavePath) &&
        File.Exists(DownloadItemInformation.SavePath);
    
    public bool CanShowWidget => true ||
        _widgetWindow  is null &&
        (Status == DownloadStatus.Downloading ||
         Status == DownloadStatus.Initializing ||
         Status == DownloadStatus.CheckingChecksum);
    
    [ObservableProperty] public partial DateTime? AddedDateTime { get; set; }
    [ObservableProperty] public partial string AddedDateTimeText { get; set; }
    [ObservableProperty] public partial DownloadStatus Status { get; set; } = DownloadStatus.Queued;
    [ObservableProperty] public partial DownloadProgress Progress { get; set; } = new();
    [ObservableProperty] public partial DownloadItemInformationViewModel? DownloadItemInformation { get; set; }
    [ObservableProperty] private bool _isTopMost;

    public event EventHandler<DownloadStatus>? StatusChanged;

    partial void OnAddedDateTimeChanged(DateTime? value)
    {
        if (value is null)
        {
            AddedDateTimeText = string.Empty;
            return;
        }

        var date = value.Value;
        var now = DateTime.UtcNow;
        var localDate = date.ToLocalTime();

        var age = now - date.ToUniversalTime();

        if (age < TimeSpan.FromMinutes(1))
        {
            AddedDateTimeText = "Now";
        }
        else if (age < TimeSpan.FromHours(1))
        {
            // Humanizer: "10 minutes ago" -> "10m ago"
            AddedDateTimeText = $"{Math.Max(1, (int)age.TotalMinutes)}m ago";
        }
        else if (age < TimeSpan.FromHours(24))
        {
            AddedDateTimeText = $"{(int)age.TotalHours}h ago";
        }
        else if (localDate.Date == DateTime.Now.Date.AddDays(-1))
        {
            AddedDateTimeText = $"Yesterday {localDate:HH:mm}";
        }
        else if (age < TimeSpan.FromDays(7))
        {
            AddedDateTimeText = $"{(int)age.TotalDays}d ago";
        }
        else
        {
            AddedDateTimeText = localDate.Year == DateTime.Now.Year
                ? localDate.ToString("MMM d HH:mm")
                : localDate.ToString("MMM d, yyyy");
        }
    }

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

        if (!_speedChartTimer.IsEnabled)
            _speedChartTimer.Start();
    }

    private void DownloadServiceOnInitializing(object? sender, EventArgs e)
    {
        StatusChanged?.Invoke(this, DownloadStatus.Initializing);
        Status = DownloadStatus.Initializing;
    }

    private async void DownloadServiceOnCompleted(object? sender, EventArgs e)
    {
        Progress.BytesPerSecond = 0;
        _speedChartTimer.Stop();
        if (DownloadItemInformation?.SavePath == null)
            return;
        if (!string.IsNullOrEmpty(DownloadItemInformation?.Checksum))
        {
            Status = DownloadStatus.CheckingChecksum;
            StatusChanged?.Invoke(this, DownloadStatus.CheckingChecksum);
            var match = await _checkSumService.Check(DownloadItemInformation.SavePath,
                DownloadItemInformation.Checksum);
            if (!match)
            {
                Status = DownloadStatus.ChecksumFailed;
                StatusChanged?.Invoke(this, DownloadStatus.ChecksumFailed);
                return;
            }
        }

        Status = DownloadStatus.Completed;
        StatusChanged?.Invoke(this, DownloadStatus.Completed);
    }

    [RelayCommand]
    public void ShowWidget()
    {
        if (_widgetWindow != null && _widgetWindow.IsVisible)
        {
            _widgetWindow.Activate();
            _widgetWindow.Topmost = true;
            _widgetWindow.Topmost = false;
            return;
        }

        _widgetWindow = WidgetManager.ShowWidget(this);
        _widgetWindow?.Closed += (s, e) => _widgetWindow = null;
    }

    [RelayCommand]
    public void Resume()
    {
        if (Status == DownloadStatus.Paused || Status == DownloadStatus.Queued)
        {
            Status = DownloadStatus.Downloading;
            _downloadService.Resume(_cancellationTokenSource.Token);
            if (!_speedChartTimer.IsEnabled)
                _speedChartTimer.Start();
        }
    }

    [RelayCommand]
    public void Stop()
    {
        if (Status == DownloadStatus.Completed
            || Status == DownloadStatus.Stopped
            || Status == DownloadStatus.Failed)
            return;

        Progress.BytesPerSecond = 0;
        _downloadService.Stop();
        Status = DownloadStatus.Stopped;

        _speedChartTimer.Stop();

        _widgetWindow?.Close();
    }

    [RelayCommand]
    public void Pause()
    {
        if (Status == DownloadStatus.Downloading)
        {
            _downloadService.Pause();
            Progress.BytesPerSecond = 0;
            Status = DownloadStatus.Paused;
            StatusChanged?.Invoke(this, DownloadStatus.Paused);
            _speedChartTimer.Stop();
        }
    }

    [RelayCommand]
    public void Start()
    {
        _downloadService.Start(_cancellationTokenSource.Token);
    }

    [RelayCommand]
    private void Remove()
    {
        ApplicationManager.DownloadManager.Remove(this);
        _widgetWindow?.Close();
    }
    
    [RelayCommand]
    private void DeleteFile()
    {
        DialogManager.ShowMessage( MessageDialogType.Warning,
            "Delete File", 
            "Are you sure you want to delete the downloaded file?", 
            "Yes", 
            new RelayCommand(() =>
        {
            ApplicationManager.DownloadManager.Remove(this);  
            File.Delete(DownloadItemInformation?.SavePath ?? string.Empty);
            File.Delete(DownloadItemInformation?.SavePath + ".meta.json" ?? string.Empty);
  
            _widgetWindow?.Close();
        }), "No");
    }
    
    [RelayCommand]
    private void Restart()
    {
        Status = DownloadStatus.Queued;
        File.Delete(DownloadItemInformation?.SavePath ?? string.Empty);
        File.Delete(DownloadItemInformation?.SavePath + ".meta.json" ?? string.Empty);
        Start();
    }

    [RelayCommand]
    private void CopyUrl()
    {
        ApplicationManager.GetClipboard()?.SetTextAsync(DownloadItemInformation?.Uri.ToString() ?? string.Empty);
    }

    [RelayCommand]
    private void Open()
    {
        var path = DownloadItemInformation?.SavePath;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to open file: {e}");
        }
    }

    [RelayCommand]
    private void OpenContainingFolder()
    {
        var path = DownloadItemInformation?.SavePath;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{Path.GetDirectoryName(path)}\"",
                    UseShellExecute = false
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"\"{Path.GetDirectoryName(path)}\"",
                    UseShellExecute = false
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to open containing folder: {e}");
        }
    }

    [RelayCommand]
    private void ShowProgress()
    {
        // Show progress
    }

    [RelayCommand]
    private void ShowProperties()
    {
        // Show properties
    }
}