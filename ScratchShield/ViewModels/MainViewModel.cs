using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchShield.Helper;

namespace ScratchShield.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private IViewModel? currentView;

    [ObservableProperty]
    private bool isPaneOpen = true;

    public MainViewModel()
    {
        CurrentView = new HomeViewModel(this);
    }

    [RelayCommand]
    public void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    [RelayCommand]
    public void ShowHome()
    {
        CurrentView = new HomeViewModel(this);
    }

    [RelayCommand]
    public void ShowSettings()
    {
        CurrentView = new SettingsViewModel(this);
    }
}