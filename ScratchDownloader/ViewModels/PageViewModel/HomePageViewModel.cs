using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using ScratchDownloader.Helper;
using ScratchDownloader.Models;
using ScratchDownloader.ViewModels.DialogControlViewModel;

namespace ScratchDownloader.ViewModels;

public partial class HomePageViewModel : ViewModelBase, IViewModel
{
    private readonly MainPageViewModel _mainPageViewModel;

    public HomePageViewModel(MainPageViewModel mainPageViewModel)
    {
        _mainPageViewModel = mainPageViewModel;
        Downloads = DownloadManager.Downloads;
    }

    [ObservableProperty] public partial Models.DownloadItemViewModel? SelectedDownload { get; set; }

    [ObservableProperty] public partial ObservableCollection<Models.DownloadItemViewModel> Downloads { get; set; }

    public string DownloadSummary =>
        $"{Downloads.Count} download{(Downloads.Count == 1 ? "" : "s")}";

    public string ActiveCount =>
        $"{Downloads.Count(x => x.Status == "Downloading")} active";

    public string TotalSpeed =>
        $"{Downloads.Where(x => x.Status == "Downloading")
            .Sum(x => x.SpeedValue):N1} MB/s";

    public string TotalDownloaded =>
        $"{Downloads.Sum(x => x.DownloadedBytes):N0} MB";


    [RelayCommand]
    private void New()
    {
        try
        {
            throw new NotImplementedException("New download dialog is not implemented yet.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            DialogManager.ShowMessage(MessageDialogType.Error, "New Download",
                "Failed to create a new download. Please try again.");
        }
        // Open the new download dialog.
    }

    [RelayCommand]
    private void ResumeAll()
    {
        try
        {
            foreach (var download in Downloads)
                download.Resume();
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
            foreach (var download in Downloads)
                download.Pause();
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
            foreach (var download in Downloads)
                download.Stop();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            DialogManager.ShowMessage(MessageDialogType.Error, "Stop Download",
                "Failed to stop all downloads. Please try again.");
        }
    }

    [RelayCommand]
    public void ShowSettings()
    {
        _mainPageViewModel.ShowSettings();
    }
}