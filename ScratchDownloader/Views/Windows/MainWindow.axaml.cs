using Avalonia.Controls;
using Avalonia.Interactivity;
using ScratchDownloader.ViewModels.PageViewModel;

namespace ScratchDownloader.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if(DataContext is MainPageViewModel vm)
        {
            if (vm.Views.TryGetValue(nameof(HomePageViewModel), out var homeView) &&
                homeView is HomePageViewModel homePageViewModel)
            {
                foreach (var downloadItemViewModel in homePageViewModel.DownloadManager.Downloads)
                {
                    downloadItemViewModel.Closing();
                }
            }
        }
        base.OnClosing(e);
    }
}