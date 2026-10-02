using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;
using ScratchDownloader.Models;
using ScratchDownloader.Services;
using ScratchDownloader.ViewModels.DialogControlViewModel;

namespace ScratchDownloader.ViewModels.PageViewModel;

public partial class HomePageViewModel : ViewModelBase, IViewModel
{
    private readonly MainPageViewModel _mainPageViewModel;

    public HomePageViewModel(MainPageViewModel mainPageViewModel)
    {
        _mainPageViewModel = mainPageViewModel;
        DownloadManager = ApplicationManager.DownloadManager;

    }

    public DownloadManager DownloadManager { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DownloadItemViewModel> FilteredDownloads { get; set; } =
        new();


    [ObservableProperty] public partial DownloadItemViewModel? SelectedDownload { get; set; }
    [ObservableProperty] public partial ObservableCollection<Queue> Queues { get; set; }
    [ObservableProperty] public partial ObservableCollection<Category> Categories { get; set; }

    [ObservableProperty] public partial Queue? SelectedQueue { get; set; }
    [ObservableProperty] public partial Category? SelectedCategory { get; set; }
    [ObservableProperty] public partial string SearchText { get; set; }


    [RelayCommand]
    private async Task NewAsync()
    {
        try
        {
            var newDownloadDialog = new AddUrlDialogControlViewModel();
            var okCommand = new RelayCommand(() => StartDownload(newDownloadDialog));
            await DialogManager.ShowMessage(newDownloadDialog, "New Download", okCommand);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            DialogManager.ShowMessage(MessageDialogType.Error, "New Download",
                "Failed to create a new download. Please try again.");
        }
        // Open the new download dialog.
    }

    private void StartDownload(AddUrlDialogControlViewModel newDownloadDialog)
    {
        ApplicationManager.DownloadManager.Add(newDownloadDialog.DownloadItemInformation);
        UpdateFilter();
    }

    [RelayCommand]
    private void ResumeAll()
    {
        try
        {
            foreach (var download in DownloadManager.Downloads)
                if (download.Status == DownloadStatus.Paused)
                    DownloadManager.Resume(download);
                else if (download.Status == DownloadStatus.Queued)
                    DownloadManager.Start(download);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            DialogManager.ShowMessage(MessageDialogType.Error, "Resume Download",
                "Failed to resume all downloads. Please try again.");
        }
    }

    [RelayCommand]
    private void PauseAll()
    {
        try
        {
            foreach (var download in DownloadManager.Downloads)
                DownloadManager.Pause(download);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            DialogManager.ShowMessage(MessageDialogType.Error, "Pause Download",
                "Failed to pause all downloads. Please try again.");
        }
    }


    [RelayCommand]
    private void StopAll()
    {
        try
        {
            DialogManager.ShowMessage(MessageDialogType.Warning, "Stop All Downloads",
                "Are you sure you want to stop all downloads? This action cannot be undone.",
                "Yes",
                new RelayCommand(() =>
                {
                    foreach (var download in DownloadManager.Downloads)
                        DownloadManager.Stop(download);
                }),
                "No");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            DialogManager.ShowMessage(MessageDialogType.Error, "Stop Download",
                "Failed to stop all downloads. Please try again.");
        }
    }


    partial void OnSelectedQueueChanged(Queue? value)
    {
        if(SelectedQueue == null || SelectedCategory == null)
            return;
        UpdateFilter();
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        if(SelectedCategory == null || SelectedQueue == null)
            return;
        UpdateFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        UpdateFilter();
    }

    private void UpdateFilter()
    {
        FilteredDownloads = new ObservableCollection<DownloadItemViewModel>(
            DownloadManager.Downloads.Where(d =>
                d.DownloadItemInformation != null &&
                (SelectedCategory.Id == "all" ||
                 d.DownloadItemInformation.Category.Id == SelectedCategory.Id) &&
                (SelectedQueue.Id == "all" ||
                 d.DownloadItemInformation.Queue.Id == SelectedQueue.Id)
                && (string.IsNullOrWhiteSpace(SearchText) ||
                    d.DownloadItemInformation.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            )
        );
    }

    public override void OnNavigatedTo()
    {
        Categories = new ObservableCollection<Category>(SettingsService.Settings.Categories.Values);
        Queues = new ObservableCollection<Queue>(SettingsService.Settings.Queues.Values);
        Categories.Insert(0, new Category { Name = "All Category", Id = "all" });
        Queues.Insert(0, new Queue { Name = "All Queue", Id = "all" });
        SelectedCategory = Categories.FirstOrDefault();
        SelectedQueue = Queues.FirstOrDefault();
        base.OnNavigatedTo();
        DownloadManager.StartPendingDownloads();
    }
}