using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using ScratchDownloader.Models;
using ScratchDownloader.Services;
using ScratchDownloader.Views.Windows;

namespace ScratchDownloader.Helper;

public static class WidgetManager
{
    private const int Margin = 20;
    private const int Spacing = 10;
    private static readonly List<DownloadWidgetWindow> _widgets = new();

    private static Window? _mainWindow;

    public static void Initialize(Window window)
    {
        _mainWindow = window;

        // Reposition when the application moves between monitors.
        window.PositionChanged += (_, _) => { RepositionNotifications(); };
    }

    public static DownloadWidgetWindow? ShowWidget(DownloadItemViewModel downloadItemViewModel)
    {
        if (_mainWindow == null)
            throw new InvalidOperationException(
                "NotificationHelper.Initialize() must be called first.");

        var downloadWidgetWindow = new DownloadWidgetWindow(downloadItemViewModel);

        _widgets.Add(downloadWidgetWindow);

        downloadWidgetWindow.Closed += (_, _) =>
        {
            _widgets.Remove(downloadWidgetWindow);
            RepositionNotifications();
        };

        downloadWidgetWindow.Opened += (_, _) => { RepositionNotifications(); };

        downloadWidgetWindow.Show();

        RepositionNotifications();
        return downloadWidgetWindow;
    }


    private static void RepositionNotifications()
    {
        if (_mainWindow == null)
            return;

        double bottom = Margin;

        // Newest notification at the bottom.
        foreach (var notification in _widgets.AsEnumerable().Reverse())
        {
            if (!notification.IsVisible)
                continue;

            notification.UpdateLayout();

            var screen = notification.Screens.ScreenFromWindow(_mainWindow);

            if (screen == null)
                continue;

            var workingArea = screen.WorkingArea;

            // Right edge
            var x = workingArea.Right
                    - notification.Bounds.Width
                    - Margin;

            // Bottom edge
            var y = workingArea.BottomRight.Y
                    - notification.Bounds.Height
                    - bottom;

            notification.Position = new PixelPoint(
                (int)x,
                (int)y);

            bottom += notification.Bounds.Height + Spacing;
        }
    }
}