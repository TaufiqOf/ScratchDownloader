using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchShield.Helper;

namespace ScratchShield.ViewModels;

public partial class HomeViewModel : ViewModelBase, IViewModel
{
    [ObservableProperty] public partial string Setting { get; set; } = "Settings";

    private readonly MainViewModel _mainViewModel;

    public HomeViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }
    [RelayCommand]
    public void ShowSettings()
    {
        _mainViewModel.ShowSettings();
    }   
}