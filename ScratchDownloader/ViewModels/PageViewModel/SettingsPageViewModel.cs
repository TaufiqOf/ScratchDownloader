using System.Collections.Generic;
using System.Linq;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using ScratchDownloader.Helper;
using ScratchDownloader.Models;
using ScratchDownloader.Services;

namespace ScratchDownloader.ViewModels.PageViewModel;

public partial class SettingsPageViewModel : ViewModelBase, IViewModel
{
    public IReadOnlyList<ThemeOption> Themes { get; } =
    [
        new("System", ThemeVariant.Default),
        new("Light", ThemeVariant.Light),
        new("Dark", ThemeVariant.Dark)
    ];

    [ObservableProperty]
    private ThemeOption? selectedTheme;
    

    [ObservableProperty]
    private bool notificationsEnabled =
        SettingsService.Settings.NotificationsEnabled;

    [ObservableProperty]
    private bool startWithSystem =
        SettingsService.Settings.StartWithSystem;

    public SettingsPageViewModel()
    {
        var savedTheme = SettingsService.Settings.GetThemeVariant();

        SelectedTheme = Themes.FirstOrDefault(x =>
            x.Variant == savedTheme);
    }

    partial void OnSelectedThemeChanged(ThemeOption? value)
    {
        if (value is null)
            return;

        SettingsService.Settings.Theme = value.Variant;

        ThemeManager.SetTheme(value.Variant);

        SettingsService.Save();
    }

    partial void OnNotificationsEnabledChanged(bool value)
    {
        SettingsService.Settings.NotificationsEnabled = value;
        SettingsService.Save();
    }

    partial void OnStartWithSystemChanged(bool value)
    {
        SettingsService.Settings.StartWithSystem = value;
        AutoStartManager.SetEnabled(value);
        SettingsService.Save();
    }
}