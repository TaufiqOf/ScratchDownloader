using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchShield.Helper;

namespace ScratchShield.ViewModels;

public partial class HomeViewModel : ViewModelBase, IViewModel
{
    public HomeViewModel()
    {
        
    }
    [ObservableProperty] public partial string ProtectionStatus { get; set; } = "Protection Status";

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