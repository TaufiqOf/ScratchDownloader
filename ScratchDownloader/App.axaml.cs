using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
        if (_trayIcon == null) return;

        var rootMenu = new NativeMenu();

        // --------------------------------------------------------
        // Show
        // --------------------------------------------------------

        var showItem =
            new NativeMenuItem(Strings.Get("Show"));

        showItem.Click +=
            ShowWindow_OnClick;

        rootMenu.Items.Add(showItem);


        // --------------------------------------------------------
        // Separator
        // --------------------------------------------------------

        rootMenu.Items.Add(
            new NativeMenuItemSeparator());

        // --------------------------------------------------------
        // Exit
        // --------------------------------------------------------

        var exitItem =
            new NativeMenuItem(Strings.Get("Exit"));

        exitItem.Click +=
            Exit_OnClick;

        rootMenu.Items.Add(exitItem);

        _trayIcon.Menu = rootMenu;
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