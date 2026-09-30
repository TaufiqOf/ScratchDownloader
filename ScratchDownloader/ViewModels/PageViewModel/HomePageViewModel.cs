using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using ScratchDownloader.Helper;
using ScratchDownloader.Models;
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

    [ObservableProperty] public partial Models.DownloadItemViewModel? SelectedDownload { get; set; }


    [RelayCommand]
    private async Task NewAsync()
    {
        try
        {
            var newDownloadDialog = new AddUrlDialogControlViewModel();
            newDownloadDialog.OkCommand = new RelayCommand(StartDownload);
            await DialogManager.ShowMessage(newDownloadDialog, "New Download");
            ApplicationManager.DownloadManager.Add(newDownloadDialog.DownloadItemInformation);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            DialogManager.ShowMessage(MessageDialogType.Error, "New Download",
                "Failed to create a new download. Please try again.");
        }
        // Open the new download dialog.
    }

    private void StartDownload()
    {
    }

    [RelayCommand]
    private void ResumeAll()
    {
        try
        {
            foreach (var download in DownloadManager.Downloads)
                DownloadManager.Resume(download);
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
            foreach (var download in DownloadManager.Downloads)
                DownloadManager.Stop(download);
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