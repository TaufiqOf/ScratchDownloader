using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using LiveChartsCore.SkiaSharpView.Painting;
using ScratchDownloader.Models;
using ScratchDownloader.Services;
using SkiaSharp;

namespace ScratchDownloader.Views.Windows;

public partial class DownloadWidgetWindow : Window
{
    private System.Timers.Timer _timer = new();
    private readonly DownloadItemViewModel _downloadItemViewModel;

    public DownloadWidgetWindow(DownloadItemViewModel downloadItemViewModel)
    {
        this.DataContext = downloadItemViewModel;
        _downloadItemViewModel = downloadItemViewModel;
        var accent = (Color)this.FindResource("SystemAccentColor")!;

        _downloadItemViewModel.SpeedChartStroke = new SolidColorPaint(
            new SKColor(accent.R, accent.G, accent.B, accent.A))
        {
            StrokeThickness = 2
        };

        _downloadItemViewModel.SpeedChartFill = new SolidColorPaint(
            new SKColor(accent.R, accent.G, accent.B, 50));
        _downloadItemViewModel.StatusChanged += (s, e) =>
        {
            UpdatePlayPauseButton();
            AutoCloseIfCompleted();
        };
        _timer.Elapsed += (s, e) =>
            Application.Current?.Dispatcher.Post(() => Close());

        InitializeComponent();
        UpdatePlayPauseButton();
    }

    private void AutoCloseIfCompleted()
    {
        if(SettingsService.Settings.CloseWidgetEnabled &&
            (_downloadItemViewModel.Status == DownloadStatus.Completed ||
           _downloadItemViewModel.Status == DownloadStatus.ChecksumFailed ||
           _downloadItemViewModel.Status == DownloadStatus.Failed))
        {
            _timer.Interval = SettingsService.Settings.AutoCloseInterval;
            _timer.Start();
        }
    }

    private void UpdatePlayPauseButton()
    {
        Application.Current?.Dispatcher.Post(() =>
        {
            if (_downloadItemViewModel.Status == DownloadStatus.Completed ||
                _downloadItemViewModel.Status == DownloadStatus.ChecksumFailed ||
                _downloadItemViewModel.Status == DownloadStatus.Failed)
            {
                PlayPauseButtonIcon.Icon = FluentIcons.Common.Icon.ArrowClockwise;
            }
            else if (_downloadItemViewModel.Status == DownloadStatus.Downloading)
            {
                PlayPauseButtonIcon.Icon = FluentIcons.Common.Icon.Pause;
            }
            else if (_downloadItemViewModel.Status == DownloadStatus.Paused)
            {
                PlayPauseButtonIcon.Icon = FluentIcons.Common.Icon.Play;
            }
        });
       
    }


    public DownloadItemViewModel ItemViewModel => _downloadItemViewModel;

    private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        _downloadItemViewModel.OpenContainingFolderCommand.Execute(null);
    }

    private void OnPlayPauseButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_downloadItemViewModel.Status == DownloadStatus.Downloading)
        {
            _downloadItemViewModel.PauseCommand.Execute(null);
        }
        else if (_downloadItemViewModel.Status == DownloadStatus.Paused)
        {
            _downloadItemViewModel.ResumeCommand.Execute(null);
        }
        if( _downloadItemViewModel.Status == DownloadStatus.Completed ||
           _downloadItemViewModel.Status == DownloadStatus.ChecksumFailed ||
           _downloadItemViewModel.Status == DownloadStatus.Failed)
        {
            _downloadItemViewModel.RestartCommand.Execute(null);
        }
        UpdatePlayPauseButton();
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}