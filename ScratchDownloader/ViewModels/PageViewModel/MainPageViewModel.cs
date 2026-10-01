using System.Collections.Generic;
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

    private readonly Dictionary<string, IViewModel> _views = new();

    public MainPageViewModel()
    {
        _views.Add(nameof(HomePageViewModel), new HomePageViewModel(this));
        _views.Add(nameof(CategoryPageViewModel), new CategoryPageViewModel());
        _views.Add(nameof(SettingsPageViewModel), new SettingsPageViewModel());
        UpdateSelectedView(nameof(HomePageViewModel));
    }

    [RelayCommand]
    public void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    [RelayCommand]
    public void ShowPage(string viewName)
    {
        UpdateSelectedView(viewName);
    }

    private void UpdateSelectedView(string viewName)
    {
        if (_views.TryGetValue(viewName, out var view))
        {
            CurrentView = view;
        }
    }
}