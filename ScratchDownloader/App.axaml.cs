using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ScratchDownloader.Helper;
using ScratchDownloader.Services;
using ScratchDownloader.ViewModels;
using ScratchDownloader.Views;
using ScratchDownloader.Views.Windows;
using MainPageView = ScratchDownloader.Views.PageControls.MainPageView;
using MainPageViewModel = ScratchDownloader.ViewModels.PageViewModel.MainPageViewModel;

namespace ScratchDownloader;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private TrayIcon? _trayIcon;

    private bool _isExiting;
    private bool _startedFromAutostart;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ThemeManager.SetTheme(SettingsService.Settings.GetThemeVariant());
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var desktopMainWindow = new MainWindow
            {
                DataContext = new MainPageViewModel()
            };
            desktop.MainWindow = desktopMainWindow;
            _mainWindow = desktopMainWindow;
            desktop.ShutdownMode =
                ShutdownMode.OnExplicitShutdown;

            desktop.ShutdownRequested +=
                OnShutdownRequested;

            _mainWindow.Closing +=
                MainWindow_OnClosing;
            if (SettingsService.Settings.StartMinimized)
            {
                _mainWindow.Loaded +=
                    (sender, args) => { _mainWindow.Hide(); };
                
            }
            SettingsService.Settings.Categories.Values.ToList().ForEach(category =>
            {
                Console.WriteLine($" Loading QueueId: {category.QueueId} CategoryId: {category.Id} Name: {category.Name} Folder: {category.Folder} Extension: {category.Extension}");
            });

            ApplicationManager.MainWindow = desktop.MainWindow;
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
        {
            // Allow the window/application to close.
            return;
        }

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
            return;
        }

        var rootMenu = new NativeMenu();

        // --------------------------------------------------------
        // Show
        // --------------------------------------------------------

        var showItem =
            new NativeMenuItem("Show");

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
            new NativeMenuItem("Exit");

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
        if (_mainWindow == null)
        {
            return;
        }

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
        {
            desktop.Shutdown();
        }
    }

    private void TrayIconOnClicked(
        object? sender,
        EventArgs e)
    {
        ShowMainWindow();
    }
}