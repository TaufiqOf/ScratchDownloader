using System.Collections.Generic;
using Avalonia.Styling;
using ScratchDownloader.Helper;

namespace ScratchDownloader.Models;

public class AppSettings
{
    public bool NotificationsEnabled { get; set; } = true;

    public bool StartWithSystem { get; set; } = false;
    public bool StartMinimized { get; set; } = true;

    public ThemeVariant Theme { get; set; } = ThemeVariant.Default;
    public Dictionary<string, Category> Categories { get; set; } = ApplicationManager.Categories;
    public Dictionary<string, Queue> Queues { get; set; } = ApplicationManager.Queues;

    public ThemeVariant GetThemeVariant()
    {
        var key = Theme.Key.ToString();
        return key == "Light" ? ThemeVariant.Light : key == "Dark" ? ThemeVariant.Dark : ThemeVariant.Default;
    }
}