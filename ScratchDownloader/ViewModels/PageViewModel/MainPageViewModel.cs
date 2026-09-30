using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;

namespace ScratchDownloader.ViewModels;

public partial class MainPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private IViewModel? currentView;

    [ObservableProperty]
    private bool isPaneOpen = false;

    [ObservableProperty]
    private bool isHomeSelected;

    [ObservableProperty]
    private bool isSettingsSelected;

    public MainPageViewModel()
    {
        UpdateSelectedView(nameof(HomePageViewModel));
    }


    [RelayCommand]
    public void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    [RelayCommand]
    public void ShowHome()
    {
        UpdateSelectedView(nameof(HomePageViewModel));
    }

    [RelayCommand]
    public void ShowSettings()
    {
        UpdateSelectedView(nameof(SettingsPageViewModel));
    }
    
    private void UpdateSelectedView(string viewName)
    {
        CurrentView = viewName == nameof(HomePageViewModel) ? new HomePageViewModel(this) : new SettingsPageViewModel();
        IsHomeSelected = viewName == nameof(HomePageViewModel);
        IsSettingsSelected = viewName == nameof(SettingsPageViewModel);
    }
}