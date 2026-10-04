using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;
using ScratchDownloader.Localization;
using ScratchDownloader.Models;
using ScratchDownloader.Services;
using ScratchDownloader.ViewModels.DialogControlViewModel;

namespace ScratchDownloader.ViewModels.PageViewModel;

public partial class HomePageViewModel : ViewModelBase, IViewModel
{
    private readonly MainPageViewModel _mainPageViewModel;
    private bool _updateFilterDisabled = true;
    private Timer _loadTimer;

    public HomePageViewModel(MainPageViewModel mainPageViewModel)
    {
        _mainPageViewModel = mainPageViewModel;
        Strings.Instance.PropertyChanged += OnStringsChanged;
        DownloadManager = ApplicationManager.DownloadManager;
        DownloadManager.ItemUpdated += UpdateFilter;
        _loadTimer = new Timer(1000);
        _loadTimer.Elapsed += (sender, args) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _loadTimer.Stop();
                DownloadManager.StartPendingDownloads();
                _updateFilterDisabled = false;
                UpdateFilter();
            });
        };
        _loadTimer.Start();
    }

    private void OnStringsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "Item[]")
            return;

        if (Categories is { Count: > 0 })
            Categories[0].Name = Strings.Get("AllCategories");
        if (Queues is { Count: > 0 })
            Queues[0].Name = Strings.Get("AllQueues");
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

    partial void OnSelectedDownloadChanged(DownloadItemViewModel? value)
    {
        OnPropertyChanged(nameof(value.CanShowWidget));
        OnPropertyChanged(nameof(value.CanOpen));

    }

    [RelayCommand]
    private async Task NewAsync()
    {
        try
        {
            var newDownloadDialog = new AddUrlDialogControlViewModel();
            var okCommand = new RelayCommand(() => StartDownload(newDownloadDialog));
            await DialogManager.ShowMessage(newDownloadDialog, Strings.Get("NewDownload"), okCommand);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await DialogManager.ShowMessage(MessageDialogType.Error, Strings.Get("NewDownload"),
                Strings.Get("FailedNewDownload"));
        }
        // Open the new download dialog.
    }

    private void StartDownload(AddUrlDialogControlViewModel newDownloadDialog)
    {
        ApplicationManager.DownloadManager.Add(newDownloadDialog.DownloadItemInformation);
        UpdateFilter();
    }

    [RelayCommand]
    private async Task ResumeAllAsync()
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
            await DialogManager.ShowMessage(MessageDialogType.Error, Strings.Get("ResumeDownloadTitle"),
                Strings.Get("FailedResumeAll"));
        }
    }

    [RelayCommand]
    private async Task PauseAllAsync()
    {
        try
        {
            foreach (var download in DownloadManager.Downloads)
                DownloadManager.Pause(download);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await DialogManager.ShowMessage(MessageDialogType.Error, Strings.Get("PauseDownloadTitle"),
                Strings.Get("FailedPauseAll"));
        }
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        try
        {
            await DialogManager.ShowMessage(MessageDialogType.Warning, Strings.Get("ClearDownloadHistory"),
                Strings.Get("ClearHistoryConfirmation"),
                Strings.Get("Yes"),
                new RelayCommand(() =>
                {
                    DownloadManager.Clear();
                    UpdateFilter();
                }),
                Strings.Get("No"));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await DialogManager.ShowMessage(MessageDialogType.Error, Strings.Get("ClearDownloadTitle"),
                Strings.Get("FailedClearDownloads"));
        }
    }


    [RelayCommand]
    private async Task StopAllAsync()
    {
        try
        {
            await DialogManager.ShowMessage(MessageDialogType.Warning, Strings.Get("StopAll"),
                Strings.Get("StopAllConfirmation"),
                Strings.Get("Yes"),
                new RelayCommand(() =>
                {
                    foreach (var download in DownloadManager.Downloads)
                        DownloadManager.Stop(download);
                }),
                Strings.Get("No"));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await DialogManager.ShowMessage(MessageDialogType.Error, Strings.Get("StopDownloadTitle"),
                Strings.Get("FailedStopAll"));
        }
    }
    [RelayCommand]
    private async Task ResumeDownloadAsync(DownloadItemViewModel? download)
    {
        if (download is null)
            return;

        DownloadManager.Resume(download);
    }

    [RelayCommand]
    private void StartDownload(DownloadItemViewModel? download)
    {
        if (download is null)
            return;

        DownloadManager.Start(download);
    }

    [RelayCommand]
    private void StopDownload(DownloadItemViewModel? download)
    {
        if (download is null)
            return;

        DownloadManager.Stop(download);
    }
    
   
    partial void OnSelectedQueueChanged(Queue? value)
    {
        if (SelectedQueue == null || SelectedCategory == null)
            return;
        UpdateFilter();
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (SelectedCategory == null || SelectedQueue == null)
            return;
        UpdateFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        UpdateFilter();
    }

    private void UpdateFilter()
    {
        if (_updateFilterDisabled)
            return;
        Categories[0].ApplicationCount = SettingsService.Settings.Categories.Values.Sum(c => c.ApplicationCount);
        Queues[0].ApplicationCount = SettingsService.Settings.Queues.Values.Sum(q => q.ApplicationCount);
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
        Categories.Insert(0, new Category { Name = Strings.Get("AllCategories"), Id = "all" });
        Queues.Insert(0, new Queue { Name = Strings.Get("AllQueues"), Id = "all" });
        SelectedCategory = Categories.FirstOrDefault();
        SelectedQueue = Queues.FirstOrDefault();
        base.OnNavigatedTo();

    }
}