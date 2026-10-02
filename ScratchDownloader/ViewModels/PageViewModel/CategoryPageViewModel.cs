using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;
using ScratchDownloader.Models;
using ScratchDownloader.Services;

namespace ScratchDownloader.ViewModels.PageViewModel;

public partial class CategoryPageViewModel : ViewModelBase, IViewModel
{
    [ObservableProperty] private ObservableCollection<Category> _categories = new();

    public ObservableCollection<Queue> Queues { get; } = new();


    [RelayCommand]
    private async Task BrowseFolderAsync(Category? category)
    {
        if (category is null)
            return;

        var path = await ApplicationManager.OpenFolderDialog(category.Folder);

        if (string.IsNullOrEmpty(path))
            return;

        category.Folder = path;
    }

    [RelayCommand]
    private void DeleteCategory(Category? category)
    {
        if (category is null)
            return;

        category.PropertyChanged -= OnCategoryPropertyChanged;

        Categories.Remove(category);
    }

    [RelayCommand]
    private void ResetExtensions(Category? category)
    {
        if (category is null)
            return;

        category.Extension = string.Empty;
    }

    public override void OnNavigatedTo()
    {
        InitializeQueues();
        InitializeCategories();

        Categories.FirstOrDefault()?.IsExpanded = true;

        base.OnNavigatedTo();
    }

    public override void OnNavigatedFrom()
    {
        foreach (var category in Categories) category.PropertyChanged -= OnCategoryPropertyChanged;

        SettingsService.Save();

        base.OnNavigatedFrom();
    }

    private void InitializeQueues()
    {
        Queues.Clear();

        foreach (var queue in SettingsService.Settings.Queues.Values) Queues.Add(queue);
    }

    private void InitializeCategories()
    {
        foreach (var category in Categories)
            category.PropertyChanged -= OnCategoryPropertyChanged;

        Categories.Clear();

        foreach (var category in SettingsService.Settings.Categories.Values)
        {
            category.SelectedQueue =
                Queues.FirstOrDefault(q => q.Id == category.QueueId);

            category.PropertyChanged += OnCategoryPropertyChanged;

            Categories.Add(category);
        }
    }

    private void OnCategoryPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (sender is not Category category)
            return;

        if (e.PropertyName == nameof(Category.IsExpanded))
        {
            if (category.IsExpanded)
                foreach (var other in Categories)
                    if (other != category)
                        other.IsExpanded = false;

            return;
        }

        if (e.PropertyName == nameof(Category.SelectedQueue)) category.QueueId = category.SelectedQueue?.Id;
    }
}