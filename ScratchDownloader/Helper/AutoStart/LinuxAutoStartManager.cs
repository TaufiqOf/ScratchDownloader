using System;
using System.IO;
using ScratchDownloader.Models;

namespace ScratchDownloader.Helper.AutoStart;

public sealed class LinuxAutoStartManager(string appName, string desktopFile) : IAutoStartManager
{
    private string _appName = appName;

    private string _desktopFileFile = desktopFile;


    private static readonly string? FlatpakId =
        Environment.GetEnvironmentVariable(
            "FLATPAK_ID");


    private static bool IsFlatpak =>
        !string.IsNullOrWhiteSpace(FlatpakId);


    private static string AutostartDirectory =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            ".config",
            "autostart");


    private string DesktopFilePath =>
        Path.Combine(
            AutostartDirectory,
            _desktopFileFile);


    public bool IsEnabled()
    {
        return File.Exists(
            DesktopFilePath);
    }


    public void SetEnabled(bool enable)
    {
        if (!enable)
        {
            Disable();

            return;
        }


        Enable();
    }


    private  void Enable()
    {
        if (!TryGetExecCommand(
                out var execCommand))
        {
            return;
        }


        Directory.CreateDirectory(
            AutostartDirectory);


        var content = $"""
            [Desktop Entry]
            Type=Application
            Version=1.0
            Name={_appName}
            Comment=ScratchShield
            Exec={execCommand}
            Terminal=false
            StartupNotify=false
            X-GNOME-Autostart-enabled=true
            X-KDE-autostart-enabled=true
            """;


        File.WriteAllText(
            DesktopFilePath,
            content);
    }


    private  void Disable()
    {
        if (!File.Exists(
                DesktopFilePath))
        {
            return;
        }


        File.Delete(
            DesktopFilePath);
    }


    private  bool TryGetExecCommand(
        out string execCommand)
    {
        // --------------------------------------------------------
        // Flatpak
        // --------------------------------------------------------

        if (IsFlatpak &&
            !string.IsNullOrWhiteSpace(FlatpakId))
        {
            execCommand =
                $"flatpak run {FlatpakId} --autostart";

            return true;
        }


        // --------------------------------------------------------
        // AppImage
        // --------------------------------------------------------

        var appImagePath =
            Environment.GetEnvironmentVariable(
                "APPIMAGE");


        if (!string.IsNullOrWhiteSpace(
                appImagePath))
        {
            try
            {
                appImagePath =
                    Path.GetFullPath(
                        appImagePath);
            }
            catch
            {
                appImagePath =
                    string.Empty;
            }


            if (File.Exists(appImagePath))
            {
                execCommand =
                    $"{QuoteExecArgument(appImagePath)} --autostart";

                return true;
            }
        }


        // --------------------------------------------------------
        // Normal executable
        // --------------------------------------------------------

        var executablePath =
            Environment.ProcessPath;


        if (!string.IsNullOrWhiteSpace(
                executablePath))
        {
            try
            {
                executablePath =
                    Path.GetFullPath(
                        executablePath);
            }
            catch
            {
                executablePath =
                    string.Empty;
            }


            if (File.Exists(
                    executablePath))
            {
                execCommand =
                    $"{QuoteExecArgument(executablePath)} --autostart";

                return true;
            }
        }


        execCommand =
            string.Empty;

        return false;
    }


    private static string QuoteExecArgument(
        string value)
    {
        return "\"" +
               value
                   .Replace(
                       "\\",
                       "\\\\")
                   .Replace(
                       "\"",
                       "\\\"") +
               "\"";
    }
}