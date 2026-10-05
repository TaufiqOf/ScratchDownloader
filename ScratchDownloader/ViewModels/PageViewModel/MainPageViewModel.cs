using System.Collections.Generic;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;
using ScratchDownloader.Models;

namespace ScratchDownloader.ViewModels.PageViewModel;

public partial class MainPageViewModel : ViewModelBase
{
    [ObservableProperty] private Dictionary<string, IViewModel> _views = new();

    [ObservableProperty] private IViewModel? _currentView;

    [ObservableProperty] private bool _isPaneOpen;

    [ObservableProperty] private string _version = "v" + Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);

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
        if (Views.TryGetValue(viewName, out var view))
        {
            if (CurrentView is ViewModelBase viewModel) viewModel.OnNavigatedFrom();
            if (view is ViewModelBase newViewModel) newViewModel.OnNavigatedTo();
            CurrentView = view;
        }
    }
}