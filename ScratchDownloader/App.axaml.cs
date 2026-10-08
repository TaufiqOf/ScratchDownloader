using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using ScratchDownloader.Helper;
using ScratchDownloader.Localization;
using ScratchDownloader.Models;
using ScratchDownloader.Services;
using ScratchDownloader.Views.Windows;
using MainPageView = ScratchDownloader.Views.PageControls.MainPageView;
using MainPageViewModel = ScratchDownloader.ViewModels.PageViewModel.MainPageViewModel;

namespace ScratchDownloader;

public class App : Application
{
    private bool _isExiting;
    private MainWindow? _mainWindow;
    private bool _startedFromAutostart;
    private TrayIcon? _trayIcon;
    private NativeMenu _rootMenu;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Strings.Instance.Language = SettingsService.Settings.Language;
        ApplicationManager.LocalizeDefaults();
        ThemeManager.SetTheme(SettingsService.Settings.GetThemeVariant());
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
  
            desktop.ShutdownMode =
                ShutdownMode.OnExplicitShutdown;

            desktop.ShutdownRequested +=
                OnShutdownRequested;



            var desktopMainWindow = new MainWindow
            {
                DataContext = new MainPageViewModel()
            };
            desktop.MainWindow = desktopMainWindow;
            _mainWindow = desktopMainWindow;
            NotificationManager.Initialize(desktop.MainWindow);
            WidgetManager.Initialize(desktop.MainWindow);
            ApplicationManager.MainWindow = desktop.MainWindow;
            _mainWindow.Closing +=
                MainWindow_OnClosing;
            RebuildTrayMenu();
            Strings.Instance.PropertyChanged +=
                (_, args) =>
                {
                    if (args.PropertyName == nameof(Strings.Language)) RebuildTrayMenu();
                };
            if (SettingsService.Settings.StartMinimized)
                _mainWindow.Loaded +=
                    (sender, args) => { _mainWindow.Hide(); };
            foreach (var downloadItemViewModel in SettingsService.HistorySettings.DownloadItems)
            {
                if(downloadItemViewModel.DownloadItemInformation == null)
                    continue;
                downloadItemViewModel.DownloadItemInformation.Loading = false;
                ApplicationManager.DownloadManager.Add(downloadItemViewModel,false,true);
            }
            
            WidgetManager.OnWidgetChanged += OnWidgetChanged;  
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory =
                () => new MainPageView { DataContext = new MainPageViewModel() };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainPageView
            {
                DataContext = new MainPageViewModel()
            };
        }


        base.OnFrameworkInitializationCompleted();
    }

    private void OnWidgetChanged(List<DownloadWidgetWindow> obj)
    {
        PopulateTrayMenu(_rootMenu, obj);
    }
    


    private void OnShutdownRequested(
        object? sender,
        ShutdownRequestedEventArgs e)
    {
        // Linux Mint / desktop session is shutting down.
        //
        // The normal window Closing handler normally hides the
        // window instead of allowing it to close.
        //
        // Set this flag so Closing allows the application to
        // terminate.

        _isExiting = true;
    }

    // ============================================================
    // WINDOW CLOSE
    // ============================================================

    private void MainWindow_OnClosing(
        object? sender,
        WindowClosingEventArgs e)
    {
        // --------------------------------------------------------
        // Real application exit
        // --------------------------------------------------------

        if (_isExiting)
            // Allow the window/application to close.
            return;

        // --------------------------------------------------------
        // Normal X button
        // --------------------------------------------------------

        e.Cancel = true;

        _mainWindow?.Hide();
    }

    // ============================================================
    // TRAY MENU
    // ============================================================

    private void RebuildTrayMenu()
    {
        if (_trayIcon == null)
        {
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(
                    AssetLoader.Open(new Uri("avares://ScratchDownloader/Assets/avalonia-logo.ico"))),
                ToolTipText = "ScratchDownloader"
            };
            _trayIcon.Clicked += TrayIconOnClicked;
            TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
        }

        _rootMenu = new NativeMenu();
        _rootMenu.Opening += (_, _) => PopulateTrayMenu(_rootMenu,  new List<DownloadWidgetWindow>()); 
        PopulateTrayMenu(_rootMenu, new List<DownloadWidgetWindow>());
        _trayIcon.Menu = _rootMenu;
    }

    private void PopulateTrayMenu(NativeMenu rootMenu, List<DownloadWidgetWindow> downloadWidgetWindows)
    {
        rootMenu.Items.Clear();

        // --------------------------------------------------------
        // Show
        // --------------------------------------------------------

        var showItem =
            new NativeMenuItem(Strings.Get(Language.Tray.Show));

        showItem.Click +=
            ShowWindow_OnClick;

        rootMenu.Items.Add(showItem);


        // --------------------------------------------------------
        // Separator
        // --------------------------------------------------------

        rootMenu.Items.Add(
            new NativeMenuItemSeparator());
        
        if(downloadWidgetWindows.Count > 0)
        {
            // Add download widget windows to the tray menu
            foreach (var widget in downloadWidgetWindows)
            {
                if(widget.ItemViewModel.DownloadItemInformation?.SavedFileName == null)
                    continue;
                var widgetItem = new NativeMenuItem(widget.ItemViewModel.DownloadItemInformation.SavedFileName);
                widgetItem.Click += (_, _) => widget.Activate();
                rootMenu.Items.Add(widgetItem);
            }
        }

        // --------------------------------------------------------
        // Exit
        // --------------------------------------------------------

        var exitItem =
            new NativeMenuItem(Strings.Get(Language.Tray.Exit));

        exitItem.Click +=
            Exit_OnClick;

        rootMenu.Items.Add(exitItem);
    }

    // ============================================================
    // SHOW WINDOW
    // ============================================================

    private void ShowWindow_OnClick(
        object? sender,
        EventArgs e)
    {
        ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;

        _mainWindow.Show();

        _mainWindow.WindowState =
            WindowState.Normal;

        _mainWindow.Activate();
    }


    // ============================================================
    // EXIT
    // ============================================================

    private void Exit_OnClick(
        object? sender,
        EventArgs e)
    {
        _isExiting = true;

        if (ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void TrayIconOnClicked(
        object? sender,
        EventArgs e)
    {
        ShowMainWindow();
    }
}