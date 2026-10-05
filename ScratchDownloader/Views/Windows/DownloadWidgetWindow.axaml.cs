using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using LiveChartsCore.SkiaSharpView.Painting;
using ScratchDownloader.Models;
using ScratchDownloader.Services;
using SkiaSharp;

namespace ScratchDownloader.Views.Windows;

public partial class DownloadWidgetWindow : Window
{
    private readonly List<string> _csvList = new();
    private int _currentCsvIndex = -1;
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
        var csvdata0 = GetData($"avares://ScratchDownloader/Assets/Ani1.csv");
        SnakeBackground.CsvPaths= new List<string>();

        AddCsvToLoop(GetData($"avares://ScratchDownloader/Assets/Ani1.csv"));
        AddCsvToLoop(GetData($"avares://ScratchDownloader/Assets/Ani2.csv"));
        AddCsvToLoop(GetData($"avares://ScratchDownloader/Assets/Ani3.csv"));
        AddCsvToLoop(GetData($"avares://ScratchDownloader/Assets/Ani4.csv"));
        UpdatePlayPauseButton();
    }

    private static string GetData(string csv)
    {
        using var stream0 = AssetLoader.Open(new Uri(csv));
        using var reader0 = new StreamReader(stream0);
        var csvdata0 = reader0.ReadToEnd();
        return csvdata0;
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
            else if (_downloadItemViewModel.Status == DownloadStatus.Downloading ||  _downloadItemViewModel.Status == DownloadStatus.Initializing)
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

    public void AddCsvToLoop(string csvData)
    {
        SnakeBackground.CsvPaths.Add(csvData);

    }
    
   
}