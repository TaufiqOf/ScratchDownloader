using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownload.Helper;
using System.Collections.ObjectModel;
using System.Linq;

namespace ScratchDownload.ViewModels;

public partial class HomeViewModel : ViewModelBase, IViewModel
{
    private readonly MainViewModel _mainViewModel;

    public HomeViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        Downloads = DownloadManager.Downloads;
    }

    [ObservableProperty] public partial DownloadItemViewModel? SelectedDownload { get; set; }

    [ObservableProperty] public partial ObservableCollection<DownloadItemViewModel> Downloads { get; set; }

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
        // Open the new download dialog.
    }

    [RelayCommand]
    private void Resume()
    {
        if (SelectedDownload is null)
            return;

        SelectedDownload.Resume();
    }

    [RelayCommand]
    private void Stop()
    {
        if (SelectedDownload is null)
            return;

        SelectedDownload.Stop();
    }

    [RelayCommand]
    private void StopAll()
    {
        foreach (var download in Downloads)
            download.Stop();
    }

    [RelayCommand]
    public void ShowSettings()
    {
        _mainViewModel.ShowSettings();
    }
}