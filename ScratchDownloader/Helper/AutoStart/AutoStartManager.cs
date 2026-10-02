using System;
using System.Runtime.InteropServices;
using ScratchDownloader.Models;
#if ANDROID
using Android.App;
#endif

namespace ScratchDownloader.Helper.AutoStart;

public static class AutoStartManager
{
    private const string AppName = "ScratchDownloader";

    private const string DesktopFile =
        "scratchdownload.desktop";

    private static readonly IAutoStartManager _autostartManager;

    static AutoStartManager()
    {
        _autostartManager =
            CreateAutoStartManager();
    }

    public static bool IsEnabled()
    {
        return _autostartManager.IsEnabled();
    }

    public static void SetEnabled(bool enable)
    {
        _autostartManager.SetEnabled(enable);
    }

    private static IAutoStartManager CreateAutoStartManager()
    {
#if ANDROID
        return new AndroidAutoStartManager(
            Application.Context);

#else

        if (RuntimeInformation.IsOSPlatform(
                OSPlatform.Windows))
            return new WindowsAutoStartManager(
                AppName,
                DesktopFile);

        if (RuntimeInformation.IsOSPlatform(
                OSPlatform.Linux))
            return new LinuxAutoStartManager(
                AppName,
                DesktopFile);

        throw new PlatformNotSupportedException(
            $"Auto-start is not supported on this platform. " +
            $"OS: {RuntimeInformation.OSDescription}");

#endif
    }
}