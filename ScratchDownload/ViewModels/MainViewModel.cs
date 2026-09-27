using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownload.Helper;

namespace ScratchDownload.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private IViewModel? currentView;

    [ObservableProperty]
    private bool isPaneOpen = false;

    [ObservableProperty]
    private bool isHomeSelected;

    [ObservableProperty]
    private bool isSettingsSelected;

    public MainViewModel()
    {
        UpdateSelectedView(nameof(HomeViewModel));
    }


    [RelayCommand]
    public void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    [RelayCommand]
    public void ShowHome()
    {
        UpdateSelectedView(nameof(HomeViewModel));
    }

    [RelayCommand]
    public void ShowSettings()
    {
        UpdateSelectedView(nameof(SettingsViewModel));
    }
    
    private void UpdateSelectedView(string viewName)
    {
        CurrentView = viewName == nameof(HomeViewModel) ? new HomeViewModel(this) : new SettingsViewModel();
        IsHomeSelected = viewName == nameof(HomeViewModel);
        IsSettingsSelected = viewName == nameof(SettingsViewModel);
    }
}