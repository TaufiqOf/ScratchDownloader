using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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
        var initialCsvData =
            " ,,70,1,18,19,30,31,,\n,,69,2,17,20,29,32,,,,,\n,,68,3,16,21,28,33,,,,,\n,,67,4,15,22,27,34,,,,,\n,,66,5,14,23,26,35,,,,,\n,,65,6,13,24,25,36,,,,,\n,,64,7,12,11,38,37,,,,,\n61,62,63,8,9,10,39,40,41,42,,,\n60,59,,,,,,,44,43,,,\n ,58,57,,,,,46,45,,,,\n ,,56,55,,,48,47,,,,,\n ,,,54,53,50,49,,,,,,\n,,,,52,51,,,,,,,";
        var csvdata2 =
            "﻿,,,,,,,8,,,\n,,,,,,7,9,,,\n,,,,,6,10,,,,\n,,,,5,12,,,,,\n,,,4,13,,,,,,24\n,,3,,14,,,,,23,25\n,2,,,15,,,,22,,26\n1,,,,16,,,21,31,,27\n,,,,17,,20,,,30,28\n,,,,18,19,,,,,29\n,,,,,,,,,,";
        AddCsvToLoop(initialCsvData);
        AddCsvToLoop(csvdata2);
        SnakeBackground.CsvData = initialCsvData;
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
        if (string.IsNullOrWhiteSpace(csvData)) return;

        _csvList.Add(csvData);

        if (_currentCsvIndex == -1)
        {
            PlayNextInLoop();
        }
    }
    


    private void OnSnakeAnimationCompleted(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(PlayNextInLoop, DispatcherPriority.Normal);
    }

    private void PlayNextInLoop()
    {
        if (_csvList.Count == 0) return;
        _currentCsvIndex = (_currentCsvIndex + 1) % _csvList.Count;
        SnakeBackground.CsvData = _csvList[_currentCsvIndex];
    }
}