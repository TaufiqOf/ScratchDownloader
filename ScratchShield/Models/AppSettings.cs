using Avalonia.Styling;

namespace ScratchShield.Models;

public class AppSettings
{
    public bool ProtectionEnabled { get; set; } = true;

    public bool NotificationsEnabled { get; set; } = true;

    public bool StartWithSystem { get; set; } = false;

    public ThemeVariant Theme { get; set; } = ThemeVariant.Default;
    
    public ThemeVariant GetThemeVariant()
    {
        var key = Theme.Key.ToString();
        return key == "Light" ? ThemeVariant.Light : key == "Dark" ? ThemeVariant.Dark : ThemeVariant.Default;
    }
}