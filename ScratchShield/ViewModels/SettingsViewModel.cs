using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchShield.Helper;

namespace ScratchShield.ViewModels;

public partial class SettingsViewModel : ViewModelBase,IViewModel
{
    private readonly MainViewModel _mainViewModel;

    public SettingsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private void ShowHome()
    {
        _mainViewModel.ShowHome();
    }
    
    
}