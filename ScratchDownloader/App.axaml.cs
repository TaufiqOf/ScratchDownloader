using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ScratchDownloader.Helper;
using ScratchDownloader.Services;
using ScratchDownloader.ViewModels;
using ScratchDownloader.Views;
using ScratchDownloader.Views.Windows;

namespace ScratchDownloader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ThemeManager.SetTheme(SettingsService.Settings.GetThemeVariant());
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainPageViewModel()
            };
            DialogManager.MainWindow = desktop.MainWindow;
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory =
                () => new MainPageView { DataContext = new MainPageViewModel() };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainPageView
            {
                DataContext = new MainPageViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}