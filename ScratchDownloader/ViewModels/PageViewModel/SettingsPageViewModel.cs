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
    [ObservableProperty] private bool _notificationsEnabled =
        SettingsService.Settings.NotificationsEnabled;

    [ObservableProperty] private ThemeOption? _selectedTheme;

    [ObservableProperty] private bool _startMinimized =
        SettingsService.Settings.StartMinimized;

    [ObservableProperty] private bool _startWithSystem =
        SettingsService.Settings.StartWithSystem;

    [ObservableProperty] private bool _openWidgetEnabled =
        SettingsService.Settings.OpenWidgetEnabled;

    [ObservableProperty] private bool _closeWidgetEnabled =
        SettingsService.Settings.CloseWidgetEnabled;

    [ObservableProperty] private double _closeWidgetInterval =
        SettingsService.Settings.AutoCloseInterval / 1000;

    public SettingsPageViewModel()
    {
        var savedTheme = SettingsService.Settings.GetThemeVariant();

        SelectedTheme = Themes.FirstOrDefault(x =>
            x.Variant == savedTheme);
    }

    public IReadOnlyList<ThemeOption> Themes { get; } =
    [
        new("System", ThemeVariant.Default),
        new("Light", ThemeVariant.Light),
        new("Dark", ThemeVariant.Dark)
    ];


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

    partial void OnStartMinimizedChanged(bool value)
    {
        SettingsService.Settings.StartMinimized = value;
        SettingsService.Save();
    }

    partial void OnCloseWidgetEnabledChanged(bool value)
    {
        SettingsService.Settings.CloseWidgetEnabled = value;
        SettingsService.Save();
    }

    partial void OnOpenWidgetEnabledChanged(bool value)
    {
        SettingsService.Settings.OpenWidgetEnabled = value;
        SettingsService.Save();
    }

    partial void OnCloseWidgetIntervalChanged(double value)
    {
        SettingsService.Settings.AutoCloseInterval = value * 1000;
        SettingsService.Save();
    }
}