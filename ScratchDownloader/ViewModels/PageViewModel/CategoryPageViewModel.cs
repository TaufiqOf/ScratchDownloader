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

public partial class CategoryPageViewModel:  ViewModelBase, IViewModel
{
    [ObservableProperty] private ObservableCollection<Category> _categories = new();
    
    public CategoryPageViewModel()
    {
        InitializeCategories();
        Categories.FirstOrDefault()?.IsExpanded = true;
    }
    
    private void InitializeCategories()
    {
        foreach (var category in SettingsService.Settings.Categories.Values)
        {
            category.PropertyChanged += OnCategoryPropertyChanged;
            Categories.Add(category);
        }
    }

    private void OnCategoryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Category.IsExpanded) && sender is Category expandedCategory)
        {
            if (expandedCategory.IsExpanded)
            {
                // Collapse all other categories
                foreach (var category in Categories)
                {
                    if (category != expandedCategory)
                    {
                        category.IsExpanded = false;
                    }
                }
            }
        }
    }
    
    [RelayCommand]
    private async Task BrowseFolderAsync(Category? category)
    {
        if (category is null)
            return;
        var path = await ApplicationManager.OpenFolderDialog(category.Folder);
        if (string.IsNullOrEmpty(path))
            return;
        category.Folder = path;
        SettingsService.Settings.Categories[category.Id].Folder = path;
    }
    
    [RelayCommand]
    private void DeleteCategory(Category? category)
    {
        if (category is null)
            return;

        category.PropertyChanged -= OnCategoryPropertyChanged;
        Categories.Remove(category);
        // Save settings here if applicable: SettingsService.Save();
    }

    [RelayCommand]
    private void ResetExtensions(Category? category)
    {
        if (category is null)
            return;

        category.Extension = string.Empty;
        // Save settings here if applicable: SettingsService.Save();
    }
}

