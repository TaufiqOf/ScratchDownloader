using System.Collections.Generic;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;
using ScratchDownloader.Models;

namespace ScratchDownloader.ViewModels.PageViewModel;

public partial class MainPageViewModel : ViewModelBase
{
    [ObservableProperty] 
    private IViewModel? _currentView;

    [ObservableProperty] 
    private bool _isPaneOpen;

    private readonly Dictionary<string, IViewModel> _views = new();

    public MainPageViewModel()
    {
        _views.Add(nameof(HomePageViewModel), new HomePageViewModel(this));
        _views.Add(nameof(CategoryPageViewModel), new CategoryPageViewModel());
        _views.Add(nameof(QueuePageViewModel), new QueuePageViewModel());
        _views.Add(nameof(SettingsPageViewModel), new SettingsPageViewModel());
        UpdateSelectedView(nameof(HomePageViewModel));
    }

    [RelayCommand]
    private void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    [RelayCommand]
    private void ShowPage(string viewName)
    {
        UpdateSelectedView(viewName);
    }

    private void UpdateSelectedView(string viewName)
    {
        if (_views.TryGetValue(viewName, out var view))
        {
            if(CurrentView is ViewModelBase viewModel)
            {
                viewModel.OnNavigatedFrom();
            }   
            if(view is ViewModelBase newViewModel)
            {
                newViewModel.OnNavigatedTo();
            }
            CurrentView = view;
        }
    }
}