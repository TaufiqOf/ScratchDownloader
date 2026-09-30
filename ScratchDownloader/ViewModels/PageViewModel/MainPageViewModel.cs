using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;
using ScratchDownloader.Models;

namespace ScratchDownloader.ViewModels.PageViewModel;

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
        UpdateSelectedView(nameof(PageViewModel.HomePageViewModel));
    }


    [RelayCommand]
    public void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    [RelayCommand]
    public void ShowHome()
    {
        UpdateSelectedView(nameof(PageViewModel.HomePageViewModel));
    }

    [RelayCommand]
    public void ShowSettings()
    {
        UpdateSelectedView(nameof(SettingsPageViewModel));
    }
    
    private void UpdateSelectedView(string viewName)
    {
        CurrentView = viewName == nameof(PageViewModel.HomePageViewModel) ? new PageViewModel.HomePageViewModel(this) : new SettingsPageViewModel();
        IsHomeSelected = viewName == nameof(PageViewModel.HomePageViewModel);
        IsSettingsSelected = viewName == nameof(SettingsPageViewModel);
    }
}