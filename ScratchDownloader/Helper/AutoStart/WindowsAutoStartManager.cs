using System;
using Microsoft.Win32;
using ScratchDownloader.Models;

namespace ScratchDownloader.Helper.AutoStart;

public sealed class WindowsAutoStartManager(string appName, string scratchshieldDesktop) : IAutoStartManager
{
    private string AppName = appName;

    private string RunKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";


    public bool IsEnabled()
    {
        using var key =
            Registry.CurrentUser.OpenSubKey(
                RunKey,
                writable: false);

        return key?.GetValue(AppName) != null;
    }


    public void SetEnabled(bool enable)
    {
        var exePath =
            Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(exePath))
            return;


        using var key =
            Registry.CurrentUser.OpenSubKey(
                RunKey,
                writable: true);

        if (key == null)
            return;


        if (enable)
        {
            key.SetValue(
                AppName,
                $"\"{exePath}\" --autostart");
        }
        else
        {
            key.DeleteValue(
                AppName,
                throwOnMissingValue: false);
        }
    }
}