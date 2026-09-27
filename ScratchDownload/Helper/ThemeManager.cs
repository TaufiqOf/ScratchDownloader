using Avalonia;
using Avalonia.Styling;

namespace ScratchDownload.Helper;

public static class ThemeManager
{
    public static void SetTheme(ThemeVariant theme)
    {
        if (Application.Current is null)
            return;

        Application.Current.RequestedThemeVariant = theme;
    }
}