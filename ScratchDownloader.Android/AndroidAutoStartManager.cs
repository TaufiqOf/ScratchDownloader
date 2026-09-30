using Android.Content;
using ScratchDownloader.Models;

namespace ScratchDownloader.Android;

public sealed class AndroidAutoStartManager : IAutoStartManager
{
    private const string PreferenceName = "autostart";
    private const string EnabledKey = "enabled";

    private readonly Context _context;

    public AndroidAutoStartManager(Context context)
    {
        _context = context;
    }

    public bool IsEnabled()
    {
        var prefs = _context.GetSharedPreferences(
            PreferenceName,
            FileCreationMode.Private);

        return prefs?.GetBoolean(EnabledKey, false) ?? false;
    }

    public void SetEnabled(bool enable)
    {
        var prefs = _context.GetSharedPreferences(
            PreferenceName,
            FileCreationMode.Private);

        prefs?.Edit()
            ?.PutBoolean(EnabledKey, enable)
            ?.Apply();
    }
}